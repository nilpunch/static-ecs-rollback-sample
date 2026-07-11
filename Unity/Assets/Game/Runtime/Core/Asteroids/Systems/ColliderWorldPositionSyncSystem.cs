using FFS.Libraries.StaticEcs;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class ColliderWorldPositionSyncSystem : ISystem {
			public void Update() {
				W.Query<None<W.Link<Body>>>().For(static (ref Collider collider, in PhysicalBody physicalBody) => {
					collider.WorldPosition = physicalBody.WorldCoM + physicalBody.Rotation.Counterclockwise * (physicalBody.OriginOffset + collider.Offset);
				});

				W.Query().For(static (ref Collider collider, in W.Link<Body> bodyLink) => {
					if (!bodyLink.Value.TryUnpack<TWorld>(out var body)) {
						return;
					}

					ref readonly var physicalBody = ref body.Read<PhysicalBody>()!;
					collider.WorldPosition = physicalBody.WorldCoM + physicalBody.Rotation.Counterclockwise * (physicalBody.OriginOffset + collider.Offset);
				});
			}
		}
	}
}
