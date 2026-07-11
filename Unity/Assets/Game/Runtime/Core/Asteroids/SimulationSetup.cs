using FFS.Libraries.StaticEcs;
using Fixed32;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public static class SimulationSetup {
			public static void Register() {
				Const.DeltaTime = FP.One / S.TickRate;
				Const.InvDeltaTime = S.TickRate.ToFP();

				W.SetResource(new BroadPhase(200, 100, FVector2.One));

				Systems.Add(new MovementIntegrationSystem());
				Systems.Add(new ColliderWorldPositionSyncSystem());
				Systems.Add(new BroadPhaseSystem());
				Systems.Add(new DestroySelfSystem());
			}
		}
	}
}
