using Game.Core;

namespace Game.Client
{
	public static class GameInterpolationSetup {
		public static void CreateAndInitialize() {
			WP.Create();
			WP.Types().RegisterAll(typeof(GameWorld).Assembly);
			WP.Initialize();

			S.SetInterpolationReceiver(new GameInterpolationReceiver());
		}

		public static void Destroy() {
			WP.Destroy();
		}
	}
}
