using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct Collider : IComponent {
		public FVector2 Offset;
		public FP Radius;

		public FVector2 WorldPosition;
	}
}
