using System;
using Fixed32;
using UnityEditor;
using UnityEngine;
using Collider = Game.Core.Collider;

namespace Game.Client {
	public class ColliderAutoSize : EntityBehaviour {
		[SerializeField] private SpriteRenderer _sprite;
		[SerializeField] private float _originalSpriteRadius = 0.32f;

		protected override void OnEntityAssigned() {
			var actualRadius = Entity.Read<Collider>()!.Radius.ToFloat();

			_sprite.transform.localScale = actualRadius / _originalSpriteRadius * Vector3.one;
		}

		private void OnDrawGizmosSelected() {
			if (_sprite == null) {
				return;
			}

			Handles.Disc(_sprite.transform.rotation,
				_sprite.transform.position,
				Vector3.forward,
				_originalSpriteRadius,
				default,
				default);
		}
	}
}
