using Shenanicode.Rollback;

namespace Game.Core
{
	public static class GameSessionSetup {
		public static SessionConfig SessionConfig => new(tickRate: 60);

		public static void Register() {
			S.Types().RegisterAll();
		}
	}
}
