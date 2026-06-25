using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback.LiteNetLib;

namespace Game.Client {
	public class ServerConnection : IResource {
		public LiteNetLibServerConnection Connection { get; } = new();
	}
}
