using Fixed32;
using Game.Core;
using UnityEngine;

namespace Game.Client {
	public class AutoCorrectMaxSpeed : EntityBehaviour {
		[SerializeField] private EntityView _entityView;

		private float _maxSpeed;

		private void Awake() {
			_maxSpeed = _entityView.PoseCorrectionSpeed;
		}

		protected override void OnEntityAssigned() {
			if (Entity.Has<Velocity>()) {
				enabled = true;
				TryUpdateVelocity();
			}
			else {
				_entityView.PoseCorrectionSpeed = _maxSpeed;
				enabled = false;
			}
		}

		private void Update() {
			TryUpdateVelocity();
		}

		private void TryUpdateVelocity() {
			var entitySpeed = FVector2.Length(Entity.Read<Velocity>()!.Linear).ToFloat();

			_entityView.PoseCorrectionSpeed = Mathf.Max(_maxSpeed, entitySpeed * 2);
		}
	}
}
