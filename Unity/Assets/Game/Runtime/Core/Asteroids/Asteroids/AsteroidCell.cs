using FFS.Libraries.StaticEcs;

namespace Game.Core.Simulation.Asteroids
{
	public struct AsteroidCell : IMultiComponent {
		public short X;
		public short Y;
	}
}