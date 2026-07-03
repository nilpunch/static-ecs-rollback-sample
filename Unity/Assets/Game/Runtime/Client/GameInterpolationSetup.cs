using Game.Core;
using static Game.Core.Core<Game.Client.ClientWorld>;

namespace Game.Client {
	public static class GameInterpolationSetup {
		public static void CreateAndInitialize() {
			WP.Create(GameWorldSetup.WorldConfig);
			WP.Types().RegisterAll(typeof(CoreRoot).Assembly);
			WP.Initialize();

			S.SetInterpolationReceiver(new GameInterpolationReceiver());
		}

		public static void Destroy() {
			WP.Destroy();
		}
	}
}
