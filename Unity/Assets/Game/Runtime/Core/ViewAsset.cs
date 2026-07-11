using System;
using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;

namespace Game.Core {
	[Serializable]
	public struct ViewAsset : IComponent, IEquatable<ViewAsset>
	{
		public short IdPlusOne;

		/// <summary>
		/// Negative view ID is invalid.
		/// </summary>
		public short Id
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (short)(IdPlusOne - 1);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => IdPlusOne = (short)(value + 1);
		}

		public bool IsValid
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Id >= 0;
		}

		public ViewAsset(short id)
		{
			IdPlusOne = (short)(id + 1);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(ViewAsset a, ViewAsset b)
		{
			return a.Equals(b);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(ViewAsset a, ViewAsset b)
		{
			return !(a == b);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(ViewAsset other)
		{
			return IdPlusOne == other.IdPlusOne;
		}

		public override bool Equals(object obj)
		{
			return obj is ViewAsset other && Equals(other);
		}

		public override int GetHashCode()
		{
			return IdPlusOne;
		}
	}
}
