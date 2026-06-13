using UnityEngine;

namespace Game.Client {
	public class EntryPoint : MonoBehaviour {
		private void Start() {
			App.GetResource<StateMachine>().Enter<Gameplay>();
		}
	}
}
