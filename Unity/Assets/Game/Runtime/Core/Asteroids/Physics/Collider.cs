using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation {
	public struct Collider : IComponent, ITrackableAdded, ITrackableChanged, IDisableable {
		public FVector2 Offset;
		public FVector2 WorldPosition;
		public FP Radius;

		public FAABB2 WorldBounds {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => FAABB2.FromCenterAndExtents(WorldPosition, new FVector2(Radius, Radius));
		}
	}
}
