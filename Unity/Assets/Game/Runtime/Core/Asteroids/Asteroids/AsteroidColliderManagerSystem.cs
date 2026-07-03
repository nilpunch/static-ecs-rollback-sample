using System;
using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation.Asteroids {
	public class AsteroidColliderManagerSystem : ISystem {
		private byte[] _occupancy = Array.Empty<byte>();

		public void Update() {
			foreach (var body in W.Query<All<AsteroidDirty, W.Links<AsteroidChildren>>>().Entities()) {
				ref readonly var asteroid = ref body.Read<Asteroid>();
				ref readonly var cells = ref body.Read<W.Multi<AsteroidCell>>();
				ref readonly var children = ref body.Read<W.Links<AsteroidChildren>>();

				if (children.Length == 0) continue;

				var gridSize = asteroid.Width * asteroid.Height;
				if (_occupancy.Length < gridSize) {
					_occupancy = new byte[gridSize];
				}

				Array.Clear(_occupancy, 0, gridSize);
				var cellsSpan = cells.AsReadOnlySpan;
				for (int i = 0; i < cellsSpan.Length; i++) {
					_occupancy[AsteroidMath.IndexOf(cellsSpan[i].X, cellsSpan[i].Y, asteroid.Width)] = 1;
				}

				for (int i = 0; i < children.Length; i++) {
					if (children[i].Value.TryUnpack<GameWorld>(out var child)) {
						if (!child.Has<AsteroidCellCollider>()) continue;
						ref readonly var cellCollider = ref child.Read<AsteroidCellCollider>();
						bool isShell = IsShellCell(cellCollider.X, cellCollider.Y, in asteroid, _occupancy);
						if (isShell) {
							child.Enable<Collider>();
						} else {
							child.Disable<Collider>();
						}
					}
				}
			}
		}

		private static bool IsShellCell(short x, short y, in Asteroid asteroid, byte[] occupancy) {
			if (IsEmpty(x - 1, y, asteroid, occupancy)) return true;
			if (IsEmpty(x + 1, y, asteroid, occupancy)) return true;
			if (IsEmpty(x, y - 1, asteroid, occupancy)) return true;
			if (IsEmpty(x, y + 1, asteroid, occupancy)) return true;
			return false;
		}

		private static bool IsEmpty(int x, int y, in Asteroid asteroid, byte[] occupancy) {
			if (x < 0 || y < 0 || x >= asteroid.Width || y >= asteroid.Height) return true;
			return occupancy[AsteroidMath.IndexOf(x, y, asteroid.Width)] == 0;
		}
	}
}
