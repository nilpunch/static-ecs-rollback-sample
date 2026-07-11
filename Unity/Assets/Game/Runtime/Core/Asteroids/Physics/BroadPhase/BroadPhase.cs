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
			private const int InitialCapacity = 128;

			private readonly List<W.Entity> _queryBuffer = new();

			public struct Node {
				// Next node in the cell chain, -1 if tail. While free: next free node.
				public int Next;
				public W.Entity Entity;
			}

			public readonly int Width;
			public readonly int Height;
			public readonly FVector2 CellSize;
			public readonly FVector2 InvertedCellSize;
			public readonly FVector2 OriginOffset;

			public int[] Heads;
			public Node[] Nodes;

			public int UsedNodesCount;
			public int NextFreeNodeIndex;
			public uint QueryId;

			public BroadPhase(int width, int height, FVector2 cellSize) {
				if (!MathUtils.IsPowerOfTwo(width * height)) {
					throw new ArgumentException("width * height must be power of 2");
				}

				Width = width;
				Height = height;
				CellSize = cellSize;

				InvertedCellSize = FVector2.One / cellSize;
				OriginOffset = new FVector2(Width.ToFP(), Height.ToFP()) * cellSize / 2;

				Heads = new int[Width * Height];
				for (var i = 0; i < Heads.Length; i++) {
					Heads[i] = -1;
				}

				Nodes = new Node[InitialCapacity];
				UsedNodesCount = 0;
				NextFreeNodeIndex = -1;
			}

			public Guid? Guid() => new Guid("559781fb614f49408cfc7cd5be71dc4e");

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Write(ref BinaryPackWriter writer) {
				writer.WriteArrayUnmanaged(Heads);
				writer.WriteArrayUnmanaged(Nodes);
				writer.WriteInt(UsedNodesCount);
				writer.WriteInt(NextFreeNodeIndex);
				writer.WriteUint(QueryId);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Read(ref BinaryPackReader reader, byte version) {
				reader.ReadArrayUnmanaged(ref Heads);
				reader.ReadArrayUnmanaged(ref Nodes);
				UsedNodesCount = reader.ReadInt();
				NextFreeNodeIndex = reader.ReadInt();
				QueryId = reader.ReadUint();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public List<W.Entity> FindNearbyEntities(FAABB2 bounds) {
				_queryBuffer.Clear();

				// CellIndex already clamps into [0,Width) x [0,Height).
				var minIndex = CellIndex(bounds.Min);
				var maxIndex = CellIndex(bounds.Max);

				MathUtils.IncrementWrapTo1(ref QueryId);
				var queryId = QueryId;

				var heads = Heads;
				var nodes = Nodes;

				for (var x = minIndex.X; x <= maxIndex.X; x++) {
					for (var y = minIndex.Y; y <= maxIndex.Y; y++) {
						var nodeIndex = heads[FlatIndex(x, y)];

						while (nodeIndex != -1) {
							var node = nodes[nodeIndex];
							var entity = node.Entity;

							// Only touch the component of a live entity.
							if (entity.IsNotDestroyed) {
								ref var info = ref entity.Mut<BroadPhaseInfo>()!;
								if (info.QueryId != queryId) {
									info.QueryId = queryId;
									_queryBuffer.Add(entity);
								}
							}

							nodeIndex = node.Next;
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

				for (var x = minIndex.X; x <= maxIndex.X; x++) {
					for (var y = minIndex.Y; y <= maxIndex.Y; y++) {
						var cell = FlatIndex(x, y);
						var nodeIndex = AllocateNode();

						Nodes[nodeIndex] = new Node { Entity = entity, Next = Heads[cell] };
						Heads[cell] = nodeIndex;
					}
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Remove(W.Entity entity, BroadPhaseInfo info) {
				var min = info.LowerBound;
				var max = info.UpperBound;

				var nodes = Nodes;

				for (var x = min.X; x <= max.X; x++) {
					for (var y = min.Y; y <= max.Y; y++) {
						var cell = FlatIndex(x, y);
						var nodeIndex = Heads[cell];
						var prevIndex = -1;

						while (nodeIndex != -1) {
							var node = nodes[nodeIndex];
							if (node.Entity == entity) {
								// Unlink from the singly-linked cell chain.
								if (prevIndex == -1) {
									Heads[cell] = node.Next;
								}
								else {
									nodes[prevIndex].Next = node.Next;
								}

								FreeNode(nodeIndex);   // leaves a reusable hole
								break;
							}

							prevIndex = nodeIndex;
							nodeIndex = node.Next;
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

			// Reuse a hole if one exists, otherwise bump the frontier (growing if full).
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

			// Turn a slot into a hole and push it onto the free list. The frontier
			// (UsedNodesCount) is intentionally left untouched.
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void FreeNode(int index) {
				Nodes[index].Entity = default;
				Nodes[index].Next = NextFreeNodeIndex;
				NextFreeNodeIndex = index;
			}
		}
	}
}
