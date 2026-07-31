using System;
using System.Runtime.CompilerServices;
using Fixed32;
using Shenanicode.Rollback;

namespace Game.Core {
	public static class Const {
		public static int TickRate = 30;

		public static int GridWidth = 512;
		public static int GridHeight = 512;
		public static int CellSize = 4;

		public static readonly FP BoundsPadding = FP.FromRatio(5, 10);

		public static FP DeltaTime = FP.One / TickRate;
		public static FVector2 WorldSize = new FVector2(GridWidth.ToFP(), GridHeight.ToFP()) * CellSize;
		public static FVector2 WorldSizeHalf = WorldSize / 2;
		private static int WorldSizeRawMaskX = WorldSize.X.RawValue - 1;
		private static int WorldSizeRawMaskY = WorldSize.Y.RawValue - 1;
		private static int WorldHalfRawX = WorldSizeHalf.X.RawValue;
		private static int WorldHalfRawY = WorldSizeHalf.Y.RawValue;

		static Const() {
			if (!MathUtils.IsPowerOfTwo(WorldSize.X.RawValue)) {
				throw new ArgumentException("World size X must be a power of two for bit-mask wrapping.");
			}

			if (!MathUtils.IsPowerOfTwo(WorldSize.Y.RawValue)) {
				throw new ArgumentException("World size Y must be a power of two for bit-mask wrapping.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP WrapCoordX(FP v) {
			return FP.FromRaw((v.RawValue + WorldHalfRawX & WorldSizeRawMaskX) - WorldHalfRawX);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FP WrapCoordY(FP v) {
			return FP.FromRaw((v.RawValue + WorldHalfRawY & WorldSizeRawMaskY) - WorldHalfRawY);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FVector2 Wrap(FVector2 p) {
			return new FVector2(WrapCoordX(p.X), WrapCoordY(p.Y));
		}
	}
}
