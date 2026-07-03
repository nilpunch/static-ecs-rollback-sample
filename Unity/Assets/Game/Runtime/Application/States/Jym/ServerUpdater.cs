using Game.Client;
using UnityEngine;

namespace Game.Application {
	public class ServerUpdater : MonoBehaviour {
		private void Update() {
			SRVR.Update(Time.realtimeSinceStartup);
		}
	}
}
