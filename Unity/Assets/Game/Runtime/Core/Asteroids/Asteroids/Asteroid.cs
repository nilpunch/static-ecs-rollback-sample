using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation.Asteroids {
	public struct Asteroid : IComponent {
		public ushort Width;
		public ushort Height;
		public FP CellSpacing;
		public FP CellRadius;
	}
}
