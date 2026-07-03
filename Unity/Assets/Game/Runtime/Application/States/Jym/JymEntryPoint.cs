using UnityEngine;

namespace Game.Application
{
	public class JymEntryPoint : MonoBehaviour {
		private void Start() {
			App.Get<StateMachine>().Enter<Jym>();
		}
	}
}
