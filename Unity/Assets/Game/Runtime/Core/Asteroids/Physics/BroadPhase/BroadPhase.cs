using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using FFS.Libraries.StaticPack;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class BroadPhase : IResource {
			private const int InitialCapacity = 128;

			private readonly List<W.Entity> _queryBuffer = new();

			private readonly List<(W.Entity A, W.Entity B)> _pairsBuffer = new();
			private readonly List<(W.Entity Entity, CellIndex Lo, CellIndex Hi)> _cellScratch = new();

			public struct Node {
				// Next node in the cell chain, -1 if tail. While free: next free node.
				public int Next;
				public W.Entity Entity;
			}

			public readonly int Width;
			public readonly int Height;
			public readonly int WidthMask;
			public readonly int HeightMask;
			public readonly int CellSize;
			public readonly int CellSizeShift;
			public readonly FVector2 OriginOffset;

			public int[] Heads;
			public Node[] Nodes;

			public int[] ActiveCells; // Two or more entries in cell.
			public int ActiveCellCount;
			private int[] _cellSlot;

			public int UsedNodesCount;
			public int NextFreeNodeIndex;
			public uint QueryId;

			public BroadPhase(int width, int height, int cellSize) {
				if (!MathUtils.IsPowerOfTwo(width)) {
					throw new ArgumentException("World width must be a power of two for bit-mask wrapping.");
				}

				if (!MathUtils.IsPowerOfTwo(height)) {
					throw new ArgumentException("World height must be a power of two for bit-mask wrapping.");
				}

				if (!MathUtils.IsPowerOfTwo(cellSize)) {
					throw new ArgumentException("Cell size must be a power of two.");
				}

				Width = width;
				Height = height;
				WidthMask = width - 1;
				HeightMask = height - 1;
				CellSize = cellSize;
				CellSizeShift = MathUtils.CeilLog2((uint)CellSize);

				OriginOffset = new FVector2(Width.ToFP(), Height.ToFP()) * cellSize / 2;

				Heads = new int[Width * Height];
				for (var i = 0; i < Heads.Length; i++) {
					Heads[i] = -1;
				}

				Nodes = new Node[InitialCapacity];
				UsedNodesCount = 0;
				NextFreeNodeIndex = -1;

				// At most every cell can be non-empty, so a fixed-size backing never resizes.
				ActiveCells = new int[Width * Height];
				_cellSlot = new int[Width * Height];
				ActiveCellCount = 0;
			}

			public Guid? Guid() => new Guid("559781fb614f49408cfc7cd5be71dc4e");

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Write(ref BinaryPackWriter writer) {
				writer.WriteArrayUnmanaged(Heads);
				writer.WriteArrayUnmanaged(Nodes);
				writer.WriteInt(UsedNodesCount);
				writer.WriteInt(NextFreeNodeIndex);
				writer.WriteUint(QueryId);

				writer.WriteInt(ActiveCellCount);
				writer.WriteArrayUnmanaged(ActiveCells, 0, ActiveCellCount);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Read(ref BinaryPackReader reader, byte version) {
				reader.ReadArrayUnmanaged(ref Heads);
				reader.ReadArrayUnmanaged(ref Nodes);
				UsedNodesCount = reader.ReadInt();
				NextFreeNodeIndex = reader.ReadInt();
				QueryId = reader.ReadUint();

				ActiveCellCount = reader.ReadInt();
				reader.ReadArrayUnmanaged(ref ActiveCells);

				for (var i = 0; i < ActiveCellCount; i++) {
					_cellSlot[ActiveCells[i]] = i;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public List<W.Entity> FindNearbyEntities(FAABB2 bounds) {
				_queryBuffer.Clear();

				MathUtils.IncrementWrapTo1(ref QueryId);
				var queryId = QueryId;

				var heads = Heads;
				var nodes = Nodes;

				var minCellX = FP.FloorToInt(ScaleToGrid(bounds.Min.X + OriginOffset.X));
				var minCellY = FP.FloorToInt(ScaleToGrid(bounds.Min.Y + OriginOffset.Y));
				var maxCellX = FP.FloorToInt(ScaleToGrid(bounds.Max.X + OriginOffset.X));
				var maxCellY = FP.FloorToInt(ScaleToGrid(bounds.Max.Y + OriginOffset.Y));

				var spanX = MathUtils.Min(maxCellX - minCellX + 1, Width);
				var spanY = MathUtils.Min(maxCellY - minCellY + 1, Height);

				var startX = minCellX & WidthMask;
				var startY = minCellY & HeightMask;

				var x = startX;
				for (var ix = 0; ix < spanX; ix++, x = (x + 1) & WidthMask) {
					var y = startY;
					for (var iy = 0; iy < spanY; iy++, y = (y + 1) & HeightMask) {
						var nodeIndex = heads[FlatIndex(x, y)];

						while (nodeIndex != -1) {
							var node = nodes[nodeIndex];
							var entity = node.Entity;

							ref var info = ref W.Components<BroadPhaseInfo>.Instance.Ref(entity)!;
							if (info.QueryId != queryId) {
								info.QueryId = queryId;
								_queryBuffer.Add(entity);
							}

							nodeIndex = node.Next;
						}
					}
				}

				return _queryBuffer;
			}

			public List<(W.Entity A, W.Entity B)> CollectPairs() {
				_pairsBuffer.Clear();

				var nodes = Nodes;
				var heads = Heads;
				var width = Width;
				var scratch = _cellScratch;

				for (var c = 0; c < ActiveCellCount; c++) {
					var cell = ActiveCells[c];
					var cellX = cell % width;
					var cellY = cell / width;

					// Gather the cell's entities once (one component fetch each) so the
					// O(k^2) pair loop below works on cached local data.
					scratch.Clear();
					var nodeIndex = heads[cell];
					while (nodeIndex != -1) {
						var entity = nodes[nodeIndex].Entity;
						ref readonly var info = ref entity.Read<BroadPhaseInfo>()!;
						scratch.Add((entity, info.LowerBound, info.UpperBound));
						nodeIndex = nodes[nodeIndex].Next;
					}

					var count = scratch.Count;
					for (var i = 0; i < count; i++) {
						var (entityA, loA, hiA) = scratch[i];
						for (var j = i + 1; j < count; j++) {
							var (entityB, loB, _) = scratch[j];

							if (cellX != OverlapStart(loA.X, hiA.X, loB.X, WidthMask)
								|| cellY != OverlapStart(loA.Y, hiA.Y, loB.Y, HeightMask)) {
								continue;
							}

							_pairsBuffer.Add(entityA.ID < entityB.ID ? (entityA, entityB) : (entityB, entityA));
						}
					}
				}

				return _pairsBuffer;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void MarkCellActive(int cell) {
				_cellSlot[cell] = ActiveCellCount;
				ActiveCells[ActiveCellCount++] = cell;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void MarkCellInactive(int cell) {
				// Swap-remove: move the last active cell into the freed slot.
				var slot = _cellSlot[cell];
				var last = ActiveCells[--ActiveCellCount];
				ActiveCells[slot] = last;
				_cellSlot[last] = slot;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private CellIndex CellIndex(FVector2 position) {
				var shiftedX = position.X + OriginOffset.X;
				var shiftedY = position.Y + OriginOffset.Y;

				var x = FP.FloorToInt(ScaleToGrid(shiftedX)) & WidthMask;
				var y = FP.FloorToInt(ScaleToGrid(shiftedY)) & HeightMask;

				CellIndex cellIndex = default;
				cellIndex.X = x;
				cellIndex.Y = y;

				return cellIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private FP ScaleToGrid(FP fp) {
				return FP.FromRaw(fp.RawValue >> CellSizeShift); // x / CellSize
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int FlatIndex(int x, int y) {
				return x + y * Width;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static int SpanCount(int lo, int hi, int mask) {
				return ((hi - lo) & mask) + 1;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private static int OverlapStart(int aLo, int aHi, int bLo, int mask) {
				var bLoInsideA = ((bLo - aLo) & mask) <= ((aHi - aLo) & mask);
				return bLoInsideA ? bLo : aLo;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Insert(W.Entity entity, ref BroadPhaseInfo info, FAABB2 bounds) {
				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);
				Insert(entity, ref info, minIndex, maxIndex);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void Insert(W.Entity entity, ref BroadPhaseInfo info, CellIndex minIndex, CellIndex maxIndex) {
				info.LowerBound = minIndex;
				info.UpperBound = maxIndex;

				var spanX = SpanCount(minIndex.X, maxIndex.X, WidthMask);
				var spanY = SpanCount(minIndex.Y, maxIndex.Y, HeightMask);
				var x = minIndex.X;
				for (var ix = 0; ix < spanX; ix++, x = (x + 1) & WidthMask) {
					var y = minIndex.Y;
					for (var iy = 0; iy < spanY; iy++, y = (y + 1) & HeightMask) {
						var cell = FlatIndex(x, y);
						var head = Heads[cell];
						if (head != -1 && Nodes[head].Next == -1) {
							MarkCellActive(cell);
						}

						var nodeIndex = AllocateNode();

						Nodes[nodeIndex] = new Node { Entity = entity, Next = Heads[cell] };
						Heads[cell] = nodeIndex;
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Remove(W.Entity entity, in BroadPhaseInfo info) {
				var min = info.LowerBound;
				var max = info.UpperBound;

				var nodes = Nodes;

				var spanX = SpanCount(min.X, max.X, WidthMask);
				var spanY = SpanCount(min.Y, max.Y, HeightMask);
				var x = min.X;
				for (var ix = 0; ix < spanX; ix++, x = (x + 1) & WidthMask) {
					var y = min.Y;
					for (var iy = 0; iy < spanY; iy++, y = (y + 1) & HeightMask) {
						var cell = FlatIndex(x, y);
						var nodeIndex = Heads[cell];
						var prevIndex = -1;

						while (nodeIndex != -1) {
							var node = nodes[nodeIndex];
							if (node.Entity == entity) {
								if (prevIndex == -1) {
									Heads[cell] = node.Next;
								} else {
									nodes[prevIndex].Next = node.Next;
								}

								FreeNode(nodeIndex);
								break;
							}

							prevIndex = nodeIndex;
							nodeIndex = node.Next;
						}

						var head = Heads[cell];
						if (head != -1 && nodes[head].Next == -1) {
							MarkCellInactive(cell);
						}
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void UpdateInfo(W.Entity entity, ref BroadPhaseInfo info, FAABB2 bounds) {
				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);

				if (ColliderHasMovedCells(info, minIndex, maxIndex)) {
					Remove(entity, info);
					Insert(entity, ref info, minIndex, maxIndex);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool ColliderHasMovedCells(in BroadPhaseInfo info, CellIndex minIndex, CellIndex maxIndex) {
				return info.LowerBound != minIndex || info.UpperBound != maxIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int AllocateNode() {
				if (NextFreeNodeIndex != -1) {
					var index = NextFreeNodeIndex;
					NextFreeNodeIndex = Nodes[index].Next;
					return index;
				}

				if (UsedNodesCount == Nodes.Length) {
					Array.Resize(ref Nodes, Nodes.Length * 2);
				}

				return UsedNodesCount++;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void FreeNode(int index) {
				Nodes[index].Entity = default;
				Nodes[index].Next = NextFreeNodeIndex;
				NextFreeNodeIndex = index;
			}
		}
	}
}
