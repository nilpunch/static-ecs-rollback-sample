using System;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Client;
using Game.Core;
using UnityEngine;

namespace Game.Application {
	[DefaultExecutionOrder(-1)]
	public class ClientUpdater : MonoBehaviour {
		[SerializeField] private bool _simulateLatency;

		private void Update() {
			CLNT.Update(ClientTime.CurrentTime);

			if (!CLNT.Connection.IsConnected && !App.Get<AppLocalServer>().IsLocalServer) {
				App.Get<StateMachine>().Enter<MainMenu>();
				return;
			}

			App.Get<AppServerConnection>().Connection.SimulateLatency = _simulateLatency;
		}

		private void OnGUI() {
			var color = _simulateLatency ? Color.red : Color.white;
			var style = new GUIStyle(GUI.skin.label) {
				alignment = TextAnchor.UpperCenter,
				fontSize = 20,
				fontStyle = FontStyle.Bold,
				normal = { textColor = color, background = null },
				hover = { textColor = color, background = null },
				active = { textColor = color, background = null },
				focused = { textColor = color, background = null },
				onNormal = { textColor = color, background = null },
				onHover = { textColor = color, background = null },
				onActive = { textColor = color, background = null },
				onFocused = { textColor = color, background = null }
			};

			var rect = new Rect(0, 10, Screen.width, 30);
			GUI.Label(rect, $"Simulate Latency: {_simulateLatency}", style);
		}
	}
}
