using DG.Tweening;
using Game.Services.UI.Elements;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Particle bursts (sparks, dust, confetti) and floating praise words. Particles are pooled elements that are
    /// attached to the overlay only while they fly.
    /// </summary>
    public sealed class EffectsView : ElementView
    {
        private const string ParticleClass = "particle";
        private const string PraiseClass = "praise";
        private const string ComboClass = "praise--combo";
        private const int PoolCapacity = 64;
        private const int PoolMax = 256;

        private static readonly Color[] SparkColors = { new Color(1f, 0.85f, 0.4f), Color.white, new Color(1f, 0.7f, 0.5f) };
        private static readonly Color[] DustColors = { new Color(0.85f, 0.72f, 0.55f), new Color(0.75f, 0.6f, 0.42f) };
        private static readonly Color[] ConfettiColors =
        {
            new Color(0.78f, 0.24f, 0.31f), new Color(0.93f, 0.71f, 0.25f), new Color(0.37f, 0.6f, 0.27f),
            new Color(0.24f, 0.44f, 0.71f), new Color(0.88f, 0.44f, 0.54f),
        };

        private ObjectPool<VisualElement> _particles;

        /// <summary>
        /// Bursts particles from a point.
        /// </summary>
        /// <param name="panelPosition">Origin in panel space (e.g. <c>worldBound.center</c>).</param>
        /// <param name="kind">Look of the particles.</param>
        /// <param name="count">Number of particles.</param>
        /// <param name="colors">Palette override.</param>
        public void Burst(Vector2 panelPosition, EffectKind kind, int count, Color[] colors = null)
        {
            var origin = Root.WorldToLocal(panelPosition);
            var palette = colors ?? (kind == EffectKind.Dust ? DustColors : kind == EffectKind.Confetti ? ConfettiColors : SparkColors);
            var isConfetti = kind == EffectKind.Confetti;
            for (var i = 0; i < count; i++)
            {
                var p = _particles.Get();
                var side = isConfetti ? Random.Range(7f, 12f) : Random.Range(4f, 8f);
                var size = isConfetti ? new Vector2(side, side * 0.6f) : new Vector2(side, side);
                p.PlaceCentered(origin, size);
                p.SetTint(palette[Random.Range(0, palette.Length)]);
                p.style.opacity = 1f;
                VisualElementTweens.SetRotation(p, Random.Range(0f, 360f));
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var speed = isConfetti ? Random.Range(140f, 320f) : Random.Range(40f, 120f);
                var target = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                if (isConfetti)
                {
                    target.y += Random.Range(80f, 240f);
                }
                var duration = isConfetti ? Random.Range(0.9f, 1.4f) : Random.Range(0.4f, 0.7f);
                var particle = p;
                DOTween.Sequence()
                    .Append(p.DOPosition(target - size / 2f, duration).SetEase(Ease.OutCubic))
                    .Join(p.DORotate(VisualElementTweens.GetRotation(p) + Random.Range(-360f, 360f), duration))
                    .Join(p.DOFade(0f, duration).SetEase(Ease.InQuad))
                    .OnComplete(() => _particles.Release(particle));
            }
        }

        /// <summary>
        /// Floats a praise word up from a point.
        /// </summary>
        /// <param name="panelPosition">Origin in panel space.</param>
        /// <param name="text">Word.</param>
        /// <param name="isCombo">Larger golden combo style.</param>
        public void Praise(Vector2 panelPosition, string text, bool isCombo)
        {
            var origin = Root.WorldToLocal(panelPosition);
            var label = CreateLabel(text, PraiseClass);
            label.EnableInClassList(ComboClass, isCombo);
            Root.Add(label);
            var size = new Vector2(260f, 40f);
            label.PlaceCentered(origin, size);
            VisualElementTweens.SetScale(label, Vector2.one * 0.6f);
            var start = VisualElementTweens.GetPosition(label);
            DOTween.Sequence()
                .Append(label.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
                .Join(label.DOPosition(start + Vector2.down * 60f, 1.1f).SetEase(Ease.OutCubic))
                .Insert(0.7f, label.DOFade(0f, 0.4f))
                .OnComplete(label.RemoveFromHierarchy);
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _particles = new ObjectPool<VisualElement>(
                () =>
                {
                    var p = CreateElement(ParticleClass);
                    p.pickingMode = PickingMode.Ignore;
                    return p;
                },
                p => Root.Add(p),
                p =>
                {
                    p.Kill();
                    p.RemoveFromHierarchy();
                },
                p => p.RemoveFromHierarchy(),
                false,
                PoolCapacity,
                PoolMax);
        }
    }
}
