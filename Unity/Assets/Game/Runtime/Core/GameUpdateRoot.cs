using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class GameUpdateRoot : IUpdateRoot {
			public void Update(int tick) {
				Systems.Update();
			}
		}
	}
}
