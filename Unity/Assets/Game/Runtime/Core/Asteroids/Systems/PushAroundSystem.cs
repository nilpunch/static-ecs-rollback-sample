using FFS.Libraries.StaticEcs;
using Fixed;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class DebugPushAroundSystem : ISystem {
			public void Update() {
				var debugInput = S.GetInput<DebugInput>(0).LastFresh();

				if (debugInput.Acceleration <= FP.Epsilon) {
					return;
				}

				var nearbyEntities = W.GetResource<BroadPhase>().FindNearbyEntities(FAABB2.FromCenterAndExtents(debugInput.MousePosition, debugInput.Radius * FVector2.One));
				foreach (var entity in nearbyEntities) {
					if (entity.Has<Collider, Velocity>()) {
						var delta = Const.MinImageDelta(entity.Read<Collider>().WorldPosition - debugInput.MousePosition);
						var distSqr = Fixed64.FVector2.LengthSqr(delta.To64());
						var radiusSum = debugInput.Radius + entity.Read<Collider>().Radius;

						if (distSqr < (radiusSum * radiusSum).To64()) {
							entity.Mut<Velocity>().Linear += FVector2.NormalizeSafe(delta) * debugInput.Acceleration * Const.DeltaTime;
						}
					}
				}
			}
		}
	}
}
