using System;
using Game.Core;
using Game.Utils;
using UnityEngine;

namespace Game.Client {
	public static class EntityViewFactory {
		private static VariantPool<ViewAsset, EntityView> _viewPool = new();
		private static Transform[] _poolRoots = new Transform[4];

		public static EntityView CreateView(ViewAsset viewAsset) {
			if (!_viewPool.ContainsVariant(viewAsset)) {
				var viewPrefab = ViewDataBase.Instance.GetViewPrefab(viewAsset);
				var poolRoot = new GameObject(viewPrefab.name + " Pool").transform;
				_viewPool.AddVariant(viewAsset, new Pool<EntityView>(new PrefabFactory<EntityView>(viewPrefab, poolRoot)));

				if (_poolRoots.Length <= viewAsset.Id) {
					Array.Resize(ref _poolRoots, viewAsset.Id << 1);
				}

				_poolRoots[viewAsset.Id] = poolRoot;
			}

			var view = _viewPool.Get(viewAsset);
			return view;
		}

		public static void DestroyView(EntityView view) {
			view.transform.SetParent(_poolRoots[_viewPool.GetKey(view).Id]);
			_viewPool.Return(view);
		}

		public static void Reset() {
			_viewPool = new VariantPool<ViewAsset, EntityView>();
			Array.Fill(_poolRoots, null);
		}
	}
}
