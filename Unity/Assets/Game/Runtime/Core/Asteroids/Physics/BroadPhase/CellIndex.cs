using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Core.Simulation {
	[StructLayout(LayoutKind.Sequential)]
	public struct CellIndex {
		public int X;
		public int Y;

		public CellIndex(int x, int y) {
			X = x;
			Y = y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(CellIndex a, CellIndex b) {
			return a.X == b.X && a.Y == b.Y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(CellIndex a, CellIndex b) {
			return a.X != b.X || a.Y != b.Y;
		}

		public override bool Equals(object obj) {
			if (!(obj is CellIndex)) {
				return false;
			}

			return Equals((CellIndex)obj);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(CellIndex other) {
			return X == other.X && Y == other.Y;
		}

		public override int GetHashCode() {
			return GenerateHashCode((int)X, (int)Y);
		}

		private static int GenerateHashCode(int x, int y) {
			return (x << 2) ^ y;
		}

		public override string ToString() {
			return $"(x:{X}, y:{Y})";
		}
	}
}
