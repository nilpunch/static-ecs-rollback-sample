namespace Game.Core {
	public static class GameWorldSetup {
		public static void CreateAndInitialize() {
			W.Create();
			Systems.Create();

			W.Types().RegisterAll();

			W.Initialize();
			Systems.Initialize();
		}

		public static void Destroy() {
			Systems.Destroy();
			W.Destroy();
		}
	}
}
