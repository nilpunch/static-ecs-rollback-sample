using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public abstract class W : World<TWorld> { }

		public abstract class S : Session<TWorld> { }

		public struct GameSystemsType : ISystemsType { }

		public abstract class Systems : W.Systems<GameSystemsType> { }
	}
}
