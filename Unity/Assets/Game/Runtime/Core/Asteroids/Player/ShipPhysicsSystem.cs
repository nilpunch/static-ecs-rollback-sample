using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class ShipPhysicsSystem : ISystem {
			private struct Config {
				public FAngle TurnStabilization;
				public FP ThrusterOffset;
				public FP MomentOfInertia;
			}

			private Config _config;

			public ShipPhysicsSystem(FAngle turnStabilization, FP thrusterOffset, FP momentOfInertia) {
				_config = new Config {
					TurnStabilization = turnStabilization,
					ThrusterOffset = thrusterOffset,
					MomentOfInertia = momentOfInertia
				};
			}

			public void Update() {
				W.Query().For(_config,
					static (ref Config config, ref Velocity velocity, in PhysicalBody body, in Ship ship) => {

						var forward = body.Rotation.Counterclockwise * FVector2.Right;

						if (ship.Turn == FAngle.Zero) {
							velocity.Angular = FAngle.MoveTowards(velocity.Angular, FAngle.Zero, config.TurnStabilization * Const.DeltaTime);
						} else {
							var perpendicular = FVector2.Orthogonal(forward);
							var force = perpendicular * ship.Turn.Radians;
							var lever = forward * config.ThrusterOffset;
							var torque = FVector2.Cross(lever, force);

							velocity.Angular += FAngle.FromRadians(torque / config.MomentOfInertia) * Const.DeltaTime;
							velocity.Linear += force * Const.DeltaTime;
						}

						velocity.Linear += forward * ship.Thrust * Const.DeltaTime;
					});
			}
		}
	}
}
