namespace Game.Client {
	public static class GameInterpolationSetup {
		public static void CreateAndInitialize() {
			Core<GameWorldPrev>.GameWorldSetup.CreateAndInitialize();

			Core<ClientWorldType>.S.SetInterpolationReceiver(new GameInterpolationReceiver());
		}

		public static void Destroy() {
			Core<GameWorldPrev>.GameWorldSetup.Destroy();
		}
	}
}
