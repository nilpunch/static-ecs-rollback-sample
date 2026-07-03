using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation
{
	public class DestroySelfSystem : ISystem {
		public void Update() {
			W.Query<All<DestroySelf>>().BatchDestroy();
		}
	}
}
