using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using Game.Client;
using Shenanicode.Rollback;
using Shenanicode.Rollback.LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Application {
	public class Jym : AppBehaviour<Jym>, IState {
		[SerializeField] private SceneReference _scene;
		[SerializeField] private int _port = 1887;
		[SerializeField] private int _numberOfAsteroids = 100;

		public async UniTask Enter(CancellationToken token) {
			// Server.
			var clientListener = new LiteNetLibRemoteClientListener(_port);
			JymServerSetup.CreateAndInitialize(clientListener, _numberOfAsteroids);
			clientListener.Start();

			// Client.
			var connection = App.Get<AppServerConnection>().Connection;
			ClientSetup.CreateAndInitialize(connection, TickSyncConfig.Robust);
			connection.Connect(new IPEndPoint(IPAddress.Loopback, _port));

			await SceneManager.LoadSceneAsync(_scene.Path, LoadSceneMode.Additive).ToUniTask(cancellationToken: token);
			SceneManager.SetActiveScene(_scene.LoadedScene);
		}

		public async UniTask Exit(CancellationToken token) {
			await SceneManager.UnloadSceneAsync(_scene.LoadedScene).ToUniTask(cancellationToken: token);

			ClientSetup.Destroy();
			JymServerSetup.Destroy();
		}
	}
}
