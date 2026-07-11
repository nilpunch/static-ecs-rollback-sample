namespace Game.Utils {
	public interface IPoolReturn<in TItem> {
		void Return(TItem item);
	}
}
