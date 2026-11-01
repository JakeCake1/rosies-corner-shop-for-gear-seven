using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Flow
{
    /// <summary>
    /// Asks the player to name the shop ("You're my partner now!"); used after level 10 and from settings.
    /// </summary>
    public interface IShopNamePrompt
    {
        /// <summary>
        /// Shows the naming dialog and stores the chosen name.
        /// </summary>
        /// <param name="ct">Cancels waiting.</param>
        /// <returns>Completes when the dialog closes.</returns>
        /// <exception cref="System.OperationCanceledException">Waiting was cancelled.</exception>
        UniTask AskAsync(CancellationToken ct);
    }
}
