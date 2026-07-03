using System;
using System.Buffers;
using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation.Asteroids {
	public class AsteroidRebuildSystem : ISystem {
		private byte[] _occupancy = Array.Empty<byte>();
		private byte[] _visited = Array.Empty<byte>();
		private int[] _queue = Array.Empty<int>();
		private AsteroidCell[] _orderedCells = Array.Empty<AsteroidCell>();
		private W.Entity[] _entityMap = Array.Empty<W.Entity>();
		private readonly List<ComponentSlice> _slices = new List<ComponentSlice>();

		public void Update() {
			foreach (var body in W.Query<All<AsteroidDirty>>().Entities()) {
				ref var cells = ref body.Mut<W.Multi<AsteroidCell>>();
				ref readonly var asteroid = ref body.Read<Asteroid>();
				ref readonly var position = ref body.Read<Position>();
				ref readonly var physicalBody = ref body.Read<PhysicalBody>();
				ref readonly var rotation = ref body.Read<Rotation>();
				ref readonly var angularVelocity = ref body.Read<AngularVelocity>();

				RebuildBody(body, in asteroid, in position, in physicalBody, in rotation, in angularVelocity, ref cells);
			}
		}

		private void RebuildBody(W.Entity body, in Asteroid asteroid, in Position position, in PhysicalBody physicalBody, in Rotation rotation, in AngularVelocity angularVelocity, ref W.Multi<AsteroidCell> cells) {
			_slices.Clear();
			var cellCount = cells.Length;
			if (cellCount == 0) {
				DestroyAllChildren(body);
				body.Set<DestroySelf>();
				body.Delete<AsteroidDirty>();
				return;
			}

			var gridSize = asteroid.Width * asteroid.Height;
			EnsureBufferSize(gridSize, cellCount);

			Array.Clear(_occupancy, 0, gridSize);
			Array.Clear(_visited, 0, gridSize);
			Array.Clear(_entityMap, 0, gridSize);

			var sourceCells = cells.AsReadOnlySpan;
			for (var i = 0; i < sourceCells.Length; i++) {
				var cell = sourceCells[i];
				if (InBounds(cell.X, cell.Y, asteroid)) {
					_occupancy[AsteroidMath.IndexOf(cell.X, cell.Y, asteroid.Width)] = 1;
				}
			}

			if (body.Has<W.Links<AsteroidChildren>>()) {
				ref readonly var children = ref body.Read<W.Links<AsteroidChildren>>();
				for (var i = 0; i < children.Length; i++) {
					if (children[i].Value.TryUnpack<GameWorld>(out var child)) {
						if (child.Has<AsteroidCellCollider>()) {
							ref readonly var cellCollider = ref child.Read<AsteroidCellCollider>();
							_entityMap[AsteroidMath.IndexOf(cellCollider.X, cellCollider.Y, asteroid.Width)] = child;
						}
					}
				}
			}

			var orderedCount = 0;
			for (var y = 0; y < asteroid.Height; y++) {
				for (var x = 0; x < asteroid.Width; x++) {
					var index = AsteroidMath.IndexOf(x, y, asteroid.Width);
					if (_occupancy[index] == 0 || _visited[index] != 0) continue;

					var start = orderedCount;
					var head = 0;
					var tail = 0;
					_queue[tail++] = index;
					_visited[index] = 1;

					while (head < tail) {
						var current = _queue[head++];
						var cx = current % asteroid.Width;
						var cy = current / asteroid.Width;

						_orderedCells[orderedCount++] = new AsteroidCell { X = (short)cx, Y = (short)cy };

						EnqueueNeighbor(cx - 1, cy, asteroid, _occupancy, _visited, _queue, ref tail);
						EnqueueNeighbor(cx + 1, cy, asteroid, _occupancy, _visited, _queue, ref tail);
						EnqueueNeighbor(cx, cy - 1, asteroid, _occupancy, _visited, _queue, ref tail);
						EnqueueNeighbor(cx, cy + 1, asteroid, _occupancy, _visited, _queue, ref tail);
					}

					_slices.Add(new ComponentSlice(start, orderedCount - start));
				}
			}

			if (_slices.Count == 0) {
				DestroyAllChildren(body);
				body.Set<DestroySelf>();
				body.Delete<AsteroidDirty>();
				return;
			}

			var primaryIndex = 0;
			var primaryLength = _slices[0].Length;
			for (var i = 1; i < _slices.Count; i++) {
				if (_slices[i].Length > primaryLength) {
					primaryIndex = i;
					primaryLength = _slices[i].Length;
				}
			}

			for (var i = 0; i < _slices.Count; i++) {
				var slice = _slices[i];
				W.Entity targetBody;
				if (i == primaryIndex) {
					targetBody = body;
				} else {
					targetBody = W.NewEntity<Default>();
					targetBody.Set(
						new Asteroid {
							Width = asteroid.Width,
							Height = asteroid.Height,
							CellSpacing = asteroid.CellSpacing,
							CellRadius = asteroid.CellRadius,
						},
						new Position { Value = position.Value },
						new Velocity() { },
						new Rotation { Value = rotation.Value },
						new AngularVelocity { Value = angularVelocity.Value }
					);
					targetBody.Set<AsteroidDirty>();
				}

				ref var targetCells = ref targetBody.Mut<W.Multi<AsteroidCell>>();
				targetCells.Clear();

				if (!targetBody.Has<W.Links<AsteroidChildren>>()) {
					targetBody.Add<W.Links<AsteroidChildren>>();
				}
				ref var targetChildren = ref targetBody.Mut<W.Links<AsteroidChildren>>();
				targetChildren.Clear();

				for (var j = 0; j < slice.Length; j++) {
					var cell = _orderedCells[slice.Start + j];
					targetCells.Add(cell);

					var index = AsteroidMath.IndexOf(cell.X, cell.Y, asteroid.Width);
					var collider = _entityMap[index];

					if (collider.GID == default) {
						var localOffset = AsteroidMath.CellOffset(in asteroid, cell.X, cell.Y);
						var rotatedLocalOffset = localOffset.RotateAround(FVector2.Zero, rotation.Value.Counterclockwise);

						collider = W.NewEntity<Default>();
						collider.Set(
							new Collider {
								Offset = localOffset,
								WorldPosition = position.Value + rotatedLocalOffset,
								Radius = asteroid.CellRadius,
							},
							new AsteroidCellCollider {
								X = cell.X,
								Y = cell.Y,
								LocalOffset = localOffset,
							}
						);
					} else {
						_entityMap[index] = default;
					}

					collider.Set(new W.Link<AsteroidBodyLink>(targetBody.GID));
					targetChildren.TryAdd(collider.AsLink<AsteroidChildren>());
				}
			}

			for (var i = 0; i < gridSize; i++) {
				if (_entityMap[i].GID != default) {
					_entityMap[i].Set<DestroySelf>();
				}
			}

			body.Delete<AsteroidDirty>();
		}

		private void EnsureBufferSize(int gridSize, int cellCount) {
			if (_occupancy.Length < gridSize) _occupancy = new byte[gridSize];
			if (_visited.Length < gridSize) _visited = new byte[gridSize];
			if (_queue.Length < gridSize) _queue = new int[gridSize];
			if (_orderedCells.Length < cellCount) _orderedCells = new AsteroidCell[cellCount];
			if (_entityMap.Length < gridSize) _entityMap = new W.Entity[gridSize];
		}

		private static void DestroyAllChildren(W.Entity body) {
			if (!body.Has<W.Links<AsteroidChildren>>()) return;
			ref readonly var children = ref body.Read<W.Links<AsteroidChildren>>();
			for (var i = 0; i < children.Length; i++) {
				if (children[i].Value.TryUnpack<GameWorld>(out var child)) {
					child.Set<DestroySelf>();
				}
			}
		}

		private static bool InBounds(int x, int y, in Asteroid asteroid) {
			return x >= 0 && y >= 0 && x < asteroid.Width && y < asteroid.Height;
		}

		private static void EnqueueNeighbor(int x, int y, in Asteroid asteroid, byte[] occupancy, byte[] visited, int[] queue, ref int tail) {
			if (!InBounds(x, y, asteroid)) return;
			var index = AsteroidMath.IndexOf(x, y, asteroid.Width);
			if (occupancy[index] == 0 || visited[index] != 0) return;
			visited[index] = 1;
			queue[tail++] = index;
		}

		private readonly struct ComponentSlice {
			public readonly int Start;
			public readonly int Length;
			public ComponentSlice(int start, int length) {
				Start = start;
				Length = length;
			}
		}
	}
}
