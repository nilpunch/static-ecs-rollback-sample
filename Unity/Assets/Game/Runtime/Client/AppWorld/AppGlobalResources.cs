using Game.Utils;

namespace Game.Client
{
	public static class AppGlobalResources {
		public static void SetResources() {
			ResourceConfigUtils.SetResourceConfigs<AppWorldType>();

			App.Set(new AppServerConnection());
		}

		public static void Dispose() {
			App.Get<AppServerConnection>().Connection.Close();
		}
	}
}
