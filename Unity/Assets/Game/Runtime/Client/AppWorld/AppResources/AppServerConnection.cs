using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback.LiteNetLib;

namespace Game.Client {
	public class AppServerConnection : IResource {
		public LiteNetLibServerConnection Connection { get; } = new();
	}
}
