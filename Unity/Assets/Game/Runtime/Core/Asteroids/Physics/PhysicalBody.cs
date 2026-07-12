using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct PhysicalBody : IComponent {
		public FVector2 WorldCoM;
		public FVector2 OriginOffset; // Accumulates center of mass changes.
		public FAngle Rotation;

		public FVector2 WorldOrigin {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => WorldCoM + Rotation.Counterclockwise * OriginOffset;
		}
	}
}
