using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback.LiteNetLib;

namespace Game.Application {
	public class AppServerConnection : IResource {
		public LiteNetLibServerConnection Connection { get; } = new();
	}
}
