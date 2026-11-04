using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Localization;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using UniRx;
using VContainer.Unity;

namespace Game.Flow
{
    /// <summary>
    /// Offers a retry when a state of the app flow fails to enter (an Addressables or scene load error), so the
    /// player is not left on a blank screen.
    /// </summary>
    public sealed class FlowFailureRecovery : IStartable, IDisposable
    {
        private readonly GameStateMachine _states;
        private readonly IDialogService _dialogs;
        private readonly ILocalizationService _texts;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        /// <summary>
        /// Creates the recovery.
        /// </summary>
        /// <param name="states">Reports failed transitions.</param>
        /// <param name="dialogs">Shows the retry card.</param>
        /// <param name="texts">Card texts.</param>
        public FlowFailureRecovery(GameStateMachine states, IDialogService dialogs, ILocalizationService texts)
        {
            _states = states;
            _dialogs = dialogs;
            _texts = texts;
        }

        /// <inheritdoc/>
        public void Start()
        {
            _states.Failed.Subscribe(retry => OfferRetryAsync(retry).Forget()).AddTo(_subscriptions);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
            _subscriptions.Dispose();
        }

        private async UniTaskVoid OfferRetryAsync(Func<UniTask> retry)
        {
            var dialog = new DialogViewModel(_texts.Get(FlowTexts.ErrorTitle), RibbonStyle.Berry)
            {
                Text = _texts.Get(FlowTexts.ErrorText),
            };
            dialog.Buttons.Add(new DialogButton(_texts.Get(FlowTexts.ErrorRetry), ButtonStyle.Green,
                () => RetryAsync(retry).Forget()));
            await _dialogs.ShowAsync(dialog, _lifetime.Token).SuppressCancellationThrow();
        }

        // A repeated failure raises Failed again, which offers the next retry.
        private static async UniTaskVoid RetryAsync(Func<UniTask> retry)
        {
            try
            {
                await retry();
            }
            catch (Exception)
            {
                // Already logged and reported by the state machine.
            }
        }
    }
}
