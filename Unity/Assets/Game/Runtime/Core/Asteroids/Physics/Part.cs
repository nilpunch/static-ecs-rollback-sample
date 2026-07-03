using FFS.Libraries.StaticEcs;

namespace Game.Core {
	public struct Part : ILinksType {
		public void OnAdd<TW>(World<TW>.Entity self, EntityGID link) where TW : struct, IWorldType {
			link.TryAddLink<TW, Body>(self);
		}

		public void OnDelete<TW>(World<TW>.Entity self, EntityGID link, HookReason reason) where TW : struct, IWorldType {
			link.TryDeleteLink<TW, Body>(self);
		}
	}
}
