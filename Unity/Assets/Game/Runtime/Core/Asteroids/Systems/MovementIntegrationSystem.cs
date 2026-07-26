using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> {
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

					physicalBody.WorldCoM += linearDelta;
					physicalBody.WorldCoM = Const.Wrap(physicalBody.WorldCoM);
					physicalBody.Rotation += angularDelta;
				});
			}
		}
	}
}
