using FFS.Libraries.StaticPack;
using Game.Core;
using Shenanicode.Rollback;

namespace Game.Core
{
	public class GameWorldFullSyncHandler : IFullSyncHandler {
		public void WriteFullSync(ref BinaryPackWriter writer) {
			W.Serializer.CreateWorldSnapshot(ref writer);
		}

		public void ReadFullSync(ref BinaryPackReader reader) {
			W.Serializer.LoadWorldSnapshot(reader, hardReset: true);
		}
	}
}
