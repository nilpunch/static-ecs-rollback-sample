using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation {
	public class BroadPhaseSystem : ISystem {
		public void Update() {
			var broadPhase = W.GetResource<BroadPhase>();

			// Add new.
			W.Query<None<DestroySelf, BroadPhaseInfo>>().For(ref broadPhase,
				static (ref BroadPhase broadPhase, W.Entity entity, in Collider collider) => {
					ref var broadPhaseInfo = ref entity.Add<BroadPhaseInfo>();
					broadPhase.Insert(entity, ref broadPhaseInfo, collider.WorldBounds);
				});

			// Remove dying.
			W.Query<Or<AllWithDisabled<Collider>, All<DestroySelf>>>().For(ref broadPhase,
				static (ref BroadPhase broadPhase, W.Entity entity, in BroadPhaseInfo info) => {
					broadPhase.Remove(entity, info);
				});

			// Update existing.
			W.Query<AllChanged<Collider>>().For(ref broadPhase,
				static (ref BroadPhase broadPhase, W.Entity entity, ref BroadPhaseInfo info, in Collider collider) => {
					broadPhase.UpdateInfo(entity, ref info, collider.WorldBounds);
				});
		}
	}
}
