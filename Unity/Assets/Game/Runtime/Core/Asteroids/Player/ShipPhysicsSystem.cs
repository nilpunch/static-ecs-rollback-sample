using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class ShipPhysicsSystem : ISystem {
			private readonly FAngle _turnStabilization;

			public ShipPhysicsSystem(FAngle turnStabilization) {
				_turnStabilization = turnStabilization;
			}

			public void Update() {
				W.Query().For(_turnStabilization,
					static (ref FAngle turnStabilization, ref Velocity velocity, in PhysicalBody body, in Ship ship) => {

						if (ship.Turn == FAngle.Zero) {
							velocity.Angular = FAngle.MoveTowards(velocity.Angular, FAngle.Zero, turnStabilization * Const.DeltaTime);
						}
						else {
							velocity.Angular += ship.Turn * Const.DeltaTime;
						}

						var direction = body.Rotation.Counterclockwise * FVector2.Right;
						velocity.Linear += direction * ship.Thrust * Const.DeltaTime;
					});
			}
		}
	}
}
