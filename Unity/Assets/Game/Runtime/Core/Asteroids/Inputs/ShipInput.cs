using Shenanicode.Rollback;

namespace Game.Core {
	public struct ShipInput : IInput {
		private const byte LeftMask = 1 << 0;
		private const byte RightMask = 1 << 1;
		private const byte UpMask = 1 << 2;
		private const byte DownMask = 1 << 3;
		private const byte ShootMask = 1 << 4;

		public byte Encoded;

		public bool Left {
			get => (Encoded & LeftMask) != 0;
			set => Encoded = (byte)(value ? Encoded | LeftMask : Encoded & ~LeftMask);
		}

		public bool Right {
			get => (Encoded & RightMask) != 0;
			set => Encoded = (byte)(value ? Encoded | RightMask : Encoded & ~RightMask);
		}

		public bool Up {
			get => (Encoded & UpMask) != 0;
			set => Encoded = (byte)(value ? Encoded | UpMask : Encoded & ~UpMask);
		}

		public bool Down {
			get => (Encoded & DownMask) != 0;
			set => Encoded = (byte)(value ? Encoded | DownMask : Encoded & ~DownMask);
		}
	}
}
