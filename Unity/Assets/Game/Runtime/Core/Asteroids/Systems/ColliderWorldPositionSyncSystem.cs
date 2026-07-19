using System.Runtime.CompilerServices;
using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;
using Const = Game.Core.Const;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class ColliderWorldPositionSyncSystem : ISystem {
			public void Update() {
				W.Query<None<W.Link<Body>>>()
				.Write<Collider>()
				.Read<PhysicalBody, Bounds>()
				.For<SingleColliderBodies>();

				// W.Query().For(static (ref Collider collider, in W.Link<Body> bodyLink) => {
				// 	if (!bodyLink.Value.TryUnpack<TWorld>(out var body)) {
				// 		return;
				// 	}
				//
				// 	ref readonly var physicalBody = ref body.Read<PhysicalBody>()!;
				// 	collider.WorldPosition = physicalBody.WorldCoM + (physicalBody.Rotation.Counterclockwise * (physicalBody.OriginOffset + collider.Offset));
				// });
			}

			private struct SingleColliderBodies : W.IQuery.Write<Collider>.Read<PhysicalBody, Bounds> {
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public void Invoke(World<TWorld>.Entity entity, ref Collider collider, in PhysicalBody physicalBody, in Bounds bounds) {
					collider.WorldPosition = physicalBody.WorldCoM + physicalBody.Rotation.Counterclockwise * (physicalBody.OriginOffset + collider.Offset);

					var positionDelta = collider.WorldPosition - bounds.WorldPosition;

					if (FP.Abs(positionDelta.X) >= Const.BoundsPadding || FP.Abs(positionDelta.Y) >= Const.BoundsPadding) {
						entity.Mut<Bounds>().WorldPosition = collider.WorldPosition;
					}
				}
			}
		}
	}
}
