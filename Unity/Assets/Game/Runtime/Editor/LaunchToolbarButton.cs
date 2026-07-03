using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityToolbarExtender;

namespace Game.Application {
	[InitializeOnLoad]
	internal static class LaunchToolbarButton {
		private static readonly (string Path, string Name)[] ScenePaths = {
			("Assets/Game/Scenes/Launcher.unity", "Main"),
			("Assets/Game/Scenes/JymLauncher.unity", "Jym"),
		};

		private const string k_SessionKey = "LaunchToolbarButton_SceneSetup";

		static LaunchToolbarButton() {
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		private static void OnToolbarGUI() {
			GUILayout.FlexibleSpace();

			using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) {
				foreach (var (path, name) in ScenePaths) {
					if (GUILayout.Button(new GUIContent(name, $"Open {name} scene and enter Play mode"), GUILayout.Width(90f))) {
						PlayScene(path);
					}
				}
			}
		}

		private static void PlayScene(string path) {
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
				return;
			}

			var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
			if (sceneAsset == null) {
				Debug.LogError($"[{nameof(LaunchToolbarButton)}] Scene not found at: {path}");
				return;
			}

			// Snapshot current scene setup before switching
			SaveSceneSetup();

			EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
			EditorApplication.isPlaying = true;
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state) {
			if (state == PlayModeStateChange.EnteredEditMode) {
				RestoreSceneSetup();
			}
		}

		private static void SaveSceneSetup() {
			var setup = EditorSceneManager.GetSceneManagerSetup();
			var json = JsonUtility.ToJson(new SceneSetupWrapper { setups = setup });
			SessionState.SetString(k_SessionKey, json);
		}

		private static void RestoreSceneSetup() {
			var json = SessionState.GetString(k_SessionKey, null);
			if (string.IsNullOrEmpty(json))
				return;

			var wrapper = JsonUtility.FromJson<SceneSetupWrapper>(json);
			if (wrapper?.setups?.Length > 0) {
				EditorSceneManager.RestoreSceneManagerSetup(wrapper.setups);
			}

			SessionState.EraseString(k_SessionKey);
		}

		[Serializable]
		private class SceneSetupWrapper {
			public SceneSetup[] setups;
		}
	}
}
