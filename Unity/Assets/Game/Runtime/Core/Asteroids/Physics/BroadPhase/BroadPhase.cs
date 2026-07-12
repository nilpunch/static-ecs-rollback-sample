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

			// Candidate collision pairs collected in a single pass over non-empty cells.
			public readonly List<(W.Entity A, W.Entity B)> Pairs = new();
			private readonly List<(W.Entity Entity, CellIndex Lo)> _cellScratch = new();

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

			// Dense list of non-empty cell flat indices, with an inverse map for O(1)
			// removal (sparse set). Lets collision detection walk only occupied cells
			// instead of scanning the whole Width*Height grid.
			public int[] ActiveCells;
			public int ActiveCellCount;
			private int[] _cellSlot;

			public int UsedNodesCount;
			public int NextFreeNodeIndex;
			public uint QueryId;

			public BroadPhase(int width, int height, FVector2 cellSize) {
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

				// Only the dense active list is serialized; _cellSlot is its inverse and
				// is rebuilt on Read, so it never bloats the per-frame snapshot.
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

				// Reconstruct the inverse map for the restored active cells (O(active),
				// not O(grid)). Slots of inactive cells are never read, so stale values
				// left behind are harmless.
				for (var i = 0; i < ActiveCellCount; i++) {
					_cellSlot[ActiveCells[i]] = i;
				}
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

			/// <summary>
			/// Collects every candidate collision pair in a single sweep over non-empty
			/// cells. Each unordered pair is emitted exactly once (ordered A.ID &lt; B.ID),
			/// so callers need no further deduplication.
			/// </summary>
			public void CollectPairs() {
				Pairs.Clear();

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
						ref var info = ref W.Components<BroadPhaseInfo>.Instance.Ref(entity)!;
						scratch.Add((entity, info.LowerBound));
						nodeIndex = nodes[nodeIndex].Next;
					}

					var count = scratch.Count;
					for (var i = 0; i < count; i++) {
						var (entityA, loA) = scratch[i];
						for (var j = i + 1; j < count; j++) {
							var (entityB, loB) = scratch[j];

							// Two entities that overlap always share the cell at the min
							// corner of their overlap. Emit only from that owner cell so a
							// pair spanning several shared cells is not emitted repeatedly.
							if (cellX != MathUtils.Max(loA.X, loB.X) || cellY != MathUtils.Max(loA.Y, loB.Y)) {
								continue;
							}

							Pairs.Add(entityA.ID < entityB.ID ? (entityA, entityB) : (entityB, entityA));
						}
					}
				}
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

				var x = FP.FloorToInt(shiftedX * InvertedCellSize.X);
				var y = FP.FloorToInt(shiftedY * InvertedCellSize.Y);

				var clampedX = MathUtils.Min(Width - 1, MathUtils.Max(0, x));
				var clampedY = MathUtils.Min(Height - 1, MathUtils.Max(0, y));

				CellIndex cellIndex = default;
				cellIndex.X = clampedX;
				cellIndex.Y = clampedY;

				return cellIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int FlatIndex(int x, int y) {
				return x + y * Width;
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

				for (var x = minIndex.X; x <= maxIndex.X; x++) {
					for (var y = minIndex.Y; y <= maxIndex.Y; y++) {
						var cell = FlatIndex(x, y);
						if (Heads[cell] == -1) {
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

						if (Heads[cell] == -1) {
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
