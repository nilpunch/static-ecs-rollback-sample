using FFS.Libraries.StaticEcs;
using UnityEngine;

namespace Game.Application {
	/// <summary>
	/// MonoBehaviour base to automatically register instance in <see cref="AppWorldType"/> resources.
	/// </summary>
	public class AppBehaviour<T> : MonoBehaviour, IResource where T : AppBehaviour<T> {
		protected virtual void Awake() {
			if (GetType() != typeof(T)) {
				Debug.LogError("Type mismatch!");
			}
			if (App.HasResource<T>()) {
				Debug.LogError("Duplicate resource!");
			}
			App.Set((T)this);
		}

		protected virtual void OnDestroy() {
			if (App.Status != WorldStatus.NotCreated) {
				App.RemoveResource<T>();
			}
		}
	}
}
