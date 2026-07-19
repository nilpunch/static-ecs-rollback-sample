using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Client {
	public struct GameWorldPrev : IWorldType, ISessionType { }

	/// <summary>
	/// Previous GameWorld state.
	/// </summary>
	public abstract class WP : World<GameWorldPrev> { }
}
