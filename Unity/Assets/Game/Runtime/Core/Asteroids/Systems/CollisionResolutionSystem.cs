using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation {
	public class CollisionResolutionSystem : ISystem {
		public void Update() {
			W.Query().For(ref W.GetResource<BroadPhase>(),
				(ref BroadPhase broadPhase, in Collider collider) => {

				});
		}
	}
}
