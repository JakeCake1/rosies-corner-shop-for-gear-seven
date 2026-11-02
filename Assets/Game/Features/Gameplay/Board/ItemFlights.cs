using System;
using DG.Tweening;
using Game.Services.Audio;
using Game.Services.UI;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Features.Gameplay.Board
{
    /// <summary>
    /// Arc flights on the board's flight layer: items moving between shelf, counter and basket, items leaving the
    /// board, and the key that flies to the shelf it unlocks.
    /// </summary>
    /// <remarks>
    /// Every tween uses the board's tween id, so a board rebuild kills all flights at once. A flight sets the flyer's
    /// layout (position and size) once at its start and then animates only <c>translate</c>, <c>scale</c> and
    /// <c>rotate</c>, so a flying item doesn't trigger a layout pass every frame.
    /// </remarks>
    public sealed class ItemFlights
    {
        private const float ArcHeightFactor = 4f;
        private const float TiltDegrees = 8f;
        private const float FlyAwaySeconds = 0.48f;
        private const float FlyAwayScale = 0.35f;
        private const string KeyClass = "key-flyer";
        private const float KeyLift = 80f;
        private const float KeySeconds = 0.55f;
        private const int KeySparks = 16;

        private static readonly Vector2 KeySize = new Vector2(20f, 26f);

        private readonly VisualElement _layer;
        private readonly IAudioService _audio;
        private readonly IOverlayService _overlay;
        private readonly object _tweenId;

        /// <summary>
        /// Creates the flights.
        /// </summary>
        /// <param name="layer">The flight layer that flying elements are moved into.</param>
        /// <param name="audio">Plays the landing sounds and the key snip.</param>
        /// <param name="overlay">Plays the key's sparks.</param>
        /// <param name="tweenId">Id of every flight tween; killed by the board on rebuild.</param>
        public ItemFlights(VisualElement layer, IAudioService audio, IOverlayService overlay, object tweenId)
        {
            _layer = layer;
            _audio = audio;
            _overlay = overlay;
            _tweenId = tweenId;
        }

        /// <summary>
        /// Moves an item onto the flight layer at a panel position, so it can fly over every other board part.
        /// </summary>
        /// <param name="view">The item.</param>
        /// <param name="panelCenter">Where its center is now, in panel coordinates.</param>
        /// <param name="size">Its layout size at the flight start; the scale of an item caught mid-flight is applied
        /// to it, so the item keeps its visible size.</param>
        public void Detach(ItemView view, Vector2 panelCenter, Vector2 size)
        {
            var root = view.Root;
            var visibleSize = size * InlineScale(root);
            root.Kill();
            _layer.Add(root);
            root.PlaceCentered(_layer.WorldToLocal(panelCenter), visibleSize);
            root.style.right = StyleKeyword.Null;
            root.style.bottom = StyleKeyword.Null;
            // Explicit values, not Null: tween getters read inline values, resolved ones lag a frame behind.
            VisualElementTweens.SetTranslate(root, Vector2.zero);
            VisualElementTweens.SetScale(root, Vector2.one);
        }

        /// <summary>
        /// Flies a detached item in an arc into <paramref name="cell"/> and snaps it there. The target is re-read every
        /// frame, so the item follows a cell that is still moving (a dropping shelf, an arriving basket).
        /// </summary>
        /// <param name="view">The detached item.</param>
        /// <param name="cell">Destination cell; a newer flight of the same item cancels this one's landing.</param>
        /// <param name="delay">Seconds to wait, e.g. until the destination has settled.</param>
        /// <param name="lift">Arc height in pixels; 0 for a straight hop.</param>
        /// <param name="seconds">Flight time.</param>
        /// <param name="landing">Sound played on landing, or null.</param>
        public void Fly(ItemView view, VisualElement cell, float delay, float lift, float seconds, SoundId? landing)
        {
            view.Destination = cell;
            var root = view.Root;
            var startSize = new Vector2(root.style.width.value.value, root.style.height.value.value);
            var start = VisualElementTweens.GetPosition(root) + startSize / 2f;
            DOTween.To(() => 0f, t => StepFlight(view, cell, start, startSize, lift, t), 1f, seconds)
                .SetEase(Ease.InOutQuad)
                .SetDelay(delay)
                .SetTarget(root)
                .SetId(_tweenId)
                .OnComplete(() => Land(view, cell, landing));
        }

        /// <summary>
        /// Flies an item off the board toward a panel position while it shrinks.
        /// </summary>
        /// <param name="view">The leaving item.</param>
        /// <param name="panelTarget">Where it flies, in panel coordinates.</param>
        /// <param name="done">Called when the flight ended; the caller returns the item to its pool.</param>
        public void FlyAway(ItemView view, Vector2 panelTarget, Action done)
        {
            Detach(view, view.Root.worldBound.center, view.Root.layout.size);
            var size = new Vector2(view.Root.style.width.value.value, view.Root.style.height.value.value);
            var start = VisualElementTweens.GetPosition(view.Root) + size / 2f;
            var offset = _layer.WorldToLocal(panelTarget) - start;
            DOTween.Sequence().SetId(_tweenId)
                .Append(view.Root.DOTranslate(offset, FlyAwaySeconds).SetEase(Ease.InQuad))
                .Join(view.Root.DOScale(FlyAwayScale, FlyAwaySeconds))
                .OnComplete(() => done());
        }

        /// <summary>
        /// Flies a key from <paramref name="start"/> to the shelf it unlocks, then plays the snip and sparks.
        /// </summary>
        /// <param name="start">Start in panel coordinates (the key tag of the shelf that held it).</param>
        /// <param name="target">The unlocked shelf; followed while it moves.</param>
        public void FlyKey(Vector2 start, VisualElement target)
        {
            var key = new VisualElement { pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
            key.AddToClassList(KeyClass);
            _layer.Add(key);
            var from = _layer.WorldToLocal(start);
            key.PlaceCentered(from, KeySize);
            DOTween.To(() => 0f, t =>
                {
                    var to = _layer.WorldToLocal(target.worldBound.center);
                    VisualElementTweens.SetTranslate(key, Arc(from, to, KeyLift, t) - from);
                }, 1f, KeySeconds)
                .SetId(_tweenId)
                .OnComplete(() =>
                {
                    key.RemoveFromHierarchy();
                    _audio.Play(SoundId.Snip);
                    _overlay.Burst(target.worldBound.center, EffectKind.Spark, KeySparks);
                });
        }

        private static Vector2 Arc(Vector2 from, Vector2 to, float lift, float t) =>
            Vector2.Lerp(from, to, t) + Vector2.down * (lift * ArcHeightFactor * t * (1f - t));

        private static Vector2 InlineScale(VisualElement element) =>
            element.style.scale.keyword == StyleKeyword.Undefined
                ? VisualElementTweens.GetScale(element)
                : Vector2.one;

        private void StepFlight(ItemView view, VisualElement cell, Vector2 start, Vector2 startSize, float lift,
            float t)
        {
            var root = view.Root;
            var end = _layer.WorldToLocal(cell.worldBound.center);
            var endSize = cell.layout.size;
            if (float.IsNaN(endSize.x))
            {
                endSize = startSize;
            }

            var scale = view.FitScale(startSize, Vector2.Lerp(startSize, endSize, t));
            VisualElementTweens.SetTranslate(root, Arc(start, end, lift, t) - start);
            VisualElementTweens.SetScale(root, new Vector2(scale, scale));
            VisualElementTweens.SetRotation(root, lift > 0f ? -TiltDegrees * Mathf.Sin(t * Mathf.PI) : 0f);
        }

        private void Land(ItemView view, VisualElement cell, SoundId? landing)
        {
            if (view.Destination != cell)
            {
                return;
            }

            view.Snap(cell);
            if (landing.HasValue)
            {
                _audio.Play(landing.Value);
            }
        }
    }
}
