using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation.Asteroids {
	public class AsteroidDamageSystem : ISystem {
		public void Update() {
			W.Query<All<DestroySelf>>().For(
				static (in AsteroidCellCollider colliderCell, in W.Link<AsteroidBodyLink> bodyLink) => {
					if (!bodyLink.Value.TryUnpack<GameWorld>(out var body)) {
						return;
					}

					if (!body.Has<W.Multi<AsteroidCell>>()) {
						return;
					}

					ref var cells = ref body.Mut<W.Multi<AsteroidCell>>();
					if (RemoveCell(ref cells, colliderCell.X, colliderCell.Y)) {
						body.Set<AsteroidDirty>();
					}
				});
		}

		private static bool RemoveCell(ref W.Multi<AsteroidCell> cells, short x, short y) {
			var span = cells.AsSpan;
			for (var i = 0; i < span.Length; i++) {
				if (span[i].X == x && span[i].Y == y) {
					cells.RemoveAtSwap(i);
					return true;
				}
			}

			return false;
		}
	}
}
