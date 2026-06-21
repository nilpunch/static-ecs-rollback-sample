using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Core {
	public struct GameWorld : IWorldType, ISessionType { }

	public struct GameWorldPrev : IWorldType { }

	public abstract class W : World<GameWorld> { }

	public abstract class S : Session<GameWorld> { }

	public struct GameSystemsType : ISystemsType { }

	public abstract class Systems : W.Systems<GameSystemsType> { }
}
