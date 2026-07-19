using Game.Client;
using UnityEngine;

namespace Game.Application
{
	public class SimulationCatchingUp : MonoBehaviour {
		[SerializeField] private GameObject _gameObject;

		private void Update() {
			_gameObject.SetActive(CLNT.IsSimulationCatchingUp);
		}
	}
}
