using Fixed32;

namespace Game.Core.Simulation {
	public static class SimulationSetup {
		public static void Register() {
			W.SetResource(new BroadPhase(200, 100, FVector2.One));

			Systems.Add(new MoveSystem());
			Systems.Add(new BroadPhaseSystem());
		}
	}
}
