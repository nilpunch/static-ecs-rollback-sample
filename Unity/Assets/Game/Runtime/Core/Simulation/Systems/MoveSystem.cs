using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation {
	public class MoveSystem : ISystem {
		void ISystem.Update() {
			W.Query().For((ref Position position, in Velocity velocity) => {
				position.Value += velocity.Value;
			});
		}
	}
}
