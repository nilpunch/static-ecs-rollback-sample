using FFS.Libraries.StaticEcs.Unity;
using Shenanicode.Rollback;
using static Game.Core<Game.Client.ClientWorldType>;

namespace Game.Client {
	public abstract class CLNT : Client<SessionType> { }

	public static class ClientSetup {
		public static void CreateAndInitialize(ServerConnection connection, TickSyncConfig tickSyncConfig = default, double maxResimulationMillis = 0) {
			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(),
				tickSyncConfig: tickSyncConfig,
				logger: new UnityLogger("Client"),
				maxResimulationMillis: maxResimulationMillis);
			GameSessionSetup.Register();
			CLNT.Initialize();

			GameWorldSetup.Create();
			GameWorldSetup.Register();
			EcsDebug<ClientWorldType>.AddWorld<GameSystemsType>();
			GameWorldSetup.Initialize();

			GameInterpolationSetup.CreateAndInitialize();
			ViewSynchronizer.Create();
		}

		public static void Destroy() {
			if (CLNT.Status != SessionStatus.NotCreated) {
				EcsDebug<ClientWorldType>.RemoveWorld();
				ViewSynchronizer.Destroy();
				GameInterpolationSetup.Destroy();
				GameWorldSetup.Destroy();
				CLNT.Destroy();
			}
		}
	}
}
