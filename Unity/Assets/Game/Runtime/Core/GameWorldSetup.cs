using FFS.Libraries.StaticEcs;
using Game.Core.Simulation;

namespace Game.Core {
	public static class GameWorldSetup {
		public static WorldConfig WorldConfig => new() {
			TrackingBufferSize = 2,
		};

		public static void CreateAndInitialize() {
			W.Create(WorldConfig);
			Systems.Create();

			W.Types().RegisterAll();
			SimulationSetup.Register();

			W.Initialize();
			Systems.Initialize();
		}

		public static void Destroy() {
			Systems.Destroy();
			W.Destroy();
		}
	}
}
