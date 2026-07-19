using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public static class SimulationSetup {
			private static int GridWidth = 512;
			private static int GridHeight = 512;
			private static FVector2 CellSize = FVector2.One * 2;
			private static FVector2 WorldSize =
				new FVector2(GridWidth.ToFP(), GridHeight.ToFP()) * CellSize;

			public static void Register() {
				Const.DeltaTime = FP.One / Const.TickRate;
				Const.InvDeltaTime = Const.TickRate.ToFP();

				W.SetResource(new BroadPhase(GridWidth, GridHeight, CellSize));
				W.SetResource(new PlayerMapping());

				Const.SetWorldSize(WorldSize);

				Systems.Add(new PlayerCreationSystem());
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
						FP.FromRaw(random.Next(-WorldSize.X.RawValue, WorldSize.X.RawValue)),
						FP.FromRaw(random.Next(-WorldSize.Y.RawValue, WorldSize.Y.RawValue))
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
