using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Default <see cref="IDialogService"/>.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        private readonly IUIService _ui;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="ui">Window management.</param>
        public DialogService(IUIService ui)
        {
            _ui = ui;
        }

        /// <inheritdoc/>
        public async UniTask ShowAsync(DialogViewModel dialog, CancellationToken ct)
        {
            var closed = new UniTaskCompletionSource();
            var window = await _ui.OpenAsync<DialogWindow, DialogViewModel>(dialog, ct);
            // Closing by button, blocker or back all end with the window being destroyed.
            using (window.GetCancellationTokenOnDestroy().Register(() => closed.TrySetResult()))
            {
                using (ct.Register(() => closed.TrySetCanceled()))
                {
                    await closed.Task;
                }
            }
        }

        /// <inheritdoc/>
        public UniTask CloseAllAsync() => _ui.CloseAllAsync(UILayer.Popups);
    }
}
