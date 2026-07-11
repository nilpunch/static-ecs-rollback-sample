using FFS.Libraries.StaticEcs;
using UnityEngine;

namespace Game.Application {
	public abstract class EntityBehaviour : MonoBehaviour {
		public EntityGID Entity { get; private set; }

		public void AssignEntity(EntityGID entity) {
			Entity = entity;
			OnEntityAssigned();
		}

		public void RemoveEntity() {
			OnEntityRemoved();
			Entity = default;
		}

		protected virtual void OnEntityAssigned() { }

		protected virtual void OnEntityRemoved() { }
	}
}
