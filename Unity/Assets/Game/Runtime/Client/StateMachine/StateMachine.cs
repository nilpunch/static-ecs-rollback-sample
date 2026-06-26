using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Client {
	public class StateMachine : AppBehaviour<StateMachine> {
		private IStateExit _activeState;
		private CancellationTokenSource _transitionCts;

		protected override void OnDestroy() {
			CancelTransition();
			base.OnDestroy();
		}

		public void Enter<TState>() where TState : class, IState {
			EnterInternal(App.Get<TState>(), static (nextState, token) => nextState.Enter(token)).Forget();
		}

		public void Enter<TState, TArgs>(TArgs args) where TState : class, IState<TArgs> {
			EnterInternal(App.Get<TState>(), (nextState, token) => nextState.Enter(args, token)).Forget();
		}

		private async UniTaskVoid EnterInternal<TNext>(TNext nextState, Func<TNext, CancellationToken, UniTask> enterNextState)
			where TNext : class, IStateExit {
			if (nextState == null) {
				throw new Exception("Next state is null");
			}

			if (_activeState == nextState) {
				return;
			}

			CancelTransition();

			var previousState = _activeState;
			_activeState = nextState;

			_transitionCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
			var token = _transitionCts.Token;

			try {
				if (previousState != null) {
					await previousState.Exit(token);
				}

				if (!token.IsCancellationRequested && _activeState == nextState) {
					await enterNextState(nextState, token);
				}
			}
			catch (OperationCanceledException) when (token.IsCancellationRequested) { }
			finally {
				_transitionCts.Dispose();
				_transitionCts = null;
			}
		}

		private void CancelTransition() {
			if (_transitionCts == null) {
				return;
			}

			_transitionCts.Cancel();
			_transitionCts.Dispose();
			_transitionCts = null;
		}
	}
}
