namespace Game.Client {
	public static class GameInterpolationSetup {
		public static void CreateAndInitialize() {
			Core<GameWorldPrev>.GameWorldSetup.CreateAndInitialize();

			Core<ClientWorld>.S.SetInterpolationReceiver(new GameInterpolationReceiver());
		}

		public static void Destroy() {
			WP.Destroy();
		}
	}
}
