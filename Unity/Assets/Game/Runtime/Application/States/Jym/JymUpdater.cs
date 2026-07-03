using Game.Core;
using UnityEngine;

namespace Game.Application
{
	public class JymUpdater : MonoBehaviour {
		private void Update() {
			S.FastForwardToTick(Mathf.RoundToInt(Time.realtimeSinceStartup * S.TickRate));
		}
	}
}
