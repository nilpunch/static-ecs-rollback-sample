using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class PlayerCreationSystem : ISystem {
			public void Update() {
				var mapping = W.GetResource<PlayerMapping>();

				foreach (var signal in S.GetAllSignals<PlayerConnectedSignal>()) {
					mapping.EntityByChannel.Add(signal.Channel, CreateShip());
				}

				foreach (var signal in S.GetAllSignals<PlayerDisconnectedSignal>()) {
					mapping.EntityByChannel[signal.Channel].Unpack<TWorld>().Set<DestroySelf>();
					mapping.EntityByChannel.Remove(signal.Channel);
				}
			}

			private W.Entity CreateShip() {
				var entity = W.NewEntity<Default>();

				var pos = FVector2.Zero;

				var radius = FP.Two;

				entity.Set(new PhysicalBody {
					WorldCoM = pos,
					Rotation = FAngle.HalfPI
				});

				entity.Set(new Velocity {
					Linear = FVector2.Zero,
					Angular = FAngle.Zero
				});

				entity.Set(new Collider {
					Radius = radius,
					WorldPosition = pos
				});

				entity.Set(Bounds.New(pos, FVector2.One * radius));

				entity.Set(new ViewAsset((short)ViewAssetTypes.Ship));

				return entity;
			}
		}
	}
}
