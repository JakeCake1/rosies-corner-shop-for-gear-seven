using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Localization;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Default <see cref="IDialogService"/>.
    /// </summary>
    public sealed class DialogService : IDialogService
    {
        private readonly IUIService _ui;
        private readonly ILocalizationService _texts;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="ui">Window management.</param>
        /// <param name="texts">Texts the card adds by itself (the "AD" badge).</param>
        public DialogService(IUIService ui, ILocalizationService texts)
        {
            _ui = ui;
            _texts = texts;
        }

        /// <inheritdoc/>
        public async UniTask ShowAsync(DialogViewModel dialog, CancellationToken ct)
        {
            dialog.AdBadgeText = _texts.Get(DialogTexts.AdBadge);
            var window = await _ui.OpenAsync<DialogWindow, DialogViewModel>(dialog, ct);
            // Closing by button, blocker or back all end with the window being released.
            await window.Closed.AttachExternalCancellation(ct);
            if (dialog.IsReplaced)
            {
                // Another dialog took its place: nobody answered, so the opener must not read it as "declined".
                throw new System.OperationCanceledException();
            }
        }

        /// <inheritdoc/>
        public UniTask CloseAllAsync() => _ui.CloseAllAsync(UILayer.Popups);
    }
}
