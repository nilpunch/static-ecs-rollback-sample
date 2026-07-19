using System.Threading;
using Cysharp.Threading.Tasks;
using Eflatun.SceneReference;
using Game.Client;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Application {
	public class Gameplay : AppBehaviour<Gameplay>, IState {
		[SerializeField] private SceneReference _scene;
		[SerializeField] private double _maxResimulationMs = 10;

		public async UniTask Enter(CancellationToken token) {
			var connection = App.Get<AppServerConnection>().Connection;

			ClientSetup.CreateAndInitialize(connection, maxResimulationMillis: _maxResimulationMs);

			await SceneManager.LoadSceneAsync(_scene.Path, LoadSceneMode.Additive).ToUniTask(cancellationToken: token);
			SceneManager.SetActiveScene(_scene.LoadedScene);
		}

		public async UniTask Exit(CancellationToken token) {
			await SceneManager.UnloadSceneAsync(_scene.LoadedScene).ToUniTask(cancellationToken: token);

			ClientSetup.Destroy();
		}
	}
}
