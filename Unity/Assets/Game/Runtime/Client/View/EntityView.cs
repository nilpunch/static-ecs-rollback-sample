using System;
using System.Collections.Generic;
using FFS.Libraries.StaticEcs;
using UnityEngine;

namespace Game.Client {
	public class EntityView : MonoBehaviour {
		[SerializeField] private List<EntityBehaviour> _entityBehaviours = new();

		[field: SerializeField] public Transform RootTransform { get; private set; }
		[field: SerializeField] public float PoseCorrectionSpeed { get; set; } = 10;
		[field: SerializeField] public float AngleCorrectionSpeed { get; set; } = 180;

		public EntityGID Entity { get; protected set; }

		private void Reset() {
			RootTransform = transform;
		}

		public void AssignEntity(EntityGID entity) {
			Entity = entity;

			gameObject.SetActive(true);

			foreach (var viewBehaviour in _entityBehaviours) {
				viewBehaviour.AssignEntity(entity);
			}
		}

		public void RemoveEntity() {
			foreach (var viewBehaviour in _entityBehaviours) {
				viewBehaviour.RemoveEntity();
			}

			gameObject.SetActive(false);

			Entity = default;
		}

		#if UNITY_EDITOR
		[ContextMenu("Find Behaviours and Components")]
		public void CollectViewBehaviours() {
			UnityEditor.Undo.RecordObject(this, "Find behaviours");
			var behaviours = GetComponentsInChildren<EntityBehaviour>(true);

			_entityBehaviours.Clear();
			foreach (var behaviour in behaviours) {
				_entityBehaviours.Add(behaviour);
			}

			UnityEditor.EditorUtility.SetDirty(this);
		}
		#endif
	}
}
