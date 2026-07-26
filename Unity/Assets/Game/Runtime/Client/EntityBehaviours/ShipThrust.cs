using Game.Core;
using UnityEngine;

namespace Game.Client {
	public class ShipThrust : EntityBehaviour {
		[SerializeField] private ParticleSystem[] _thrustParticleSystems;

		private void Update() {
			foreach (var system in _thrustParticleSystems) {
				var module = system.emission;
				if (Entity.Read<Ship>()!.Thrust > 0) {
					if (!system.isPlaying) {
						system.Play();
					}
					module.enabled = true;
				}
				else {
					module.enabled = false;
				}
			}
		}
	}
}
