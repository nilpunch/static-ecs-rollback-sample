namespace Game.Client
{
	public static class AppResources {
		public static void SetResources() {
			App.Set(new ServerConnection());
		}

		public static void DisposeResources() {
			App.Get<ServerConnection>().Connection.Close();
		}
	}
}
