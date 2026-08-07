using UnityEngine;

namespace Game.Application {
	public static class ClientTime {
		public static float CurrentTime => App.Get<AppLocalServer>().IsLocalServer ? Time.time : Time.realtimeSinceStartup;
	}
}
