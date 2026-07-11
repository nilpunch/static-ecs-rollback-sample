using UnityEngine;

namespace Game.Application {
	public class JymServerUpdater : MonoBehaviour {
		private void Update() {
			SRVR.Update(Time.time);
		}
	}
}
