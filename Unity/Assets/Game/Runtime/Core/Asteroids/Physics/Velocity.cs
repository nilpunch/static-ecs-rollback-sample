using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct Velocity : IComponent {
		public FVector2 Linear;
		public FAngle Angular;
	}
}
