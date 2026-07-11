using FFS.Libraries.StaticEcs;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class CollisionResolutionSystem : ISystem {
			public void Update() {
				W.Query().For(ref W.GetResource<BroadPhase>(),
					(ref BroadPhase broadPhase, in Collider collider) => { });
			}
		}
	}
}
