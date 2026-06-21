using FFS.Libraries.StaticEcs;

namespace Game.Client
{
	public struct GameWorldPrev : IWorldType { }

	public abstract class WP : World<GameWorldPrev> { }
}
