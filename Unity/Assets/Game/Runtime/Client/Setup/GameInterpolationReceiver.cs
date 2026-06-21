using FFS.Libraries.StaticPack;
using Game.Core;
using Shenanicode.Rollback;

namespace Game.Client
{
	public class GameInterpolationReceiver : IInterpolationReceiver {
		public void SaveInterpolationState() {
			var binaryPackWriter = BinaryPackWriter.CreateFromPool();
			W.Serializer.CreateWorldSnapshot(ref binaryPackWriter);
			WP.Serializer.LoadWorldSnapshot(binaryPackWriter.AsReader(), true);
		}
	}
}
