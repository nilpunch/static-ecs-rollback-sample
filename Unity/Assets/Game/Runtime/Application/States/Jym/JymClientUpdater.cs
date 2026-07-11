using Fixed32;
using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class JymClientUpdater : MonoBehaviour {
		[SerializeField] private bool _enittyViewCulling;

		private void Update() {
			CLNT.Update(Time.realtimeSinceStartup);

			var viewSynchronizer = App.Get<ViewSynchronizer>();
			if (_enittyViewCulling) {
				viewSynchronizer.SynchronizeFreeEntities();
				viewSynchronizer.SynchronizeBroadPhaseEntities(GetCameraBounds(Camera.main));
			}
			else {
				viewSynchronizer.SynchronizeAllDebug();
			}

			ViewTransformInterpolator.Schedule(viewSynchronizer);
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
