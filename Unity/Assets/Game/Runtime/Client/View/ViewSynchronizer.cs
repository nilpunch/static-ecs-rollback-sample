using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using UnityEngine.Jobs;
using static Game.Core<Game.Client.ClientWorld>;

namespace Game.Client {
	/// <summary>
	/// VIBE CODE
	/// Reconciles live <see cref="ViewAsset"/> entities with pooled <see cref="EntityView"/> instances.
	/// Rollback means state changes sporadically and is not observable via reactive events, so every frame we
	/// re-scan and mark-and-sweep: create/reuse views for entities that should have one, destroy the rest.
	///
	/// Persistent storage lives here (not on the entities) because the ECS state is rolled back and entity slots
	/// are reused — a view must survive that and be reattached to whatever GID currently owns it.
	/// Keyed by <see cref="EntityGID"/> (carries a version) so a reused slot never aliases a stale view.
	///
	/// MUST be driven from the Unity update loop, once per rendered frame — never as an ECS system, which would
	/// re-run during rollback resimulation and thrash GameObjects.
	/// </summary>
	public class ViewSynchronizer : IResource {
		/// <summary>
		/// Compact list of every live view, kept index-aligned in lockstep with <see cref="Transforms"/>.
		/// Consumed by <see cref="ViewTransformInterpolator"/> — slot <c>i</c> here is slot <c>i</c> there.
		/// </summary>
		public readonly List<EntityView> ActiveViews = new();

		/// <summary>
		/// Root transforms of <see cref="ActiveViews"/>, maintained incrementally (Add / RemoveAtSwapBack) so the
		/// interpolator can hand it straight to a transform job with no per-frame rebuild.
		/// </summary>
		public TransformAccessArray Transforms;

		private readonly Dictionary<EntityGID, Tracked> _views = new();
		private readonly List<EntityGID> _stale = new();
		private static int _syncFreeId;
		private static int _syncBroadPhaseId;

		private enum SyncType : byte {
			Free,
			BroadPhase
		}

		private struct Tracked {
			public EntityView View;
			public int Index; // slot in ActiveViews / Transforms.
			public int SyncId;
			public ViewAsset Asset;
			public SyncType SyncType;
		}

		public ViewSynchronizer() {
			Transforms = new TransformAccessArray(64);
		}

		public void SynchronizeAllDebug() {
			_syncFreeId++;

			var self = this;
			W.Query().For(ref self,
				static (ref ViewSynchronizer self, W.Entity entity, in ViewAsset asset) => {
					self.Actualize(entity.GID, asset, _syncFreeId, SyncType.Free);
				});

			Sweep(_syncFreeId, SyncType.Free);
		}

		/// <summary>
		/// Reconcile views against every entity that currently owns a valid <see cref="ViewAsset"/>.
		/// Only non-broad phase one.
		/// </summary>
		public void SynchronizeFreeEntities() {
			_syncFreeId++;

			var self = this;
			W.Query<None<BroadPhaseInfo>>().For(ref self,
				static (ref ViewSynchronizer self, W.Entity entity, in ViewAsset asset) => {
					self.Actualize(entity.GID, asset, _syncFreeId, SyncType.Free);
				});

			Sweep(_syncFreeId, SyncType.Free);
		}

		/// <summary>
		/// Reconcile views only for entities whose collider falls inside <paramref name="cameraBounds"/>.
		/// Entities without a <see cref="BroadPhaseInfo"/> (no collider) are never indexed and get no view.
		/// </summary>
		public void SynchronizeBroadPhaseEntities(FAABB2 cameraBounds) {
			_syncBroadPhaseId++;

			var nearby = W.GetResource<BroadPhase>().FindNearbyEntities(cameraBounds);
			foreach (var entity in nearby) {
				if (entity.Has<ViewAsset>()) {
					Actualize(entity.GID, entity.Read<ViewAsset>(), _syncBroadPhaseId, SyncType.BroadPhase);
				}
			}

			Sweep(_syncBroadPhaseId, SyncType.BroadPhase);
		}

		/// <summary>
		/// Ensure the entity has a matching, live view and stamp it as seen this frame.
		/// </summary>
		private void Actualize(EntityGID gid, ViewAsset asset, int syncId, SyncType syncType) {
			if (!asset.IsValid) {
				// Invalid asset: leave any existing view unstamped so the sweep reclaims it.
				return;
			}

			if (_views.TryGetValue(gid, out var tracked)) {
				if (tracked.Asset != asset) {
					// The entity swapped its view (e.g. state changed under it) — replace in place.
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

		/// <summary>
		/// Destroy every view whose entity was not seen this frame (dead, out of view, or lost its asset).
		/// </summary>
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

		/// <summary>Destroy the view and swap-remove it from <see cref="ActiveViews"/>, keeping slots tight.</summary>
		private void RemoveView(EntityGID gid) {
			var tracked = _views[gid];
			DestroyView(tracked.View);

			var last = ActiveViews.Count - 1;
			var index = tracked.Index;
			if (index != last) {
				var moved = ActiveViews[last];
				ActiveViews[index] = moved;

				// The moved view kept its GID (only the removed one was reset) — repoint its slot.
				var movedTracked = _views[moved.Entity];
				movedTracked.Index = index;
				_views[moved.Entity] = movedTracked;
			}

			ActiveViews.RemoveAt(last);
			Transforms.RemoveAtSwapBack(index); // Mirrors the list swap-remove; on index == last it just drops the tail.
			_views.Remove(gid);
		}

		private EntityView CreateView(ViewAsset asset, EntityGID gid) {
			var view = EntityViewFactory.CreateView(asset);
			view.AssignEntity(gid);
			return view;
		}

		private void DestroyView(EntityView view) {
			view.RemoveEntity();
			EntityViewFactory.DestroyView(view);
		}

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
			if (Transforms.isCreated) {
				Transforms.Dispose();
			}

			EntityViewFactory.Reset();
		}
	}
}
