using System;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using Game.Core;
using UnityEngine;

namespace Game.Application {
	public class ClientUpdater : MonoBehaviour {
		[SerializeField] private bool _isLocalTesting;
		[SerializeField] private bool _simulateLatency;
		[SerializeField] private bool _viewCulling = true;
		[SerializeField] private bool _interpolate = true;
		[SerializeField] private Vector2 _virtualCameraPosition;

		private float CurrentTime => _isLocalTesting ? Time.time : Time.realtimeSinceStartup;

		private void Update() {
			CLNT.Update(CurrentTime);

			if (!CLNT.Connection.IsConnected && !_isLocalTesting) {
				App.Get<StateMachine>().Enter<MainMenu>();
				return;
			}

			App.Get<AppServerConnection>().Connection.SimulateLatency = _simulateLatency;

			var interpolation = CLNT.CalculateInterpolation(CurrentTime);

			var prevPlayerPosition = GetPlayerPosition<GameWorldPrev>().GetValueOrDefault(_virtualCameraPosition);
			var playerPosition = GetPlayerPosition<ClientWorldType>().GetValueOrDefault(_virtualCameraPosition);

			_virtualCameraPosition = ToroidalLerp(prevPlayerPosition, playerPosition, interpolation);

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

		private void OnGUI() {
			var color = _simulateLatency ? Color.red : Color.white;
			var style = new GUIStyle(GUI.skin.label) {
				alignment = TextAnchor.UpperCenter,
				fontSize = 20,
				fontStyle = FontStyle.Bold,
				normal = { textColor = color, background = null },
				hover = { textColor = color, background = null },
				active = { textColor = color, background = null },
				focused = { textColor = color, background = null },
				onNormal = { textColor = color, background = null },
				onHover = { textColor = color, background = null },
				onActive = { textColor = color, background = null },
				onFocused = { textColor = color, background = null }
			};

			var rect = new Rect(0, 10, Screen.width, 30);
			GUI.Label(rect, $"Simulate Latency: {_simulateLatency}", style);
		}
	}
}
