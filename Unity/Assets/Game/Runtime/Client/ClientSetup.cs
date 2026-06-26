using Game.Core;
using Shenanicode.Rollback;

namespace Game.Client
{
	public abstract class CLNT : Client<GameWorld> { }

	public static class ClientSetup {
		public static void CreateAndInitialize(ServerConnection connection) {
			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(), logger: new UnityLogger("Client"));
			GameSessionSetup.Register();
			CLNT.Initialize();

			GameWorldSetup.CreateAndInitialize();
			GameInterpolationSetup.CreateAndInitialize();
		}

		public static void Destroy() {
			if (CLNT.IsCreated) {
				GameInterpolationSetup.Destroy();
				GameWorldSetup.Destroy();
				CLNT.Destroy();
			}
		}
	}
}
