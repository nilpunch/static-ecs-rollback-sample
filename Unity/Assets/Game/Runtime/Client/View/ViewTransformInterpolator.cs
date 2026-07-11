using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using Game.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

namespace Game.Client {
	/// <summary>
	/// Writes the interpolated pose of every active view from a Burst job over
	/// <see cref="ViewSynchronizer.Transforms"/> (which the synchronizer keeps index-aligned with its views).
	///
	/// Pose is interpolated between the two client worlds by the sub-tick alpha from the netcode clock:
	///   previous tick = <see cref="GameWorldPrev"/> (WP), current tick = <see cref="ClientWorld"/> (W),
	///   alpha = <c>CLNT.CalculateInterpolation(Time.realtimeSinceStartup)</c>.
	///
	/// Usage per frame: <see cref="Schedule"/> after the view sync (reads ECS on the main thread and kicks the job),
	/// then <see cref="Complete"/> in LateUpdate so the workers overlap the rest of the frame.
	/// </summary>
	public static class ViewTransformInterpolator {
		// Index-aligned with ViewSynchronizer.ActiveViews / .Transforms. Capacity may exceed the live count.
		public static NativeArray<Vector2> FromPos;
		public static NativeArray<Vector2> ToPos;
		public static NativeArray<float> FromAngle;
		public static NativeArray<float> ToAngle;

		private static JobHandle _handle;
		private static bool _scheduled;

		/// <summary>Read poses on the main thread and schedule the interpolation job. Call once per frame.</summary>
		public static void Schedule(ViewSynchronizer viewSynchronizer) {
			var sync = viewSynchronizer;
			var views = sync.ActiveViews;
			var count = views.Count;
			if (count == 0) {
				_scheduled = false;
				return;
			}

			EnsureCapacity(count);
			Gather(views, count);

			_handle = new InterpolateJob {
				FromPos = FromPos,
				ToPos = ToPos,
				FromAngle = FromAngle,
				ToAngle = ToAngle,
				Alpha = CLNT.CalculateInterpolation(Time.realtimeSinceStartup),
			}.Schedule(sync.Transforms);
			_scheduled = true;
		}

		/// <summary>Block until the scheduled job has written all transforms. Call in LateUpdate.</summary>
		public static void Complete() {
			if (_scheduled) {
				_handle.Complete();
				_scheduled = false;
			}
		}

		/// <summary>Read each entity's pose from the current (W) and previous (WP) worlds into the pose arrays.</summary>
		private static void Gather(List<EntityView> views, int count) {
			for (var i = 0; i < count; i++) {
				var gid = views[i].Entity;

				Vector2 toPos;
				float toAngle;
				Vector2 fromPos;
				float fromAngle;

				var entity = gid.Unpack<ClientWorld>();
				ref readonly var body = ref entity.Read<PhysicalBody>()!;
				toPos = body.WorldOrigin.FromFP();
				toAngle = body.Rotation.Radians.ToFloat();

				if (gid.TryUnpack<GameWorldPrev>(out var entityPrev)) {
					ref readonly var bodyPrev = ref entityPrev.Read<PhysicalBody>()!;
					fromPos = bodyPrev.WorldOrigin.FromFP();
					fromAngle = bodyPrev.Rotation.Radians.ToFloat();
				}
				else {
					fromPos = toPos;
					fromAngle = toAngle;
				}

				ToPos[i] = toPos;
				ToAngle[i] = toAngle;
				FromPos[i] = fromPos;
				FromAngle[i] = fromAngle;
			}
		}

		private static void EnsureCapacity(int count) {
			if (FromPos.IsCreated && FromPos.Length >= count) {
				return;
			}

			DisposeArrays();

			var capacity = Mathf.NextPowerOfTwo(count);
			FromPos = new NativeArray<Vector2>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			ToPos = new NativeArray<Vector2>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			FromAngle = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			ToAngle = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		}

		/// <summary>Release native memory. Call on teardown.</summary>
		public static void Dispose() {
			Complete();
			DisposeArrays();
		}

		private static void DisposeArrays() {
			if (FromPos.IsCreated)
				FromPos.Dispose();
			if (ToPos.IsCreated)
				ToPos.Dispose();
			if (FromAngle.IsCreated)
				FromAngle.Dispose();
			if (ToAngle.IsCreated)
				ToAngle.Dispose();
		}

		[BurstCompile]
		private struct InterpolateJob : IJobParallelForTransform {
			[ReadOnly] public NativeArray<Vector2> FromPos;
			[ReadOnly] public NativeArray<Vector2> ToPos;
			[ReadOnly] public NativeArray<float> FromAngle;
			[ReadOnly] public NativeArray<float> ToAngle;
			public float Alpha;

			public void Execute(int index, TransformAccess transform) {
				var a = FromPos[index];
				var b = ToPos[index];
				var x = a.x + (b.x - a.x) * Alpha;
				var y = a.y + (b.y - a.y) * Alpha;

				var half = LerpAngle(FromAngle[index], ToAngle[index], Alpha) * 0.5f;
				transform.position = new Vector3(x, y, 0f);

				math.sincos(half, out var sin, out var cos);
				transform.rotation = new Quaternion(0f, 0f, sin, cos);
			}

			// Shortest-path angular lerp in radians.
			private static float LerpAngle(float from, float to, float t) {
				var delta = math.fmod(to - from, 2f * math.PI);
				if (delta > math.PI) {
					delta -= 2f * math.PI;
				}
				else if (delta < -math.PI) {
					delta += 2f * math.PI;
				}

				return from + delta * t;
			}
		}
	}
}
