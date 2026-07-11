using FFS.Libraries.StaticEcs;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class DestroySelfSystem : ISystem {
			public void Update() {
				W.Query<All<DestroySelf>>().BatchDestroy();
			}
		}
	}
}
