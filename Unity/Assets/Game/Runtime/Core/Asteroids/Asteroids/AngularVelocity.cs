using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation.Asteroids
{
	public struct AngularVelocity : IComponent {
		public FAngle Value;
	}
}