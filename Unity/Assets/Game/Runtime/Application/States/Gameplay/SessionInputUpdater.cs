using Fixed32;
using Game.Client;
using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using static Game.Core<Game.Client.ClientWorld>;

namespace Game.Application
{
	public class SessionInputUpdater : MonoBehaviour {
		[SerializeField] private InputActionReference _moveInput;
		[SerializeField] private float _radius = 4;
		[SerializeField] private float _accelration = 10;

		private void OnEnable() {
			_moveInput.ToInputAction().Enable();
		}

		private void OnDisable() {
			_moveInput.ToInputAction().Disable();
		}

		private void Update() {
			var move  = _moveInput.ToInputAction().ReadValue<Vector2>();

			var moveInput = new MoveInput {
				Up    = move.y >  0.5f,
				Down  = move.y < -0.5f,
				Left  = move.x < -0.5f,
				Right = move.x >  0.5f,
			};

			S.SetPredictionInput(CLNT.Channel, moveInput);

			if (Mouse.current.leftButton.isPressed) {
				var mousePosition = Camera.main.ScreenToWorldPoint(
					new Vector3(Mouse.current.position.value.x, Mouse.current.position.value.y, 0f)).ToFP().ToXY();

				var debugInput = new DebugInput() {
					MousePosition = mousePosition,
					Acceleration = _accelration.ToFP(),
					Radius = _radius.ToFP(),
				};
				S.SetPredictionInput(0, debugInput);
			}
			else {
				S.SetPredictionInput(0, new DebugInput());
			}
		}
	}
}
