namespace Game.Core
{
	public static class GameWorldSetup {
		public static void Create() {
			W.Create();
			Systems.Create();
		}

		public static void Register() {
			W.Types().RegisterAll();
		}

		public static void Initialize() {
			W.Initialize();
			Systems.Initialize();
		}

		public static void Destroy() {
			Systems.Destroy();
			W.Destroy();
		}
	}
}
