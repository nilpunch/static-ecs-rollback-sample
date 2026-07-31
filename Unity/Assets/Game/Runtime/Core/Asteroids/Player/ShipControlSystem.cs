using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> {
		public class ShipControlSystem : ISystem {
			private readonly FP _maxThrust;
			private readonly FAngle _maxTurn;

			public ShipControlSystem(FP maxThrust, FAngle maxTurn) {
				_maxThrust = maxThrust;
				_maxTurn = maxTurn;
			}

			public void Update() {
				ref var shipInputs = ref S.GetAllInputs<ShipInput>();

				foreach (var entity in W.Query<All<Ship, Player>>().Entities()) {
					ref Ship ship = ref entity.Ref<Ship>();
					ref readonly Player player = ref entity.Read<Player>();

					ship.Thrust = FP.Zero;
					ship.Turn = FAngle.Zero;

					var input = shipInputs.Get(player.Chanel).LastFresh();
					if (input.Up) {
						ship.Thrust = _maxThrust;
					}
					if (input.Down) {
						ship.Thrust = -_maxThrust;
					}
					if (input.Right) {
						ship.Turn = -_maxTurn;
					}
					if (input.Left) {
						ship.Turn = _maxTurn;
					}
				}
			}
		}
	}
}
