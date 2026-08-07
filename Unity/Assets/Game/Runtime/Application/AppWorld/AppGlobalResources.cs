using Game.Client;
using Game.Utils;

namespace Game.Application {
	public static class AppGlobalResources {
		public static void SetResources() {
			ResourceConfigUtils.SetResourceConfigs<AppWorldType>();

			App.Set(new AppServerConnection());
			App.Set(new AppLocalServer());
		}

		public static void Dispose() {
			App.Get<AppServerConnection>().Connection.Close();
		}
	}
}
