using Game.Client;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static Game.Core<Game.Client.ClientWorld>;

namespace Game.Application
{
	public class SessionInputUpdater : MonoBehaviour {
		[SerializeField] private InputActionReference _moveInput;

		private void OnEnable() {
			_moveInput.ToInputAction().Enable();
		}

		private void OnDisable() {
			_moveInput.ToInputAction().Disable();
		}

		private void Update() {
			var move  = _moveInput.ToInputAction().ReadValue<Vector2>();

			var input = new MoveInput {
				Up    = move.y >  0.5f,
				Down  = move.y < -0.5f,
				Left  = move.x < -0.5f,
				Right = move.x >  0.5f,
			};

			S.SetPredictionInput(CLNT.Channel, input);
		}
	}
}
