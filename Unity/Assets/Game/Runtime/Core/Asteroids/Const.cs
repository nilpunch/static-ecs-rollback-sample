using System;
using System.Runtime.CompilerServices;
using Fixed32;
using Shenanicode.Rollback;

namespace Game.Core {
	public static class Const {
		public static int TickRate = 30;

		public static FP DeltaTime;
		public static FP InvDeltaTime;

		private static int WorldSizeRawMaskX;
		private static int WorldSizeRawMaskY;
		private static int WorldHalfRawX;
		private static int WorldHalfRawY;

		// Full extent and half extent of the toroidal world, exposed for consumers that
		// need to wrap (e.g. the client view interpolation across the seam).
		public static FVector2 WorldSize { get; private set; }
		public static FVector2 WorldHalf { get; private set; }

		public static readonly FP BoundsPadding = FP.FromRatio(5, 10);

		public static void SetWorldSize(FVector2 worldSize) {
			var worldHalf = worldSize / 2;

			WorldSize = worldSize;
			WorldHalf = worldHalf;

			if (!MathUtils.IsPowerOfTwo(worldSize.X.RawValue)) {
				throw new ArgumentException("World size X must be a power of two for bit-mask wrapping.");
			}

			if (!MathUtils.IsPowerOfTwo(worldSize.Y.RawValue)) {
				throw new ArgumentException("World size Y must be a power of two for bit-mask wrapping.");
			}

			// Mask is over the FULL world size (modulo WorldSize); the half only re-centers
			// the result into [-WorldHalf, +WorldHalf). Using worldHalf here folds every
			// coordinate into [-WorldHalf, 0) instead.
			WorldSizeRawMaskX = worldSize.X.RawValue - 1;
			WorldSizeRawMaskY = worldSize.Y.RawValue - 1;
			WorldHalfRawX = worldHalf.X.RawValue;
			WorldHalfRawY = worldHalf.Y.RawValue;
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
		public static FVector2 WrapPosition(FVector2 p) {
			return new FVector2(WrapCoordX(p.X), WrapCoordY(p.Y));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FVector2 MinImageDelta(FVector2 delta) {
			return new FVector2(WrapCoordX(delta.X), WrapCoordY(delta.Y));
		}
	}
}
