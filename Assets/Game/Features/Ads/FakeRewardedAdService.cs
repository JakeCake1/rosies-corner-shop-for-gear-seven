using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Ads;
using Game.Services.UI.Dialogs;

namespace Game.Features.Ads
{
    /// <summary>
    /// Placeholder rewarded ad (as in the prototype): a card says a video would play, waits 2.3 s and grants the
    /// reward. Replace with an ad SDK adapter behind <see cref="IRewardedAdService"/>.
    /// </summary>
    public sealed class FakeRewardedAdService : IRewardedAdService
    {
        private const int AdMilliseconds = 2300;

        private readonly IDialogService _dialogs;

        /// <summary>
        /// Creates the service.
        /// </summary>
        /// <param name="dialogs">Shows the placeholder card.</param>
        public FakeRewardedAdService(IDialogService dialogs)
        {
            _dialogs = dialogs;
        }

        /// <inheritdoc/>
        public async UniTask<bool> ShowAsync(CancellationToken ct)
        {
            var card = new DialogViewModel("Rewarded ad", RibbonStyle.Green)
            {
                Text = "A rewarded video plays here in the real game.",
                Progress = 1f,
                ProgressCaption = "Playing…",
            };
            var shown = _dialogs.ShowAsync(card, ct);
            await UniTask.Delay(AdMilliseconds, true, cancellationToken: ct);
            card.Close();
            await shown;
            return true;
        }
    }
}
