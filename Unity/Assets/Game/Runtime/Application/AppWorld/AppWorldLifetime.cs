using FFS.Libraries.StaticEcs;
using Game.Client;
using UnityEngine;

namespace Game.Application {
	[DefaultExecutionOrder(short.MinValue)]
	public class AppWorldLifetime : MonoBehaviour {
		private void Awake() {
			App.Create();

			AppGlobalResources.SetResources();

			App.Initialize();
		}

		private void OnDestroy() {
			if (App.Status != WorldStatus.NotCreated) {
				CleanupStatic();
			}
		}

		private static void CleanupStatic() {
			ClientSetup.Destroy();
			AppGlobalResources.Dispose();
			App.Destroy();
		}

		#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void SubscribeCleanup() {
			UnityEditor.EditorApplication.playModeStateChanged += EditorApplicationOnPlayModeStateChanged;
		}

		private static void EditorApplicationOnPlayModeStateChanged(UnityEditor.PlayModeStateChange state) {
			if (state == UnityEditor.PlayModeStateChange.EnteredEditMode) {
				if (App.Status != WorldStatus.NotCreated) {
					CleanupStatic();
					Debug.Log("Emergency cleanup was triggered due to an unsafe exit from Play Mode.");
				}
				UnityEditor.EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
			}
		}
		#endif
	}
}
