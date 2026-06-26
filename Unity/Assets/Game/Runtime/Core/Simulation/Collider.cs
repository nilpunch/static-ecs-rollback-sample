using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation {
	public struct Collider : IComponent {
		public FVector2 Offset;
		public FP Radius;
	}
}
