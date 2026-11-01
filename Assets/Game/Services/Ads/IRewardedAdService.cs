using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Ads
{
    /// <summary>
    /// Shows a rewarded video. The prototype (and this port) only has a placeholder that "plays" for 2.3 s.
    /// </summary>
    public interface IRewardedAdService
    {
        /// <summary>
        /// Plays a rewarded ad.
        /// </summary>
        /// <param name="ct">Cancelled when the screen that asked for the ad closes.</param>
        /// <returns>True when the reward should be granted.</returns>
        /// <exception cref="System.OperationCanceledException">The ad was cancelled.</exception>
        UniTask<bool> ShowAsync(CancellationToken ct);
    }
}
