using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Client {
	using static Core<ClientWorld>;

	public struct ClientWorld : IWorldType, ISessionType { }

	public abstract class CLNT : Client<ClientWorld> { }

	public static class ClientSetup {
		public static void CreateAndInitialize(ServerConnection connection) {
			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(), logger: new UnityLogger("Client"));
			GameSessionSetup.Register();
			CLNT.Initialize();

			GameWorldSetup.CreateAndInitialize();
			GameInterpolationSetup.CreateAndInitialize();
		}

		public static void Destroy() {
			if (CLNT.Status != SessionStatus.NotCreated) {
				GameInterpolationSetup.Destroy();
				GameWorldSetup.Destroy();
				CLNT.Destroy();
			}
		}
	}
}
