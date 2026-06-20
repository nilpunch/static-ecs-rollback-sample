using Shenanicode.Rollback;

namespace Game.Core {
	public static class GameSessionSetup {
		public static SessionConfig SessionConfig => new(tickRate: 60);

		public static void Register() {
			S.AddUpdateRoot(new GameUpdateRoot());
			S.AddRollback(new GameRollback(S.FramesCapacity));
			S.Types().RegisterAll();
		}
	}
}
