using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation.Asteroids {
	public class AsteroidColliderSyncSystem : ISystem {
		public void Update() {
			W.Query<None<DestroySelf>>().For(static (ref Collider collider, in AsteroidCellCollider cellCollider, in W.Link<AsteroidBodyLink> bodyLink) => {
				if (!bodyLink.Value.TryUnpack<GameWorld>(out var body)) {
					return;
				}

				if (!body.Has<Asteroid>()) {
					return;
				}

				ref readonly var asteroid = ref body.Read<Asteroid>();
				ref readonly var position = ref body.Read<Position>();
				ref readonly var rotation = ref body.Read<Rotation>();

				collider.Offset = cellCollider.LocalOffset;
				collider.Radius = asteroid.CellRadius;
				collider.WorldPosition = position.Value + cellCollider.LocalOffset.RotateAround(FVector2.Zero, rotation.Value.Counterclockwise);
			});
		}
	}
}
