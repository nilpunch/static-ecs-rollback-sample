using FFS.Libraries.StaticEcs;
using UnityEngine;

namespace Game.Client {
	/// <summary>
	/// MonoBehaviour base to automatically register instance in <see cref="AppWorldType"/> resources.
	/// </summary>
	public class AppBehaviour<T> : MonoBehaviour, IResource where T : AppBehaviour<T> {
		protected virtual void Awake() {
			App.SetResource((T)this);
		}

		protected virtual void OnDestroy() {
			if (App.Status == WorldStatus.Created) {
				App.RemoveResource<T>();
			}
		}
	}
}
