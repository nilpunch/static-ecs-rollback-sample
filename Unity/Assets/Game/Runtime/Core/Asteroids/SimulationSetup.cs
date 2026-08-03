using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> {
		public static class SimulationSetup {
			public static void Register() {
				W.SetResource(new BroadPhase(Const.GridWidth, Const.GridHeight, Const.CellSize));
				W.SetResource(new PlayerMapping());

				Systems.Add(new PlayerSpawnSystem());
				Systems.Add(new ShipControlSystem(maxThrust: 20.ToFP(), maxBrake: 5.ToFP(), maxTurn: FAngle.FromRadians(5.ToFP())));
				Systems.Add(new ShipPhysicsSystem(turnStabilization: FAngle.FromRadians(2.ToFP()), thrusterOffset: FP.FromRatio(2, 1), momentOfInertia: FP.FromRatio(5, 10)));
				Systems.Add(new MovementIntegrationSystem());
				Systems.Add(new ColliderWorldPositionSyncSystem());
				Systems.Add(new BroadPhaseSystem());
				Systems.Add(new CollisionResolutionSystem());
				Systems.Add(new DebugPushAroundSystem());
				Systems.Add(new DestroySelfSystem());
			}

			public static void PopulateAsteroids(int asteroidCount) {
				var random = new System.Random(42);
				for (int i = 0; i < asteroidCount; i++) {
					var entity = W.NewEntity<Default>();

					var pos = new FVector2(
						FP.FromRaw(random.Next(-Const.WorldSize.X.RawValue, Const.WorldSize.X.RawValue)),
						FP.FromRaw(random.Next(-Const.WorldSize.Y.RawValue, Const.WorldSize.Y.RawValue))
					);

					var radius = FP.FromRatio(random.Next(1, 3), 1);

					entity.Set(new PhysicalBody {
						WorldCoM = pos,
						Rotation = FAngle.FromRadians(FP.FromRatio(random.Next(0, 628), 100))
					});

					entity.Set(new Velocity {
						Linear = new FVector2(
							FP.FromRatio(random.Next(-5, 5), 10),
							FP.FromRatio(random.Next(-5, 5), 10)
						),
						Angular = FAngle.FromRadians(FP.FromRatio(random.Next(-30, 30), 100))
					});

					entity.Set(new Collider {
						Radius = radius,
						WorldPosition = pos
					});

					entity.Set(Bounds.New(pos, FVector2.One * radius));

					entity.Set(new ViewAsset((short)ViewAssetTypes.Asteroid));
				}
			}
		}
	}
}
