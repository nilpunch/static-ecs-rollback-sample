using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class GameUpdateRoot : IUpdateRoot {
			public void Update(int tick) {
				Systems.Update();
				W.Tick();
			}
		}
	}
}
