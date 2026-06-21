using Shenanicode.Rollback;

namespace Game.Core {
	public static class GameSessionSetup {
		public static SessionConfig SessionConfig => new(tickRate: 60);

		public static void Register() {
			S.SetUpdateRoot(new GameUpdateRoot());
			S.SetRollback(new GameWorldRollback(S.FramesCapacity));
			S.Types().RegisterAll();
		}
	}
}
