using Shenanicode.Rollback;
using static Game.Core<Game.Client.ClientWorld>;

namespace Game.Client {
	public abstract class CLNT : Client<ClientWorld> { }

	public static class ClientSetup {
		public static void CreateAndInitialize(ServerConnection connection, TickSyncConfig tickSyncConfig = default) {
			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(),
				tickSyncConfig: tickSyncConfig,
				logger: new UnityLogger("Client"));
			GameSessionSetup.Register();
			CLNT.Initialize();

			GameWorldSetup.CreateAndInitialize();
			GameInterpolationSetup.CreateAndInitialize();
			ViewSynchronizer.Create();
		}

		public static void Destroy() {
			if (CLNT.Status != SessionStatus.NotCreated) {
				ViewSynchronizer.Destroy();
				GameInterpolationSetup.Destroy();
				GameWorldSetup.Destroy();
				CLNT.Destroy();
			}
		}
	}
}
