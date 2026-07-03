using System.Runtime.CompilerServices;
using Fixed32;
using UnityEngine;

namespace Game.Client {
	public static class FPMathConversions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FVector2 ToFP(this Vector2 vector) {
			return new FVector2(vector.x.ToFP(), vector.y.ToFP());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FVector3 ToFP(this Vector3 vector) {
			return new FVector3(vector.x.ToFP(), vector.y.ToFP(), vector.z.ToFP());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static FQuaternion ToFP(this Quaternion quaternion) {
			return new FQuaternion(quaternion.x.ToFP(), quaternion.y.ToFP(), quaternion.z.ToFP(), quaternion.y.ToFP());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 FromFP(this FVector2 vector) {
			return new Vector2(vector.X.ToFloat(), vector.Y.ToFloat());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector3 FromFP(this FVector3 vector) {
			return new Vector3(vector.X.ToFloat(), vector.Y.ToFloat(), vector.Z.ToFloat());
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion FromFP(this FQuaternion quaternion) {
			return new Quaternion(quaternion.X.ToFloat(), quaternion.Y.ToFloat(), quaternion.Z.ToFloat(), quaternion.Y.ToFloat());
		}
	}
}
