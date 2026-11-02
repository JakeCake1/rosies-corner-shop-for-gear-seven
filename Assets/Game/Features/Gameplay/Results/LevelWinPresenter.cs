using System;
using Cysharp.Threading.Tasks;
using Game.Features.Gameplay.Offers;
using Game.Features.Gameplay.Session;
using Game.Services.Audio;
using Game.Services.UI;
using Game.Services.UI.Dialogs;
using UnityEngine;

namespace Game.Features.Gameplay.Results
{
    /// <summary>
    /// The win sequence: confetti, then the "Level complete" dialog with stars, stats, double-coins ad, replay and
    /// next level.
    /// </summary>
    public sealed class LevelWinPresenter
    {
        private const int CompleteDelayMs = 900;
        private const int ConfettiStaggerMs = 220;
        private const int ConfettiCount = 60;
        private const float ConfettiOffset = 80f;

        private readonly GameplayContext _context;
        private readonly PurchaseOffers _offers;
        private readonly Action _restart;

        /// <summary>
        /// Creates the presenter.
        /// </summary>
        /// <param name="context">Shared gameplay state.</param>
        /// <param name="offers">Adds the double-coins ad button.</param>
        /// <param name="restart">Restarts the level in play (Replay).</param>
        public LevelWinPresenter(GameplayContext context, PurchaseOffers offers, Action restart)
        {
            _context = context;
            _offers = offers;
            _restart = restart;
        }

        /// <summary>
        /// Plays the confetti and shows the dialog, unless the player undid the winning move meanwhile.
        /// </summary>
        /// <param name="outcome">The won level's stars and reward.</param>
        /// <returns>Completes when the dialog closes; cancelled with the level.</returns>
        public async UniTask ShowAsync(LevelOutcome outcome)
        {
            var ct = _context.LevelToken;
            var board = _context.Board;
            var overlay = _context.Services.Overlay;
            overlay.Burst(board.Counter.worldBound.center + Vector2.left * ConfettiOffset, EffectKind.Confetti,
                ConfettiCount);
            await UniTask.Delay(ConfettiStaggerMs, cancellationToken: ct);
            overlay.Burst(board.BasketArea.worldBound.center + Vector2.right * ConfettiOffset, EffectKind.Confetti,
                ConfettiCount);
            await UniTask.Delay(CompleteDelayMs - ConfettiStaggerMs, cancellationToken: ct);
            // Undo still works on a won board and reopens the level without restarting it.
            if (!_context.Session.IsOver)
            {
                return;
            }

            await _context.Services.Dialogs.ShowAsync(CreateDialog(outcome), ct);
        }

        private DialogViewModel CreateDialog(LevelOutcome outcome)
        {
            var texts = _context.Services.Texts;
            var level = _context.Level;
            var title = _context.Session.IsTricky
                ? texts.Get(GameplayTexts.CompleteTrickyTitle)
                : texts.Format(GameplayTexts.CompleteTitle, level);
            var dialog = new DialogViewModel(title, RibbonStyle.Green)
            {
                Stars = outcome.Stars,
                Headline = texts.Get(HeadlineOf(outcome.Stars)),
                Text = outcome.Stars == 3
                    ? texts.Get(GameplayTexts.CompleteNeat)
                    : texts.Format(GameplayTexts.CompleteHint, GameplayBalance.StarThreshold(level)),
                Reward = "+" + outcome.Reward,
            };
            AddStats(dialog);
            _offers.AddAdButton(dialog, GameplayTexts.DoubleCoins, () => GrantDoubleCoins(outcome.Reward));
            AddReplayAndNext(dialog);
            return dialog;
        }

        private static string HeadlineOf(int stars)
        {
            if (stars == 3)
            {
                return GameplayTexts.HeadlineThreeStars;
            }

            return stars == 2 ? GameplayTexts.HeadlineTwoStars : GameplayTexts.HeadlineOneStar;
        }

        private void AddStats(DialogViewModel dialog)
        {
            var texts = _context.Services.Texts;
            var st = _context.Session.State;
            dialog.Stats.Add((texts.Get(GameplayTexts.StatItems), st.Total.ToString()));
            dialog.Stats.Add((texts.Get(GameplayTexts.StatPeak), $"{st.Peak} / {st.Capacity}"));
            dialog.Stats.Add((texts.Get(GameplayTexts.StatMoves), st.Moves.ToString()));
        }

        private void GrantDoubleCoins(int reward)
        {
            var services = _context.Services;
            services.Wallet.Add(reward);
            services.Audio.Play(SoundId.Coin);
            services.Overlay.Toast(services.Texts.Format(GameplayTexts.CoinsAdded, reward));
            AfterWin();
        }

        private void AddReplayAndNext(DialogViewModel dialog)
        {
            var texts = _context.Services.Texts;
            var level = _context.Level;
            var curves = _context.Services.Curves;
            dialog.Buttons.Add(new DialogButton(texts.Get(GameplayTexts.Replay), ButtonStyle.Wood, _restart)
            {
                IsInRow = true,
            });
            var isDirect = level < GameplayBalance.DirectNextLevelBelow;
            var nextLabel = texts.Get(isDirect ? GameplayTexts.NextLevel : GameplayTexts.Continue);
            var next = new DialogButton(nextLabel, ButtonStyle.Berry, AfterWin) { IsInRow = true };
            if (isDirect && level < curves.Count && curves.Get(level + 1).IsHard)
            {
                next.SubLabel = texts.Get(GameplayTexts.NextTricky);
            }

            dialog.Buttons.Add(next);
        }

        private void AfterWin()
        {
            // Prototype quirk kept on purpose: from level 9 on "Continue" returns to the shop, where the album and
            // shop-naming milestones are shown and "Open the shop" starts the next level.
            var level = _context.Level;
            if (level < GameplayBalance.DirectNextLevelBelow && level < _context.Services.Curves.Count)
            {
                _context.Services.Flow.PlayLevelAsync(level + 1).Forget();
            }
            else
            {
                _context.Services.Flow.GoHomeAsync().Forget();
            }
        }
    }
}
