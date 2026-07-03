using FFS.Libraries.StaticEcs;

namespace Game.Client {
	public struct GameWorldPrev : IWorldType { }

	/// <summary>
	/// Previous GameWorld state.
	/// </summary>
	public abstract class WP : World<GameWorldPrev> { }
}
