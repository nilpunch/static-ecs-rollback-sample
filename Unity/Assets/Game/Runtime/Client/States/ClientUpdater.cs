using UnityEngine;

namespace Game.Client
{
	public class ClientUpdater : MonoBehaviour {
		private void Update() {
			CLNT.Update(Time.realtimeSinceStartup);
		}
	}
}
