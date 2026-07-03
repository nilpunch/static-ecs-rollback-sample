using Shenanicode.Rollback;

namespace Game.Core {
	public struct MoveInput : IInput {
		private const byte LeftMask  = 1 << 0;
		private const byte RightMask = 1 << 1;
		private const byte UpMask    = 1 << 2;
		private const byte DownMask  = 1 << 3;

		public byte Arrows;

		public bool Left {
			get => (Arrows & LeftMask) != 0;
			set => Arrows = (byte)(value ? Arrows | LeftMask : Arrows & ~LeftMask);
		}

		public bool Right {
			get => (Arrows & RightMask) != 0;
			set => Arrows = (byte)(value ? Arrows | RightMask : Arrows & ~RightMask);
		}

		public bool Up {
			get => (Arrows & UpMask) != 0;
			set => Arrows = (byte)(value ? Arrows | UpMask : Arrows & ~UpMask);
		}

		public bool Down {
			get => (Arrows & DownMask) != 0;
			set => Arrows = (byte)(value ? Arrows | DownMask : Arrows & ~DownMask);
		}
	}
}
