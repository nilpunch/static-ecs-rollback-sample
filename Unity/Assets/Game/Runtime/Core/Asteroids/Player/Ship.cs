using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core {
	public struct Ship : IComponent {
		public FP Thrust;
		public FAngle Turn;
	}
}
