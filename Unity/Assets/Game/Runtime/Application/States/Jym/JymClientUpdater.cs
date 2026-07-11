using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class JymClientUpdater : MonoBehaviour {
		[SerializeField] private bool _viewCulling;
		[SerializeField] private bool _interpolate;

		private void Update() {
			CLNT.Update(Time.time);

			var viewSynchronizer = App.Get<ViewSynchronizer>();
			if (_viewCulling) {
				viewSynchronizer.SynchronizeFreeEntities();
				viewSynchronizer.SynchronizeBroadPhaseEntities(GetCameraBounds(Camera.main));
			}
			else {
				viewSynchronizer.SynchronizeAllDebug();
			}

			viewSynchronizer.ScheduleTransformSync(CLNT.CalculateInterpolation(Time.time), _interpolate);
		}

		private void LateUpdate() {
			App.Get<ViewSynchronizer>().CompleteTransformSync();
		}

		public static FAABB2 GetCameraBounds(Camera camera) {
			var extents = new Vector2(camera.orthographicSize * camera.aspect, camera.orthographicSize);
			var center = (Vector2)camera.transform.position;
			return FAABB2.FromCenterAndExtents(center.ToFP(), extents.ToFP());
		}
	}
}
