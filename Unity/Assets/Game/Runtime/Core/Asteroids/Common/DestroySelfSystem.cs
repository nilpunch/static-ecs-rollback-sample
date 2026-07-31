using FFS.Libraries.StaticEcs;
using Game.Core;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class DestroySelfSystem : ISystem {
			public void Update() {
				W.Query<All<DestroySelf>>().BatchDestroy();
			}
		}
	}
}
