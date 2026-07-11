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

			App.Get<ViewSynchronizer>().SynchronizeFreeEntities();
			App.Get<ViewSynchronizer>().SynchronizeBroadPhaseEntities(GetCameraBounds(Camera.main));

			ViewTransformInterpolator.Schedule();
		}

		private void LateUpdate() {
			ViewTransformInterpolator.Complete();
		}

		public static FAABB2 GetCameraBounds(Camera camera) {
			var extents = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
			var center = (Vector2)camera.transform.position;
			return FAABB2.FromCenterAndExtents(center.ToFP(), extents.ToFP());
		}
	}
}
