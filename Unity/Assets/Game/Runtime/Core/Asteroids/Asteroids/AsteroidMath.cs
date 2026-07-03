using System.Runtime.CompilerServices;
using Fixed32;

namespace Game.Core.Simulation.Asteroids
{
	public static class AsteroidMath {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FVector2 CellOffset(in Asteroid asteroid, int x, int y) {
			var originX = (asteroid.Width - 1).ToFP() * asteroid.CellSpacing * FP.Half;
			var originY = (asteroid.Height - 1).ToFP() * asteroid.CellSpacing * FP.Half;
			return new FVector2(x.ToFP() * asteroid.CellSpacing - originX, y.ToFP() * asteroid.CellSpacing - originY);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int IndexOf(int x, int y, ushort width) {
			return x + y * width;
		}
	}
}