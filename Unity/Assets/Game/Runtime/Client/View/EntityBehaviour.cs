using FFS.Libraries.StaticEcs;
using UnityEngine;

namespace Game.Client {
	public abstract class EntityBehaviour : MonoBehaviour {
		public EntityGID EntityGID { get; private set; }
		public W.Entity Entity => EntityGID.Unpack<ClientWorld>();

		public void AssignEntity(EntityGID entity) {
			EntityGID = entity;
			OnEntityAssigned();
		}

		public void RemoveEntity() {
			OnEntityRemoved();
			EntityGID = default;
		}

		protected virtual void OnEntityAssigned() { }

		protected virtual void OnEntityRemoved() { }
	}
}
