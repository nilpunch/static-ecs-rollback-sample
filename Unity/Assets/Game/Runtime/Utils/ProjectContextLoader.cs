using UnityEngine;

namespace Game.Utils {
	internal static class ProjectContextLoader {
		private const string PrefabPath = "ProjectContext";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init() {
			var prefab = Resources.Load<GameObject>(PrefabPath);

			if (prefab == null) {
				Debug.LogError($"ProjectContext prefab not found at Resources/{PrefabPath}");
				return;
			}

			var instance = Object.Instantiate(prefab);
			Object.DontDestroyOnLoad(instance);
		}
	}
}
