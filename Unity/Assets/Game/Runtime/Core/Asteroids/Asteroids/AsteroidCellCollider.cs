using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation.Asteroids
{
	public struct AsteroidCellCollider : IComponent {
		public short X;
		public short Y;
		public FVector2 LocalOffset;
	}
}