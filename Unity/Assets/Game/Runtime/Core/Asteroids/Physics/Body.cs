using FFS.Libraries.StaticEcs;

namespace Game.Core {
	public struct Body : ILinkType {
		public void OnAdd<TW>(World<TW>.Entity self, EntityGID link) where TW : struct, IWorldType {
			link.TryAddLinkItem<TW, Part>(self);
		}

		public void OnDelete<TW>(World<TW>.Entity self, EntityGID link, HookReason reason) where TW : struct, IWorldType {
			link.TryDeleteLinkItem<TW, Part>(self);
		}
	}
}
