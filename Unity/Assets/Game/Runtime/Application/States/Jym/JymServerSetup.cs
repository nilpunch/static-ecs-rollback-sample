using FFS.Libraries.StaticEcs;
using Game.Client;
using Shenanicode.Rollback;

namespace Game.Application {
	using static Core<ServerWorldType>;

	public struct ServerWorldType : IWorldType { }

	public abstract class SRVR : Server<SessionType> { }

	public static class JymServerSetup {
		public static void CreateAndInitialize(IRemoteClientListener remoteClientListener, int numberOfAsteroids) {
			SRVR.Create(GameSessionSetup.SessionConfig, remoteClientListener, new GameWorldFullSyncHandler(), logger: new UnityLogger("Server"));
			GameSessionSetup.Register();
			SRVR.Initialize();

			GameWorldSetup.CreateAndInitialize();

			SimulationSetup.PopulateAsteroids(numberOfAsteroids);

			App.Get<AppLocalServer>().IsLocalServer = true;
		}

		public static void Destroy() {
			if (SRVR.Status != SessionStatus.NotCreated) {
				GameWorldSetup.Destroy();
				SRVR.Destroy();
			}
		}
	}
}
