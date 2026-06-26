using Game.Core;
using Shenanicode.Rollback;

public abstract class SRVR : Server<GameWorld> { }

public static class ServerSetup {
	public static void CreateAndInitialize(IRemoteClientListener remoteClientListener) {
		SRVR.Create(GameSessionSetup.SessionConfig, remoteClientListener, new GameWorldFullSyncHandler(), logger: new ConsoleLogger("Server"));
		GameSessionSetup.Register();
		SRVR.Initialize();

		GameWorldSetup.CreateAndInitialize();
	}

	public static void Destroy() {
		GameWorldSetup.Destroy();
		SRVR.Destroy();
	}
}
