using FFS.Libraries.StaticEcs;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public static class GameWorldSetup {
			public static WorldConfig WorldConfig => new() {
				TrackingBufferSize = 2,
			};

			public static void CreateAndInitialize() {
				Create();
				Register();
				Initialize();
			}

			public static void Create() {
				W.Create(WorldConfig);
				Systems.Create();
			}

			public static void Register() {
				W.Types().RegisterAll(typeof(CoreRoot).Assembly);
				SimulationSetup.Register();
			}

			public static void Initialize() {
				W.Initialize();
				Systems.Initialize();
			}

			public static void Destroy() {
				Systems.Destroy();
				W.Destroy();
			}
		}
	}
}
