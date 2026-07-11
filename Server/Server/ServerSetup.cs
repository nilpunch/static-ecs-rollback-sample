using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;
using static Game.Core<ServerWorld>;

public struct ServerWorld : IWorldType, ISessionType { }

public abstract class SRVR : Server<ServerWorld> { }

public static class ServerSetup {
	public static void CreateAndInitialize(IRemoteClientListener remoteClientListener) {
		SRVR.Create(GameSessionSetup.SessionConfig, remoteClientListener, new GameWorldFullSyncHandler(), logger: new ConsoleLogger("Server"));
		GameSessionSetup.Register();
		SRVR.Initialize();

		GameWorldSetup.CreateAndInitialize();

		SimulationSetup.PopulateAsteroids(100);
	}

	public static void Destroy() {
		GameWorldSetup.Destroy();
		SRVR.Destroy();
	}
}
