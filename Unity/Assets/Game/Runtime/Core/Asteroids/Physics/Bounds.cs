using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core
{
	public struct Bounds : IComponent, ITrackableAdded, ITrackableChanged, IDisableable {
		public FVector2 WorldPosition;
		public FVector2 Extents;

		public FAABB2 AABB {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => FAABB2.FromCenterAndExtents(WorldPosition, Extents);
		}

		public static Bounds New(FVector2 worldPosition, FVector2 extents) {
			return new Bounds {
				WorldPosition = worldPosition,
				Extents = extents + Const.BoundsPadding
			};
		}
	}
}
