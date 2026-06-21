using FFS.Libraries.StaticEcs;
using UnityEngine;
using Game.Utils;

namespace Game.Client {
	[DefaultExecutionOrder(short.MinValue)]
	public class AppWorldLifetime : MonoBehaviour {
		private void Awake() {
			App.Create();

			ResourceConfigUtils.SetResourceConfigs<AppWorldType>();

			App.Initialize();
		}

		private void OnDestroy() {
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
					App.Destroy();
					Debug.Log("Emergency cleanup was triggered due to an unsafe exit from Play Mode.");
				}
				UnityEditor.EditorApplication.playModeStateChanged -= EditorApplicationOnPlayModeStateChanged;
			}
		}
		#endif
	}
}
