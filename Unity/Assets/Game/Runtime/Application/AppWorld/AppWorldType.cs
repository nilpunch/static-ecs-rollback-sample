using FFS.Libraries.StaticEcs;

namespace Game.Application {
	public struct AppWorldType : IWorldType { }

	public abstract class App : World<AppWorldType> {
		/// <summary>
		/// Convenience overload, shorthand for App.GetResource().
		/// </summary>
		public static T Get<T>() where T : IResource {
			return GetResource<T>();
		}

		/// <summary>
		/// Convenience overload, shorthand for App.SetResource().
		/// </summary>
		public static void Set<T>(T value) where T : IResource {
			SetResource(value);
		}
	}
}
