using Fixed32;
using Game.Core;
using UnityEngine;

namespace Game.Client {
	public class ShipThrust : EntityBehaviour {
		[SerializeField] private ParticleSystem _thrust;
		[SerializeField] private ParticleSystem _brake;
		[SerializeField] private ParticleSystem _left;
		[SerializeField] private ParticleSystem _right;

		private void Update() {
			var thrustEmission = _thrust.emission;
			var brakeEmission = _brake.emission;
			var leftEmission = _left.emission;
			var rightEmission = _right.emission;

			var thrust = Entity.Read<Ship>()!.Thrust;
			var turn = Entity.Read<Ship>()!.Turn;

			if (thrust > 0) {
				thrustEmission.enabled = true;
				brakeEmission.enabled = false;
			}
			else if (thrust < 0) {
				thrustEmission.enabled = false;
				brakeEmission.enabled = true;
			}
			else {
				thrustEmission.enabled = false;
				brakeEmission.enabled = false;
			}

			if (turn > FAngle.Zero) {
				rightEmission.enabled = true;
				leftEmission.enabled = false;
			}
			else if (turn < FAngle.Zero) {
				rightEmission.enabled = false;
				leftEmission.enabled = true;
			}
			else {
				rightEmission.enabled = false;
				leftEmission.enabled = false;
			}
		}
	}
}
