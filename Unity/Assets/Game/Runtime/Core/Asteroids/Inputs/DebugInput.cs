using Fixed32;
using Shenanicode.Rollback;

namespace Game.Core {
	public struct DebugInput : IInput {
		public FVector2 MousePosition;
		public FP Radius;
		public FP Acceleration;
	}
}
