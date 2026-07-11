using FFS.Libraries.StaticEcs;

namespace Game.Core {
	public struct ContactPair : IMultiComponent {
		public EntityGID Other;

		public ContactPair(EntityGID other) {
			Other = other;
		}
	}
}
