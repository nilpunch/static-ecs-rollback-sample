using FFS.Libraries.StaticEcs;
using Game.Client;
using Shenanicode.Rollback;

namespace Game.Application {
	using static Core<ServerWorld>;

	public struct ServerWorld : IWorldType, ISessionType { }

	public abstract class SRVR : Server<ServerWorld> { }

	public static class JymServerSetup {
		public static void CreateAndInitialize(IRemoteClientListener remoteClientListener, int numberOfAsteroids) {
			SRVR.Create(GameSessionSetup.SessionConfig, remoteClientListener, new GameWorldFullSyncHandler(), logger: new UnityLogger("Server"));
			GameSessionSetup.Register();
			SRVR.Initialize();

			GameWorldSetup.CreateAndInitialize();

			SimulationSetup.PopulateAsteroids(numberOfAsteroids);
		}

		public static void Destroy() {
			if (SRVR.Status != SessionStatus.NotCreated) {
				GameWorldSetup.Destroy();
				SRVR.Destroy();
			}
		}
	}
}
