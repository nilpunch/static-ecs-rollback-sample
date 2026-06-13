using FFS.Libraries.StaticEcs;

namespace Game.Utils {
	public interface IResourceConfig<TWorld> : IResource {
		void SetResource();
	}

	public abstract class ResourceConfig<TWorld, T> : ScriptableConfig<T>, IResourceConfig<TWorld>
		where TWorld : struct, IWorldType
		where T : ResourceConfig<TWorld, T> {
		public void SetResource() => World<TWorld>.SetResource((T)this);
	}

	public static class ResourceConfigUtils {
		public static void SetResourceConfigs<TWorld>() where TWorld : struct, IWorldType {
			foreach (var config in ScriptableConfig.GetAll()) {
				if (config is IResourceConfig<TWorld> resourceConfig) {
					resourceConfig.SetResource();
				}
			}
		}
	}
}
