using FFS.Libraries.StaticEcs;
using Game.Client;
using Shenanicode.Rollback;
using static Game.Core.Core<Game.Application.ServerWorld>;

namespace Game.Application {
	public struct ServerWorld : IWorldType, ISessionType { }

	public abstract class SRVR : Server<ServerWorld> { }

	public static class JymServerSetup {
		public static void CreateAndInitialize(IRemoteClientListener remoteClientListener) {
			SRVR.Create(GameSessionSetup.SessionConfig, remoteClientListener, new GameWorldFullSyncHandler(), logger: new UnityLogger("Server"));
			GameSessionSetup.Register();
			SRVR.Initialize();

			GameWorldSetup.CreateAndInitialize();
		}

		public static void Destroy() {
			if (SRVR.Status != SessionStatus.NotCreated) {
				GameWorldSetup.Destroy();
				SRVR.Destroy();
			}
		}
	}
}
