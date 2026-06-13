using FFS.Libraries.StaticEcs;

namespace Game.Client {
	public struct AppWorldType : IWorldType { }

	public abstract class App : World<AppWorldType> { }
}
