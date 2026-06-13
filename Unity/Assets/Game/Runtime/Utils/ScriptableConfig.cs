using System;
using System.Linq;
using UnityEngine;

namespace Game.Utils {
	public abstract class ScriptableConfig : ScriptableObject {
		public static ScriptableConfig[] GetAll() {
			return Resources.FindObjectsOfTypeAll<ScriptableConfig>()
							#if UNITY_EDITOR
							.Where(i => i.IsMainInstance)
							#endif
							.ToArray();
		}

		#if UNITY_EDITOR
		public abstract bool IsMainInstance { get; }
		public abstract void SetAsMainInstance();
		#endif
	}

	public class ScriptableConfig<T> : ScriptableConfig where T : ScriptableConfig<T> {
		private static T _instance;

		public static T Instance {
			get {
				if (_instance != null) {
					return _instance;
				}

				_instance = Resources.FindObjectsOfTypeAll<T>()
									.FirstOrDefault(
										#if UNITY_EDITOR
										i => i.IsMainInstance
										#endif
									);

				if (_instance == null) {
					Debug.LogError(typeof(T).Name + " could not be loaded.");
				}

				return _instance;
			}
		}

		#if UNITY_EDITOR
		public override bool IsMainInstance => UnityEditor.PlayerSettings.GetPreloadedAssets().Contains(this);

		public override void SetAsMainInstance() {
			if (Application.isPlaying) {
				return;
			}

			_instance = null;

			var preloads = UnityEditor.PlayerSettings.GetPreloadedAssets();
			var index = Array.FindIndex(preloads, p => p is T || p == null);
			if (index >= 0) {
				preloads[index] = this;
			}
			else {
				Array.Resize(ref preloads, preloads.Length + 1);
				preloads[^1] = this;
			}
			UnityEditor.PlayerSettings.SetPreloadedAssets(preloads);
		}

		private void Awake() {
			if (Application.isPlaying) {
				return;
			}

			var preloads = UnityEditor.PlayerSettings.GetPreloadedAssets();
			if (preloads.Any(p => p is T)) {
				return;
			}

			Array.Resize(ref preloads, preloads.Length + 1);
			preloads[^1] = this;
			UnityEditor.PlayerSettings.SetPreloadedAssets(preloads);
		}
		#endif
	}

	#if UNITY_EDITOR
	[UnityEditor.CanEditMultipleObjects]
	[UnityEditor.CustomEditor(typeof(ScriptableConfig), true)]
	internal class ScriptableConfigEditor : UnityEditor.Editor {
		public override void OnInspectorGUI() {
			if (DrawSetMainInstanceButton()) {
				UnityEditor.EditorGUILayout.Space();
			}

			DrawDefaultInspector();
		}

		private bool DrawSetMainInstanceButton() {
			if (Application.isPlaying || targets.Length > 1) {
				return false;
			}

			var scriptableConfig = target as ScriptableConfig;
			if (scriptableConfig == null) {
				return false;
			}
			if (scriptableConfig.IsMainInstance) {
				return false;
			}

			if (GUILayout.Button("Set As Main Instance", GUILayout.Height(25))) {
				scriptableConfig.SetAsMainInstance();
			}

			return true;
		}
	}
	#endif
}
