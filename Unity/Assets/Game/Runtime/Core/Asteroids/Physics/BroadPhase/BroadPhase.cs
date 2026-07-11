using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using FFS.Libraries.StaticPack;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class BroadPhase : IResource {
			private readonly List<W.Entity> _queryBuffer = new();
			private readonly Stack<List<EntityGID>> _listPool = new();

			public readonly int Width;
			public readonly int Height;
			public readonly FVector2 CellSize;
			public readonly FVector2 InvertedCellSize;
			public readonly FVector2 OriginOffset;

			public readonly List<EntityGID>[] Grid;
			public uint QueryId;

			public BroadPhase(int width, int height, FVector2 cellSize) {
				Width = width;
				Height = height;
				CellSize = cellSize;
				InvertedCellSize = FVector2.One / cellSize;
				OriginOffset = new FVector2(Width.ToFP(), Height.ToFP()) * CellSize / 2;
				Grid = new List<EntityGID>[Width * Height];
			}

			public Guid? Guid() => new Guid("559781fb614f49408cfc7cd5be71dc4e");

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Write(ref BinaryPackWriter writer) {
				var cellCountPos = writer.MakePoint(sizeof(uint));
				var cellCount = 0u;
				for (var i = 0; i < Grid.Length; i++) {
					if (Grid[i] != null) {
						writer.WriteUint((uint)i);
						writer.WriteList(Grid[i]);
						cellCount++;
					}
				}
				writer.WriteUintAt(cellCountPos, cellCount);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Read(ref BinaryPackReader reader, byte version) {
				for (var i = 0; i < Grid.Length; i++) {
					if (Grid[i] != null) {
						_listPool.Push(Grid[i]);
						Grid[i] = null;
					}
				}

				var cellCount = reader.ReadUint();
				for (var i = 0; i < cellCount; i++) {
					var index = reader.ReadUint();
					Grid[index] = GetEmptyList();
					reader.ReadList(ref Grid[index]);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public List<W.Entity> FindNearbyEntities(FAABB2 bounds) {
				_queryBuffer.Clear();

				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);

				var startX = MathUtils.Max(0, minIndex.X);
				var startY = MathUtils.Max(0, minIndex.Y);

				var endX = MathUtils.Min(Width - 1, maxIndex.X);
				var endY = MathUtils.Min(Height - 1, maxIndex.Y);

				MathUtils.IncrementWrapTo1(ref QueryId);
				var queryId = QueryId;

				var grid = Grid;
				for (var x = startX; x <= endX; x++) {
					for (var y = startY; y <= endY; y++) {
						var index = FlatIndex(x, y);

						if (grid[index] == null) {
							continue;
						}

						foreach (var entityGid in grid[index]) {
							var entity = entityGid.Unpack<TWorld>();
							ref var info = ref entity.Mut<BroadPhaseInfo>()!;
							if (entity.IsNotDestroyed && info.QueryId != queryId) {
								info.QueryId = queryId;
								_queryBuffer.Add(entity);
							}
						}
					}
				}

				return _queryBuffer;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private CellIndex CellIndex(FVector2 position) {
				var shiftedX = position.X + OriginOffset.X;
				var shiftedY = position.Y + OriginOffset.Y;

				var x = FP.FloorToInt(shiftedX * InvertedCellSize.X);
				var y = FP.FloorToInt(shiftedY * InvertedCellSize.Y);

				var clampedX = MathUtils.Min(Width - 1, MathUtils.Max(0, x));
				var clampedY = MathUtils.Min(Height - 1, MathUtils.Max(0, y));

				return new CellIndex(clampedX, clampedY);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int FlatIndex(int x, int y) {
				return x + y * Width;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Insert(W.Entity entity, ref BroadPhaseInfo info, FAABB2 bounds) {
				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);

				info.LowerBound = minIndex;
				info.UpperBound = maxIndex;

				var grid = Grid;
				for (var x = minIndex.X; x <= maxIndex.X; x++) {
					for (var y = minIndex.Y; y <= maxIndex.Y; y++) {
						var index = FlatIndex(x, y);
						grid[index] ??= GetEmptyList();
						grid[index].Add(entity);
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Remove(W.Entity entity, BroadPhaseInfo info) {
				var min = info.LowerBound;
				var max = info.UpperBound;

				var grid = Grid;
				for (var x = min.X; x <= max.X; x++) {
					for (var y = min.Y; y <= max.Y; y++) {
						var index = FlatIndex(x, y);
						var list = grid[index];

						var removeIndex = list.IndexOf(entity);
						list[removeIndex] = list[^1];
						list.RemoveAt(list.Count - 1);

						if (list.Count == 0) {
							_listPool.Push(list);
							grid[index] = null;
						}
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void UpdateInfo(W.Entity entity, ref BroadPhaseInfo info, FAABB2 bounds) {
				if (ColliderHasMovedCells(info, bounds)) {
					Remove(entity, info);
					Insert(entity, ref info, bounds);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool ColliderHasMovedCells(BroadPhaseInfo info, FAABB2 bounds) {
				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);

				return info.LowerBound != minIndex || info.UpperBound != maxIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private List<EntityGID> GetEmptyList() {
				return _listPool.Count > 0 ? _listPool.Pop() : new List<EntityGID>();
			}
		}
	}
}
