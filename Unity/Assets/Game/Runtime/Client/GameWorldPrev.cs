using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Client {
	public struct GameWorldPrev : IWorldType { }

	/// <summary>
	/// Previous GameWorld state.
	/// </summary>
	public abstract class WP : Core<GameWorldPrev>.W { }
}
