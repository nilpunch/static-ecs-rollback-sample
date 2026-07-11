using Game.Core;

namespace Game.Client {
	using static Core<ClientWorld>;

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
