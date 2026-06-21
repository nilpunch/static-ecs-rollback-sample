using Game.Core;

namespace Game.Client
{
	public static class GameInterpolationSetup {
		public static void Register() {
			S.SetInterpolationReceiver(new GameInterpolationReceiver());

			WP.Create();
			WP.Types().RegisterAll(typeof(GameWorld).Assembly);
			WP.Initialize();
		}
	}
}
