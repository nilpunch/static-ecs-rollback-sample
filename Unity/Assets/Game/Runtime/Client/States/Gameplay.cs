using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using Game.Core;
using Shenanicode.Rollback.LiteNetLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client {
	public class Gameplay : AppBehaviour<Gameplay>, IState {
		[SerializeField] private SceneReference _scene;

		public async UniTask Enter(CancellationToken token) {
			var connection = new LiteNetLibServerConnection();

			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(), logger: new UnityLogger("Client"));
			S.SetInterpolationReceiver(new GameInterpolationReceiver());
			GameSessionSetup.Register();
			CLNT.Initialize();

			GameWorldSetup.Create();
			GameWorldSetup.Register();
			GameWorldSetup.Initialize();

			connection.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7777));

			await UniTask.WhenAny(
				UniTask.WaitUntil(() => connection.IsConnected, cancellationToken: token),
				UniTask.WaitForSeconds(2f, cancellationToken: token));

			if (!connection.IsConnected) {
				App.GetResource<StateMachine>().Enter<MainMenu>();
				return;
			}

			await SceneManager.LoadSceneAsync(_scene.Path, LoadSceneMode.Additive).ToUniTask(cancellationToken: token);
			SceneManager.SetActiveScene(_scene.LoadedScene);
		}

		public async UniTask Exit(CancellationToken token) {
			await SceneManager.UnloadSceneAsync(_scene.LoadedScene).ToUniTask(cancellationToken: token);

			GameWorldSetup.Destroy();
			CLNT.Destroy();
		}
	}
}
