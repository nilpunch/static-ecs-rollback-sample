using Shenanicode.Rollback;

namespace Game.Core {
	public class GameUpdateRoot : IUpdateRoot {
		public void Update(int tick) {
			Systems.Update();
		}
	}
}
