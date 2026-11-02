using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.UI.Dialogs
{
    /// <summary>
    /// Opens dialog cards; a thin helper over <see cref="IUIService"/> so features don't repeat the generic call.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Opens a dialog and returns when it has closed.
        /// </summary>
        /// <param name="dialog">Content; the UI service disposes it on close.</param>
        /// <param name="ct">The opener's lifetime; cancelling it closes the dialog.</param>
        /// <returns>Completes when the dialog is closed.</returns>
        /// <exception cref="System.OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
        UniTask ShowAsync(DialogViewModel dialog, CancellationToken ct);

        /// <summary>
        /// Closes every open dialog (level restart, leaving to the shop).
        /// </summary>
        /// <returns>Completes when all are closed.</returns>
        UniTask CloseAllAsync();
    }
}
