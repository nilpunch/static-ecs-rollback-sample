using UnityEngine;

namespace Game.Application {
	public class EntryPoint : MonoBehaviour {
		private void Start() {
			App.Get<StateMachine>().Enter<MainMenu>();
		}
	}
}
