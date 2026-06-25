using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using Game.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Client {
	public class Gameplay : AppBehaviour<Gameplay>, IState {
		[SerializeField] private SceneReference _scene;

		public async UniTask Enter(CancellationToken token) {
			var connection = App.Get<ServerConnection>().Connection;

			CLNT.Create(GameSessionSetup.SessionConfig, connection, new GameWorldFullSyncHandler(), logger: new UnityLogger("Client"));
			S.SetInterpolationReceiver(new GameInterpolationReceiver());
			GameSessionSetup.Register();
			CLNT.Initialize();
			GameWorldSetup.CreateAndInitialize();

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
