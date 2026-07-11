using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Client {
	public struct ClientWorld : IWorldType, ISessionType { }

	internal abstract class W : World<ClientWorld> { }

	internal abstract class S : Session<ClientWorld> { }
}
