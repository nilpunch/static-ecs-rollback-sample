using FFS.Libraries.StaticPack;
using Shenanicode.Rollback;

namespace Game.Core {
	public class GameRollback : IRollback {
		private const int EntitiesCount = 10000;
		private const int BytesPerEntity = 64;
		private const int WorldSnapshotLength = EntitiesCount * BytesPerEntity;

		private readonly CyclicFrameCounter _cyclicFrameCounter;
		private readonly BinaryPackWriter[] _worldFrames;

		public GameRollback(int framesCapacity) {
			_cyclicFrameCounter = new CyclicFrameCounter(framesCapacity);
			_worldFrames = new BinaryPackWriter[framesCapacity];
			for (var i = 0; i < _worldFrames.Length; i++) {
				_worldFrames[i] = BinaryPackWriter.Create(new byte[WorldSnapshotLength]);
			}
		}

		public int CanRollbackFrames => _cyclicFrameCounter.CanRollbackFrames;

		public void SaveFrame() {
			_cyclicFrameCounter.SaveFrame();

			W.Serializer.CreateWorldSnapshot(ref _worldFrames[_cyclicFrameCounter.CurrentFrame]);
		}

		public void Rollback(int frames) {
			_cyclicFrameCounter.Rollback(frames);

			W.Serializer.LoadWorldSnapshot(_worldFrames[_cyclicFrameCounter.CurrentFrame].AsReader(), hardReset: true);
		}
	}
}
