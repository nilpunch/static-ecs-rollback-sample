using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Game.Core {
	[StructLayout(LayoutKind.Explicit, Size = 8)]
	public struct CellIndex {
		[FieldOffset(0)] public long Raw;
		[FieldOffset(0)] public int X;
		[FieldOffset(4)] public int Y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(CellIndex a, CellIndex b) {
			return a.Raw == b.Raw;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(CellIndex a, CellIndex b) {
			return a.Raw != b.Raw;
		}

		public override bool Equals(object obj) {
			if (!(obj is CellIndex)) {
				return false;
			}

			return Equals((CellIndex)obj);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(CellIndex other) {
			return Raw == other.Raw;
		}

		public override int GetHashCode() {
			return GenerateHashCode(X, Y);
		}

		private static int GenerateHashCode(int x, int y) {
			return (x << 2) ^ y;
		}

		public override string ToString() {
			return $"(x:{X}, y:{Y})";
		}
	}
}
