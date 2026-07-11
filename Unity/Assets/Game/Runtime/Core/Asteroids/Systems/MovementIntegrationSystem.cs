using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class MovementIntegrationSystem : ISystem {
			public void Update() {
				W.Query().For((ref PhysicalBody physicalBody, ref Velocity velocity) => {
					if (velocity.Angular > FAngle.HalfPI) {
						velocity.Angular = FAngle.HalfPI;
					}
					if (velocity.Angular < -FAngle.HalfPI) {
						velocity.Angular = -FAngle.HalfPI;
					}

					var linearDelta = velocity.Linear * Const.DeltaTime;
					var angularDelta = velocity.Angular * Const.DeltaTime;

					physicalBody.CenterOfMass += linearDelta;
					physicalBody.Rotation += angularDelta;
					physicalBody.WorldOrigin = physicalBody.OriginOffset.RotateAround(physicalBody.CenterOfMass, physicalBody.Rotation.Counterclockwise);
				});
			}
		}
	}
}
