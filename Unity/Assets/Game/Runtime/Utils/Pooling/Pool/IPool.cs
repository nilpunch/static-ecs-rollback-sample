namespace Game.Utils {
	public interface IPool<TItem> : IPoolReturn<TItem> {
		TItem Get();
	}
}
