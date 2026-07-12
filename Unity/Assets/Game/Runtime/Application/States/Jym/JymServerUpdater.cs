using UnityEngine;

namespace Game.Application {
	[DefaultExecutionOrder(1)]
	public class JymServerUpdater : MonoBehaviour {
		private void Update() {
			SRVR.Update(Time.time);
		}
	}
}
