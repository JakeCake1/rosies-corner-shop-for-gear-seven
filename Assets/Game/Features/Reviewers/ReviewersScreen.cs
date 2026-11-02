using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Experiments;
using Game.Services.Flow;
using Game.Services.Levels;
using Game.Services.Localization;
using Game.Services.Save;
using Game.Services.UI.Dialogs;

namespace Game.Features.Reviewers
{
    /// <summary>
    /// The "For reviewers" panel (prototype <c>openReview</c>): jump to any level, switch the level curve and the
    /// rewarded-offer A/B variant, reset progress.
    /// </summary>
    public sealed class ReviewersScreen : IReviewersScreen
    {
        private readonly IDialogService _dialogs;
        private readonly IProfileService _profile;
        private readonly ILevelProgressService _levels;
        private readonly IExperimentsService _experiments;
        private readonly ILevelCurveService _curves;
        private readonly IGameFlow _flow;
        private readonly ILocalizationService _texts;

        /// <summary>
        /// Creates the screen.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="profile">Reset progress.</param>
        /// <param name="levels">Levels marked cleared when jumping ahead.</param>
        /// <param name="experiments">Rewarded offer variant.</param>
        /// <param name="curves">Level curves.</param>
        /// <param name="flow">Starts the chosen level or returns home.</param>
        /// <param name="texts">Panel texts and curve labels.</param>
        public ReviewersScreen(IDialogService dialogs, IProfileService profile, ILevelProgressService levels,
            IExperimentsService experiments, ILevelCurveService curves, IGameFlow flow, ILocalizationService texts)
        {
            _texts = texts;
            _dialogs = dialogs;
            _profile = profile;
            _levels = levels;
            _experiments = experiments;
            _curves = curves;
            _flow = flow;
        }

        /// <inheritdoc/>
        /// <remarks>Switching the curve reopens the panel on the refreshed home screen.</remarks>
        public async UniTask OpenAsync(CancellationToken ct)
        {
            var reopen = false;
            var dialog = new DialogViewModel(_texts.Get(ReviewersTexts.Title), RibbonStyle.Green)
            {
                Text = _texts.Get(ReviewersTexts.Text),
                Note = _texts.Get(ReviewersTexts.Note),
                IsClosedByBlocker = true,
            };
            for (var i = 1; i <= _curves.Count; i++)
            {
                var level = i;
                dialog.Grid.Add(new DialogButton(level.ToString(), ButtonStyle.Wood, () => JumpTo(level))
                {
                    IsHighlighted = _curves.Get(level).IsHard,
                });
            }

            var curveIndex = 0;
            for (var i = 0; i < _curves.Curves.Count; i++)
            {
                if (_curves.Curves[i] == _curves.Active) curveIndex = i;
            }

            var labels = new string[_curves.Curves.Count];
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i] = _texts.Get(_curves.Curves[i].LabelKey);
            }

            dialog.Segments.Add(new DialogSegments(_texts.Get(ReviewersTexts.Curve), labels, curveIndex, index =>
            {
                _curves.SwitchCurve(_curves.Curves[index].Id);
                reopen = true;
                dialog.Close();
            }));
            var offers = new[] { _texts.Get(ReviewersTexts.OfferCustomer), _texts.Get(ReviewersTexts.OfferSpot) };
            dialog.Segments.Add(new DialogSegments(_texts.Get(ReviewersTexts.Offer), offers,
                _experiments.LossOffer == "slot" ? 1 : 0,
                index => _experiments.SetLossOffer(index == 1 ? "slot" : "basket")));
            dialog.Buttons.Add(new DialogButton(_texts.Get(ReviewersTexts.Reset), ButtonStyle.WoodSmall, () =>
            {
                _profile.ResetProgress();
                _flow.GoHomeAsync().Forget();
            }));
            dialog.Buttons.Add(new DialogButton(_texts.Get(ReviewersTexts.Close), ButtonStyle.Berry, null));
            await _dialogs.ShowAsync(dialog, ct);
            if (!reopen) return;

            await _flow.GoHomeAsync();
            // Going home hid the old home screen and cancelled ct with it; the refreshed screen's popups are closed
            // by the next flow state instead.
            await OpenAsync(CancellationToken.None);
        }

        private void JumpTo(int level)
        {
            // Levels before the chosen one count as cleared so progression and milestones stay consistent.
            for (var k = 1; k < level; k++)
            {
                if (!_levels.IsCompleted(k)) _levels.Complete(k, 1);
            }

            _flow.PlayLevelAsync(level).Forget();
        }
    }
}
