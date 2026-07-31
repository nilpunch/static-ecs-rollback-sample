using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, IWorldType {
		public abstract class W : World<TWorld> { }

		public struct SessionType : ISessionType { }

		public abstract class S : Session<SessionType> { }

		public struct GameSystemsType : ISystemsType { }

		public abstract class Systems : W.Systems<GameSystemsType> { }
	}
}
