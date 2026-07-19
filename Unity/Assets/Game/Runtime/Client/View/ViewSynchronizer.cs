using System;
using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Game.Utils;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using static Game.Core<Game.Client.ClientWorld>;
using Object = UnityEngine.Object;

namespace Game.Client {
	public class ViewSynchronizer {
		public static ViewSynchronizer Instance { get; private set; }

		public readonly List<EntityView> ActiveViews = new();
		public TransformAccessArray Transforms = new(64);

		public NativeArray<Vector2> FromPos;
		public NativeArray<Vector2> ToPos;
		public NativeArray<float> FromAngle;
		public NativeArray<float> ToAngle;
		public NativeArray<float> MaxSpeed;
		public NativeArray<float> MaxAngularSpeed;

		private VariantPool<ViewAsset, EntityView> _pool = new();
		private Transform[] _poolRoots = new Transform[4];

		private readonly Dictionary<EntityGID, Tracked> _views = new();
		private readonly List<EntityGID> _stale = new();
		private int _freeSyncId;
		private int _broadPhaseSyncId;

		private JobHandle _jobHandle;
		private bool _jobScheduled;

		private enum SyncType : byte {
			Free,
			BroadPhase
		}

		private struct Tracked {
			public EntityView View;
			public int Index;
			public int SyncId;
			public ViewAsset Asset;
			public SyncType SyncType;
		}

		public static void Create() {
			if (Instance == null) {
				Instance = new ViewSynchronizer();
			}
		}

		public static void Destroy() {
			if (Instance != null) {
				Instance.Dispose();
				Instance = null;
			}
		}

		public void SynchronizeAllDebug() {
			_freeSyncId++;

			var self = this;
			W.Query().For(ref self,
				static (ref ViewSynchronizer self, W.Entity entity, in ViewAsset asset) => {
					self.Actualize(entity.GID, asset, self._freeSyncId, SyncType.Free);
				});

			Sweep(_freeSyncId, SyncType.Free);
		}

		public void SynchronizeFreeEntities() {
			_freeSyncId++;

			var self = this;
			W.Query<None<BroadPhaseInfo>>().For(ref self,
				static (ref ViewSynchronizer self, W.Entity entity, in ViewAsset asset) => {
					self.Actualize(entity.GID, asset, self._freeSyncId, SyncType.Free);
				});

			Sweep(_freeSyncId, SyncType.Free);
		}

		public void SynchronizeBroadPhaseEntities(FAABB2 cameraBounds) {
			_broadPhaseSyncId++;

			foreach (var entity in W.GetResource<BroadPhase>().FindNearbyEntities(cameraBounds)) {
				if (entity.Has<ViewAsset>()) {
					Actualize(entity.GID, entity.Read<ViewAsset>(), _broadPhaseSyncId, SyncType.BroadPhase);
				}
			}

			Sweep(_broadPhaseSyncId, SyncType.BroadPhase);
		}

		private void Actualize(EntityGID gid, ViewAsset asset, int syncId, SyncType syncType) {
			if (!asset.IsValid) {
				return;
			}

			if (_views.TryGetValue(gid, out var tracked)) {
				if (tracked.Asset != asset) {
					DestroyView(tracked.View);
					tracked.View = CreateView(asset, gid);
					tracked.Asset = asset;
					ActiveViews[tracked.Index] = tracked.View;
					Transforms[tracked.Index] = tracked.View.RootTransform;
				}
			}
			else {
				tracked.Asset = asset;
				tracked.View = CreateView(asset, gid);
				tracked.Index = ActiveViews.Count;
				ActiveViews.Add(tracked.View);
				Transforms.Add(tracked.View.RootTransform);
			}

			tracked.SyncId = syncId;
			tracked.SyncType = syncType;
			_views[gid] = tracked;
		}

		private void Sweep(int syncId, SyncType syncType) {
			foreach (var pair in _views) {
				if (pair.Value.SyncType == syncType && pair.Value.SyncId != syncId) {
					_stale.Add(pair.Key);
				}
			}

			foreach (var gid in _stale) {
				RemoveView(gid);
			}

			_stale.Clear();
		}

		private void RemoveView(EntityGID gid) {
			var tracked = _views[gid];
			DestroyView(tracked.View);

			var last = ActiveViews.Count - 1;
			var index = tracked.Index;
			if (index != last) {
				var moved = ActiveViews[last];
				ActiveViews[index] = moved;

				var movedTracked = _views[moved.Entity];
				movedTracked.Index = index;
				_views[moved.Entity] = movedTracked;
			}

			ActiveViews.RemoveAt(last);
			Transforms.RemoveAtSwapBack(index);
			_views.Remove(gid);
		}

		private EntityView CreateView(ViewAsset asset, EntityGID gid) {
			if (!_pool.ContainsVariant(asset)) {
				var prefab = ViewDataBase.Instance.GetViewPrefab(asset);
				var poolRoot = new GameObject(prefab.name + " Pool").transform;
				Object.DontDestroyOnLoad(poolRoot.gameObject);
				_pool.AddVariant(asset, new Pool<EntityView>(new PrefabFactory<EntityView>(prefab, poolRoot)));

				if (_poolRoots.Length <= asset.Id) {
					Array.Resize(ref _poolRoots, asset.Id << 1);
				}

				_poolRoots[asset.Id] = poolRoot;
			}

			var view = _pool.Get(asset);
			view.AssignEntity(gid);

			// Snap onto the entity so the max-speed smoothing starts from the correct pose, not its last pooled one.
			ref readonly var body = ref gid.Unpack<ClientWorld>().Read<PhysicalBody>()!;
			var spawn = body.WorldOrigin.FromFP();
			view.RootTransform.SetPositionAndRotation(
				new Vector3(spawn.x, spawn.y, 0f),
				Quaternion.Euler(0f, 0f, body.Rotation.Radians.ToFloat() * Mathf.Rad2Deg));

			return view;
		}

		private void DestroyView(EntityView view) {
			view.RemoveEntity();
			view.transform.SetParent(_poolRoots[_pool.GetKey(view).Id]);
			_pool.Return(view);
		}

		public void ScheduleTransformSync(float alpha, bool useInterpolation = true) {
			var count = ActiveViews.Count;
			if (count == 0) {
				_jobScheduled = false;
				return;
			}

			EnsureCapacity(count);
			Gather(count, useInterpolation);

			_jobHandle = new InterpolateJob {
				FromPos = FromPos,
				ToPos = ToPos,
				FromAngle = FromAngle,
				ToAngle = ToAngle,
				MaxSpeed = MaxSpeed,
				MaxAngularSpeed = MaxAngularSpeed,
				Alpha = alpha,
				DeltaTime = Time.deltaTime,
				WorldSize = Core.Const.WorldSize.FromFP(),
				WorldHalf = Core.Const.WorldHalf.FromFP(),
			}.Schedule(Transforms);
			_jobScheduled = true;
		}

		public void CompleteTransformSync() {
			if (_jobScheduled) {
				_jobHandle.Complete();
				_jobScheduled = false;
			}
		}

		private void Gather(int count, bool useInterpolation) {
			for (var i = 0; i < count; i++) {
				var view = ActiveViews[i];
				var gid = view.Entity;
				MaxSpeed[i] = view.PoseCorrectionSpeed;
				MaxAngularSpeed[i] = view.AngleCorrectionSpeed * Mathf.Deg2Rad;

				ref readonly var body = ref gid.Unpack<ClientWorld>().Read<PhysicalBody>()!;
				var toPos = body.WorldOrigin.FromFP();
				var toAngle = body.Rotation.Radians.ToFloat();

				Vector2 fromPos;
				float fromAngle;
				if (useInterpolation && gid.TryUnpack<GameWorldPrev>(out var entityPrev)) {
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

		private void EnsureCapacity(int count) {
			if (FromPos.IsCreated && FromPos.Length >= count) {
				return;
			}

			DisposeArrays();

			var capacity = Mathf.NextPowerOfTwo(count);
			FromPos = new NativeArray<Vector2>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			ToPos = new NativeArray<Vector2>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			FromAngle = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			ToAngle = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			MaxSpeed = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
			MaxAngularSpeed = new NativeArray<float>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
		}

		[BurstCompile]
		private struct InterpolateJob : IJobParallelForTransform {
			[ReadOnly] public NativeArray<Vector2> FromPos;
			[ReadOnly] public NativeArray<Vector2> ToPos;
			[ReadOnly] public NativeArray<float> FromAngle;
			[ReadOnly] public NativeArray<float> ToAngle;
			[ReadOnly] public NativeArray<float> MaxSpeed;
			[ReadOnly] public NativeArray<float> MaxAngularSpeed;
			public float Alpha;
			public float DeltaTime;
			public float2 WorldSize;
			public float2 WorldHalf;

			public void Execute(int index, TransformAccess transform) {
				var a = FromPos[index];
				var b = ToPos[index];

				// Interpolate along the shortest path around the torus. A body that wrapped
				// last tick (e.g. +half -> -half) has a raw from->to delta of a full world;
				// wrapping it keeps the visual motion continuous across the seam instead of
				// sweeping back through the centre.
				var toDeltaX = Wrap(b.x - a.x, WorldSize.x, WorldHalf.x);
				var toDeltaY = Wrap(b.y - a.y, WorldSize.y, WorldHalf.y);
				var targetX = a.x + toDeltaX * Alpha;
				var targetY = a.y + toDeltaY * Alpha;

				var current = transform.position;
				var maxStep = MaxSpeed[index] * DeltaTime;
				// Shortest signed distance from the drawn pose to the target, so crossing the
				// seam is a small step rather than a full-world pose correction.
				var dx = Wrap(targetX - current.x, WorldSize.x, WorldHalf.x);
				var dy = Wrap(targetY - current.y, WorldSize.y, WorldHalf.y);
				var distanceSq = dx * dx + dy * dy;

				float x, y;
				if (distanceSq <= maxStep * maxStep || maxStep <= 0f) {
					x = current.x + dx;
					y = current.y + dy;
				}
				else {
					var scale = maxStep / math.sqrt(distanceSq);
					x = current.x + dx * scale;
					y = current.y + dy * scale;
				}

				// Fold the drawn position back into [-half, half) so it stays on the torus.
				x = Wrap(x, WorldSize.x, WorldHalf.x);
				y = Wrap(y, WorldSize.y, WorldHalf.y);

				var targetAngle = LerpAngle(FromAngle[index], ToAngle[index], Alpha);
				var rotation = transform.rotation;
				var currentAngle = 2f * math.atan2(rotation.z, rotation.w);
				var maxAngleStep = MaxAngularSpeed[index] * DeltaTime;
				var angleDelta = ShortestDelta(currentAngle, targetAngle);

				float half;
				if (math.abs(angleDelta) <= maxAngleStep || maxAngleStep <= 0f) {
					half = targetAngle * 0.5f;
				}
				else {
					half = (currentAngle + math.sign(angleDelta) * maxAngleStep) * 0.5f;
				}

				transform.position = new Vector3(x, y, 0f);

				math.sincos(half, out var sin, out var cos);
				transform.rotation = new Quaternion(0f, 0f, sin, cos);
			}

			// Wrap a value into [-half, half); mirrors Const.WrapCoord in float space.
			private static float Wrap(float v, float size, float half) {
				return v - size * math.floor((v + half) / size);
			}

			private static float LerpAngle(float from, float to, float t) {
				return from + ShortestDelta(from, to) * t;
			}

			// Signed shortest angular distance from -> to, in (-PI, PI].
			private static float ShortestDelta(float from, float to) {
				var delta = math.fmod(to - from, 2f * math.PI);
				if (delta > math.PI) {
					delta -= 2f * math.PI;
				}
				else if (delta < -math.PI) {
					delta += 2f * math.PI;
				}

				return delta;
			}
		}

		// ── Lifecycle ───────────────────────────────────────────────────────────────

		public void Clear() {
			foreach (var view in ActiveViews) {
				DestroyView(view);
			}

			ActiveViews.Clear();
			_views.Clear();
			_stale.Clear();

			while (Transforms.length > 0) {
				Transforms.RemoveAtSwapBack(Transforms.length - 1);
			}
		}

		public void Dispose() {
			CompleteTransformSync();

			if (Transforms.isCreated) {
				Transforms.Dispose();
			}

			DisposeArrays();

			for (int i = 0; i < _poolRoots.Length; i++) {
				if (_poolRoots[i] != null) {
					Object.Destroy(_poolRoots[i].gameObject);
				}
			}

			_pool = new VariantPool<ViewAsset, EntityView>();
			Array.Fill(_poolRoots, null);
		}

		private void DisposeArrays() {
			if (FromPos.IsCreated) {
				FromPos.Dispose();
			}

			if (ToPos.IsCreated) {
				ToPos.Dispose();
			}

			if (FromAngle.IsCreated) {
				FromAngle.Dispose();
			}

			if (ToAngle.IsCreated) {
				ToAngle.Dispose();
			}

			if (MaxSpeed.IsCreated) {
				MaxSpeed.Dispose();
			}

			if (MaxAngularSpeed.IsCreated) {
				MaxAngularSpeed.Dispose();
			}
		}
	}
}
