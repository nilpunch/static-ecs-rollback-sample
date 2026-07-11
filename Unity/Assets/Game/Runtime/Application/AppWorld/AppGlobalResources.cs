using Game.Client;
using Game.Utils;

namespace Game.Application {
	public static class AppGlobalResources {
		public static void SetResources() {
			ResourceConfigUtils.SetResourceConfigs<AppWorldType>();

			App.Set(new AppServerConnection());
			App.Set(new ViewSynchronizer());
		}

		public static void Dispose() {
			App.Get<AppServerConnection>().Connection.Close();
			App.Get<ViewSynchronizer>().Dispose();
			ViewTransformInterpolator.Dispose();
		}
	}
}
