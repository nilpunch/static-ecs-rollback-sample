using FFS.Libraries.StaticEcs;
using Game.Core;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public static class GameSessionSetup {
			public static SessionConfig SessionConfig => new(tickRate: Const.TickRate, saveEachNthTick: 2);

			public static void Register() {
				S.SetUpdateRoot(new GameUpdateRoot());
				S.SetRollback(new GameWorldRollback(S.FramesCapacity));
				S.Types().RegisterAll(typeof(CoreRoot).Assembly);
			}
		}
	}
}
