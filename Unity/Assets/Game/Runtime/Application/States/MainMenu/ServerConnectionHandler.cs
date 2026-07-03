using System;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Application {
	public class ServerConnectionHandler : MonoBehaviour {
		[SerializeField] private float _connectionTimeout = 2f;
		[SerializeField] private string _presetValue = string.Empty;

		private TextField _address;
		private Button _connect;

		private void Start() {
			_address = App.Get<MainMenuUI>().Document.rootVisualElement.Q<TextField>("Address");
			_connect = App.Get<MainMenuUI>().Document.rootVisualElement.Q<Button>("Connect");

			_address.SetValueWithoutNotify(_presetValue);
			_connect.RegisterCallback<ClickEvent>(OnConnectClicked);
		}

		private void OnConnectClicked(ClickEvent e) {
			if (!TryParseAddressAndPort(out var ipEndPoint)) {
				return;
			}

			TryConnect(ipEndPoint, destroyCancellationToken).Forget();
		}

		private async UniTask TryConnect(IPEndPoint ipEndPoint, CancellationToken token) {
			_address.SetEnabled(false);
			_connect.SetEnabled(false);

			var serverConnection = App.Get<AppServerConnection>().Connection;
			serverConnection.Connect(ipEndPoint);

			await UniTask.WhenAny(
				UniTask.Delay(TimeSpan.FromSeconds(_connectionTimeout), cancellationToken: token),
				UniTask.WaitUntil(() => serverConnection.IsConnected, cancellationToken: token));

			if (serverConnection.IsConnected) {
				App.Get<StateMachine>().Enter<Gameplay>();
			}
			else {
				serverConnection.Close();
				_address.SetEnabled(true);
				_connect.SetEnabled(true);
			}
		}

		private bool TryParseAddressAndPort(out IPEndPoint ipEndPoint) {
			ipEndPoint = null;

			var addressAndPort = _address.value;

			if (string.IsNullOrWhiteSpace(addressAndPort)) {
				return false;
			}

			var addressAndPortSplit = addressAndPort.Split(':');

			if (addressAndPortSplit.Length != 2) {
				return false;
			}

			var address = addressAndPortSplit[0].Trim();
			var port = addressAndPortSplit[1].Trim();

			if (!IPAddress.TryParse(address, out var ip)) {
				return false;
			}

			if (!ushort.TryParse(port, out var portNumber)) {
				return false;
			}

			ipEndPoint = new IPEndPoint(ip, portNumber);
			return true;
		}
	}
}
