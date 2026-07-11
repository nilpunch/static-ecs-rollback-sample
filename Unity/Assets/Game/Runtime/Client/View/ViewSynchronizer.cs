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

namespace Game.Client {
	public class ViewSynchronizer : IResource {
		public readonly List<EntityView> ActiveViews = new();
		public TransformAccessArray Transforms = new(64);

		public NativeArray<Vector2> FromPos;
		public NativeArray<Vector2> ToPos;
		public NativeArray<float> FromAngle;
		public NativeArray<float> ToAngle;

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

		// ── Sync ──────────────────────────────────────────────────────────────────

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

		// ── Pool ──────────────────────────────────────────────────────────────────

		private EntityView CreateView(ViewAsset asset, EntityGID gid) {
			if (!_pool.ContainsVariant(asset)) {
				var prefab = ViewDataBase.Instance.GetViewPrefab(asset);
				var poolRoot = new GameObject(prefab.name + " Pool").transform;
				_pool.AddVariant(asset, new Pool<EntityView>(new PrefabFactory<EntityView>(prefab, poolRoot)));

				if (_poolRoots.Length <= asset.Id) {
					Array.Resize(ref _poolRoots, asset.Id << 1);
				}

				_poolRoots[asset.Id] = poolRoot;
			}

			var view = _pool.Get(asset);
			view.AssignEntity(gid);
			return view;
		}

		private void DestroyView(EntityView view) {
			view.RemoveEntity();
			view.transform.SetParent(_poolRoots[_pool.GetKey(view).Id]);
			_pool.Return(view);
		}

		// ── Interpolation ───────────────────────────────────────────────────────────

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
				Alpha = alpha,
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
				var gid = ActiveViews[i].Entity;

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
		}
	}
}
