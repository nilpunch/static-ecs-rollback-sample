using System.Threading;
using Cysharp.Threading.Tasks;
using FFS.Libraries.StaticEcs;

namespace Game.Client {
	public interface IStateExit : IResource {
		UniTask Exit(CancellationToken token);
	}

	public interface IState : IStateExit {
		UniTask Enter(CancellationToken token);
	}

	public interface IState<in TArgs> : IStateExit {
		UniTask Enter(TArgs args, CancellationToken token);
	}
}
