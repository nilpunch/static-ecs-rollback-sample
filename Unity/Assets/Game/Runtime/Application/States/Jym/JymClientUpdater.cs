using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class JymClientUpdater : MonoBehaviour {
		private void Update() {
			CLNT.Update(Time.realtimeSinceStartup);

			App.Get<ViewSynchronizer>().SynchronizeFreeEntities();

			ViewTransformInterpolator.Schedule();
		}
	}
}
