using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using Game.Core;
using UnityEngine;

namespace Game.Application {
	public class ClientUpdater : MonoBehaviour {
		[SerializeField] private bool _isJym;
		[SerializeField] private bool _viewCulling = true;
		[SerializeField] private bool _interpolate = true;
		[SerializeField] private Vector2 _virtualCameraPosition;

		private float CurrentTime => _isJym ? Time.time : Time.realtimeSinceStartup;

		private void Update() {
			CLNT.Update(CurrentTime);

			if (!CLNT.Synced) {
				return;
			}

			var interpolation = CLNT.CalculateInterpolation(CurrentTime);

			var prevPlayerPosition = GetPlayerPosition<GameWorldPrev>().GetValueOrDefault(_virtualCameraPosition);
			var playerPosition = GetPlayerPosition<ClientWorld>().GetValueOrDefault(_virtualCameraPosition);

			_virtualCameraPosition = Vector2.Lerp(prevPlayerPosition, playerPosition, interpolation);

			var viewSynchronizer = ViewSynchronizer.Instance;
			if (_viewCulling) {
				viewSynchronizer.SynchronizeFreeEntities();
				viewSynchronizer.SynchronizeBroadPhaseEntities(_virtualCameraPosition.ToFP(), GetCameraExtents(Camera.main));
			}
			else {
				viewSynchronizer.SynchronizeAllDebug();
			}

			viewSynchronizer.ScheduleTransformSync(interpolation, _interpolate);
		}

		private void LateUpdate() {
			ViewSynchronizer.Instance.CompleteTransformSync();
		}

		private static FVector2 GetCameraExtents(Camera camera) {
			return new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize).ToFP();
		}

		private static Vector2? GetPlayerPosition<TWorld>() where TWorld : struct, IWorldType {
			var playerMapping = World<TWorld>.GetResource<PlayerMapping>();
			if (playerMapping.EntityByChannel.TryGetValue(CLNT.Channel, out var entity)) {
				return entity.Unpack<TWorld>().Read<PhysicalBody>()!.WorldOrigin.FromFP();
			}
			return null;
		}
	}
}
