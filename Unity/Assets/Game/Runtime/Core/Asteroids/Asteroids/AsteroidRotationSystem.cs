using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation.Asteroids {
	public class AsteroidRotationSystem : ISystem {
		public void Update() {
			W.Query().For(static (ref Rotation rotation, in AngularVelocity angularVelocity) => {
				rotation.Value += angularVelocity.Value;
			});
		}
	}
}
