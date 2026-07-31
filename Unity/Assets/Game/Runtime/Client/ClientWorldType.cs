using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Client {
	public struct ClientWorldType : IWorldType { }

	internal abstract class W : Core<ClientWorldType>.W { }

	internal abstract class S : Session<Core<ClientWorldType>.SessionType> { }
}
