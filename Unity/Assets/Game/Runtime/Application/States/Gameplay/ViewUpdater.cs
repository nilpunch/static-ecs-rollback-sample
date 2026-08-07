using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using Game.Core;
using UnityEngine;

namespace Game.Application {
	public class ViewUpdater : MonoBehaviour {
		[SerializeField] private bool _viewCulling = true;
		[SerializeField] private bool _interpolate = true;
		[SerializeField] private Vector2 _virtualCameraPosition;
		[SerializeField] private AnimationCurve _cameraSizeByVelocity;

		private void Update() {
			var interpolation = CLNT.CalculateInterpolation(ClientTime.CurrentTime);

			UpdateVirtualCamera(interpolation);

			ScheduleViewSync(interpolation);
		}

		private void UpdateVirtualCamera(float interpolation) {
			var prevPlayerPosition = GetPlayerPosition<GameWorldPrev>().GetValueOrDefault(_virtualCameraPosition);
			var playerPosition = GetPlayerPosition<ClientWorldType>().GetValueOrDefault(_virtualCameraPosition);
			_virtualCameraPosition = ToroidalLerp(prevPlayerPosition, playerPosition, interpolation);

			var playerVelocity = GetPlayerVelocity<ClientWorldType>().GetValueOrDefault(Vector2.zero);
			var cameraSize = _cameraSizeByVelocity.Evaluate(playerVelocity.magnitude);
			var smoothAlpha = 1f - Mathf.Pow(0.5f, Time.deltaTime / 1f);
			Camera.main.orthographicSize += (cameraSize - Camera.main.orthographicSize) * smoothAlpha;
		}

		private void ScheduleViewSync(float interpolation) {
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

		private static World<TWorld>.Entity? GetPlayerEntity<TWorld>() where TWorld : struct, IWorldType {
			var playerMapping = World<TWorld>.GetResource<PlayerMapping>();
			if (playerMapping.EntityByChannel.TryGetValue(CLNT.Channel, out var entity)) {
				return entity.Unpack<TWorld>();
			}
			return null;
		}

		private static Vector2? GetPlayerPosition<TWorld>() where TWorld : struct, IWorldType {
			return GetPlayerEntity<TWorld>()?.Read<PhysicalBody>()!.WorldOrigin.FromFP();
		}

		private static Vector2? GetPlayerVelocity<TWorld>() where TWorld : struct, IWorldType {
			return GetPlayerEntity<TWorld>()?.Read<Velocity>()!.Linear.FromFP();
		}

		private static Vector2 ToroidalLerp(Vector2 prev, Vector2 current, float t) {
			var size = Core.Const.WorldSize.FromFP();
			return new Vector2(
				WrapLerpAxis(prev.x, current.x, size.x, t),
				WrapLerpAxis(prev.y, current.y, size.y, t));
		}

		private static float WrapLerpAxis(float prev, float current, float size, float t) {
			var half = size * 0.5f;
			var delta = current - prev;
			if (delta > half) {
				prev += size;
			}
			else if (delta < -half) {
				prev -= size;
			}

			var result = Mathf.Lerp(prev, current, t);
			return result - size * Mathf.Floor((result + half) / size);
		}
	}
}
