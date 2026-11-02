using System;
using DG.Tweening;
using Game.Services.Audio;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UnityEngine;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Shelf motion in the cabinet: new shelves drop in from above (column by column at a level start), shelves slide
    /// down when the shelf below them leaves. Records when each shelf settles, so items wait for it.
    /// </summary>
    /// <remarks>
    /// <c>style.bottom</c> is set to the resting place right away (one layout pass); the motion itself is a
    /// <c>translate</c> tween toward zero, so a moving shelf with all its items doesn't relayout every frame.
    /// </remarks>
    public sealed class ShelfAnimator
    {
        private const string SlotClass = "shelf-slot";
        private const float ShelfGap = 3f;
        private const float DropSeconds = 0.52f;
        private const float StartDropDelay = 0.15f;
        private const float LaterDropDelay = 0.42f;
        private const float ColumnStagger = 0.12f;
        private const float FreshStagger = 0.1f;
        private const float DropStartAbove = 20f;
        private const float FreshStartStep = 0.5f;
        private const float DropOvershoot = 1.3f;
        private const float SlideSeconds = 0.4f;
        private const float SlideDelay = 0.3f;
        private const float SlideSettle = 0.75f;
        private const float SlideOvershoot = 1.1f;
        private const float FoldSeconds = 0.3f;
        private const int FoldDust = 16;

        private readonly IAudioService _audio;
        private readonly IOverlayService _overlay;
        private readonly object _tweenId;

        /// <summary>
        /// Creates the animator.
        /// </summary>
        /// <param name="audio">Plays the roll of shelves that drop in after the level start and the fold.</param>
        /// <param name="overlay">Dust when a shelf folds away.</param>
        /// <param name="tweenId">Id of every shelf tween; killed by the board on rebuild.</param>
        public ShelfAnimator(IAudioService audio, IOverlayService overlay, object tweenId)
        {
            _audio = audio;
            _overlay = overlay;
            _tweenId = tweenId;
        }

        /// <summary>
        /// Styles a pooled shelf as a cabinet slot of <paramref name="rows"/> rows.
        /// </summary>
        /// <param name="view">A shelf taken from the pool.</param>
        /// <param name="rows">Rows of the block.</param>
        /// <param name="layout">The board layout.</param>
        public void Prepare(ShelfView view, int rows, BoardLayout layout)
        {
            view.Root.AddToClassList(SlotClass);
            view.Root.style.height = rows * layout.RowHeight - ShelfGap;
        }

        /// <summary>
        /// Puts a new shelf at <paramref name="bottom"/>, dropping it in unless <paramref name="isInstant"/>.
        /// </summary>
        /// <param name="view">The new shelf.</param>
        /// <param name="bottom">Resting position from the column bottom.</param>
        /// <param name="layout">The board layout.</param>
        /// <param name="column">Column index; staggers the opening drop.</param>
        /// <param name="fresh">How many shelves already dropped into this column in this change.</param>
        /// <param name="isStart">The opening drop of a level.</param>
        /// <param name="now">Unscaled time of the change.</param>
        /// <param name="isInstant">Snap (undo rebuild, resize).</param>
        public void Enter(ShelfView view, float bottom, BoardLayout layout, int column, int fresh, bool isStart,
            float now, bool isInstant)
        {
            if (isInstant)
            {
                view.Root.style.bottom = bottom;
                view.SettledAt = now;
                return;
            }

            var delay = (isStart ? StartDropDelay + column * ColumnStagger : LaterDropDelay) + fresh * FreshStagger;
            var from = layout.ColumnsHeight + DropStartAbove + fresh * layout.RowHeight * FreshStartStep;
            var drop = Slide(view, from, bottom, DropSeconds).SetDelay(delay).SetEase(Ease.OutBack, DropOvershoot)
                .SetId(_tweenId);
            // The level session plays one roll for the whole opening drop; later shelves each get their own.
            if (!isStart)
            {
                drop.OnStart(() => _audio.Play(SoundId.Roll));
            }

            view.SettledAt = now + delay + DropSeconds;
        }

        /// <summary>
        /// Slides an existing shelf to <paramref name="bottom"/> if it isn't there yet.
        /// </summary>
        /// <param name="view">The shelf.</param>
        /// <param name="bottom">New resting position from the column bottom.</param>
        /// <param name="now">Unscaled time of the change.</param>
        /// <param name="isInstant">Snap (undo rebuild, resize).</param>
        public void MoveTo(ShelfView view, float bottom, float now, bool isInstant)
        {
            if (Mathf.Approximately(view.Root.style.bottom.value.value, bottom))
            {
                return;
            }

            var from = VisibleBottom(view);
            view.Root.Kill();
            if (isInstant)
            {
                view.Root.style.bottom = bottom;
                VisualElementTweens.SetTranslate(view.Root, Vector2.zero);
                return;
            }

            Slide(view, from, bottom, SlideSeconds).SetDelay(SlideDelay).SetEase(Ease.OutBack, SlideOvershoot)
                .SetId(_tweenId);
            view.SettledAt = now + SlideSettle;
        }

        /// <summary>
        /// Folds an emptied shelf away with dust and the fold sound.
        /// </summary>
        /// <param name="view">The leaving shelf.</param>
        /// <param name="done">Called when the fold ended; the caller returns the shelf to its pool.</param>
        public void FoldAway(ShelfView view, Action done)
        {
            _overlay.Burst(view.Root.worldBound.center, EffectKind.Dust, FoldDust);
            _audio.Play(SoundId.Fold);
            view.Root.Kill();
            view.Root.DOScale(new Vector2(1f, 0f), FoldSeconds).SetEase(Ease.InBack).SetId(_tweenId)
                .OnComplete(() => done());
        }

        private static float VisibleBottom(ShelfView view) =>
            view.Root.style.bottom.value.value - VisualElementTweens.GetTranslate(view.Root).y;

        private static Tweener Slide(ShelfView view, float from, float to, float seconds)
        {
            view.Root.style.bottom = to;
            // A positive translate moves down, so a shelf drawn higher than its resting place starts at to - from.
            VisualElementTweens.SetTranslate(view.Root, new Vector2(0f, to - from));
            return view.Root.DOTranslate(Vector2.zero, seconds);
        }
    }
}
