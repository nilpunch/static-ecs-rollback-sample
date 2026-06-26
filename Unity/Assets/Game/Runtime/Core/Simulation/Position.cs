using FFS.Libraries.StaticEcs;
using Fixed32;

namespace Game.Core.Simulation {
	public struct Position : IComponent {
		public FVector2 Value;
	}

	public struct Velocity : IComponent {
		public FVector2 Value;
	}
}
