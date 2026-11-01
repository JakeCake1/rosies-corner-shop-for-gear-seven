using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Experiments;
using Game.Services.Flow;
using Game.Services.Levels;
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

        /// <summary>
        /// Creates the screen.
        /// </summary>
        /// <param name="dialogs">Dialog cards.</param>
        /// <param name="profile">Reset progress.</param>
        /// <param name="levels">Levels marked cleared when jumping ahead.</param>
        /// <param name="experiments">Rewarded offer variant.</param>
        /// <param name="curves">Level curves.</param>
        /// <param name="flow">Starts the chosen level or returns home.</param>
        public ReviewersScreen(IDialogService dialogs, IProfileService profile, ILevelProgressService levels,
            IExperimentsService experiments, ILevelCurveService curves, IGameFlow flow)
        {
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
            var dialog = new DialogViewModel("For reviewers", RibbonStyle.Green)
            {
                Text = "Jump to any level, or switch the variants that would be A/B tested.",
                Note = "Hard levels are red. Ads and purchases are placeholders. Vibration works on Android only.",
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
                labels[i] = _curves.Curves[i].Label;
            }

            dialog.Segments.Add(new DialogSegments("Level curve", labels, curveIndex, index =>
            {
                _curves.SwitchCurve(_curves.Curves[index].Id);
                reopen = true;
                dialog.Close();
            }));
            dialog.Segments.Add(new DialogSegments("Rewarded offer on a loss", new[] { "+1 customer", "+1 counter spot" },
                _experiments.LossOffer == "slot" ? 1 : 0,
                index => _experiments.SetLossOffer(index == 1 ? "slot" : "basket")));
            dialog.Buttons.Add(new DialogButton("Reset progress", ButtonStyle.WoodSmall, () =>
            {
                _profile.ResetProgress();
                _flow.GoHomeAsync().Forget();
            }));
            dialog.Buttons.Add(new DialogButton("Close", ButtonStyle.Berry, null));
            await _dialogs.ShowAsync(dialog, ct);
            if (!reopen) return;

            await _flow.GoHomeAsync();
            await OpenAsync(ct);
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
