using FFS.Libraries.StaticEcs;
using Fixed32;
using Shenanicode.Rollback;

namespace Game.Core {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public static class AsteroidFactory {
			public static EntityGID CreateRectangular(ushort width, ushort height, FP cellSpacing, FP cellRadius, FVector2 position, FVector2 velocity, FAngle rotation = default, FAngle angularVelocity = default) {
				// var entity = W.NewEntity<Default>();
				// entity.Set(
				// 	new Asteroid {
				// 		Width = width,
				// 		Height = height,
				// 		CellSpacing = cellSpacing,
				// 		CellRadius = cellRadius,
				// 	},
				// 	new Position { Value = position },
				// 	new PhysicalBody { },
				// 	new Rotation { Value = rotation },
				// 	new AngularVelocity { Value = angularVelocity }
				// );
				//
				// ref var cells = ref entity.Add<W.Multi<AsteroidCell>>();
				// for (var y = 0; y < height; y++) {
				// 	for (var x = 0; x < width; x++) {
				// 		cells.Add(new AsteroidCell {
				// 			X = (short)x,
				// 			Y = (short)y,
				// 		});
				// 	}
				// }
				//
				// entity.Set<AsteroidDirty>();
				// return entity.GID;
				return default;
			}
		}
	}
}
