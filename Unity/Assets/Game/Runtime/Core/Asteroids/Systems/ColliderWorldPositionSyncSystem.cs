using FFS.Libraries.StaticEcs;
using Shenanicode.Rollback;

namespace Game.Core {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class ColliderWorldPositionSyncSystem : ISystem {
			public void Update() {
				W.Query<None<W.Link<Body>>>().For(static (ref Collider collider, in PhysicalBody physicalBody) => {
					collider.WorldPosition = physicalBody.WorldOrigin + physicalBody.Rotation.Counterclockwise * collider.Offset;
				});

				W.Query().For(static (ref Collider collider, in W.Link<Body> bodyLink) => {
					if (!bodyLink.Value.TryUnpack<TWorld>(out var body)) {
						return;
					}

					ref readonly var transform = ref body.Read<PhysicalBody>()!;
					collider.WorldPosition = transform.WorldOrigin + transform.Rotation.Counterclockwise * collider.Offset;
				});
			}
		}
	}
}
