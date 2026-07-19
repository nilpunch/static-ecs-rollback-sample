using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct Collider : IComponent {
		public FVector2 Offset;
		public FP Radius;

		public FVector2 WorldPosition;

		public static Collider New(FVector2 offset, FP radius) {
			return new Collider() {
				Offset = offset,
				Radius = radius,
			};
		}
	}
}
