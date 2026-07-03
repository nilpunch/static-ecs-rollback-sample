using Game.Client;
using Game.Core;
using Shenanicode.Rollback;

namespace Game.Application
{
	public static class JymSetup {
		public static void CreateAndInitialize() {
			S.Create(SimulationType.ForwardOnly,
				GameSessionSetup.SessionConfig);
			GameSessionSetup.Register();
			S.Initialize();

			GameWorldSetup.CreateAndInitialize();
			GameInterpolationSetup.CreateAndInitialize();
		}

		public static void Destroy() {
			if (S.Status != SessionStatus.NotCreated) {
				GameInterpolationSetup.Destroy();
				GameWorldSetup.Destroy();
				S.Destroy();
			}
		}
	}
}
