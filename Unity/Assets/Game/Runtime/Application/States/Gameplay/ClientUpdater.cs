using System;
using Fixed32;
using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class ClientUpdater : MonoBehaviour {
		private void Update() {
			CLNT.Update(Time.realtimeSinceStartup);

			if (!CLNT.Connection.IsConnected) {
				App.Get<StateMachine>().Enter<MainMenu>();
				return;
			}

			var viewSynchronizer = ViewSynchronizer.Instance;
			viewSynchronizer.SynchronizeFreeEntities();
			viewSynchronizer.SynchronizeBroadPhaseEntities(GetCameraBounds(Camera.main));

			viewSynchronizer.ScheduleTransformSync(CLNT.CalculateInterpolation(Time.realtimeSinceStartup));
		}

		private void LateUpdate() {
			ViewSynchronizer.Instance.CompleteTransformSync();
		}

		public static FAABB2 GetCameraBounds(Camera camera) {
			var extents = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
			var center = (Vector2)camera.transform.position;
			return FAABB2.FromCenterAndExtents(center.ToFP(), extents.ToFP());
		}
	}
}
