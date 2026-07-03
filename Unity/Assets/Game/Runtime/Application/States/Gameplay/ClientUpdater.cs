using Game.Client;
using UnityEngine;

namespace Game.Application
{
	public class ClientUpdater : MonoBehaviour {
		private void Update() {
			CLNT.Update(Time.realtimeSinceStartup);

			if (!CLNT.Connection.IsConnected) {
				App.Get<StateMachine>().Enter<MainMenu>();
			}
		}
	}
}
