using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct PhysicalBody : IComponent {
		public FVector2 CenterOfMass;
		public FVector2 OriginOffset; // Accumulates center of mass changes.
		public FVector2 WorldOrigin;
		public FAngle Rotation;
	}
}
