using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Core {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class DestroySelfSystem : ISystem {
			public void Update() {
				W.Query<All<DestroySelf>>().BatchDestroy();
			}
		}
	}
}
