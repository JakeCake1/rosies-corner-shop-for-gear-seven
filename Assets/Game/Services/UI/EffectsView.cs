using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Game.Services.UI
{
    /// <summary>
    /// Full-screen overlay for short-lived juice: spark/dust/confetti bursts and floating praise words. Particles
    /// are pooled UI images (the prototype drew them on a canvas).
    /// </summary>
    public sealed class EffectsView : MonoBehaviour
    {
        private const int PoolCapacity = 64;
        private const int PoolMax = 256;

        private static readonly Color[] SparkColors = { new Color(1f, 0.85f, 0.4f), Color.white, new Color(1f, 0.7f, 0.5f) };
        private static readonly Color[] DustColors = { new Color(0.85f, 0.72f, 0.55f), new Color(0.75f, 0.6f, 0.42f) };
        private static readonly Color[] ConfettiColors =
        {
            new Color(0.78f, 0.24f, 0.31f), new Color(0.93f, 0.71f, 0.25f), new Color(0.37f, 0.6f, 0.27f),
            new Color(0.24f, 0.44f, 0.71f), new Color(0.88f, 0.44f, 0.54f),
        };

        [SerializeField] private RectTransform _area;
        [SerializeField] private Image _particlePrefab;
        [SerializeField] private TMP_Text _praisePrefab;

        private ObjectPool<Image> _particles;

        /// <summary>
        /// Emits a burst of particles at a world position (a UI element's position).
        /// </summary>
        /// <param name="worldPosition">Center of the burst in world space.</param>
        /// <param name="kind">Look and motion of the particles.</param>
        /// <param name="count">Number of particles; combos raise it.</param>
        /// <param name="colors">Optional palette override (blue sparks for thawing ice).</param>
        public void Burst(Vector3 worldPosition, EffectKind kind, int count, Color[] colors = null)
        {
            var local = (Vector2)_area.InverseTransformPoint(worldPosition);
            var palette = colors ?? (kind == EffectKind.Dust ? DustColors : kind == EffectKind.Confetti ? ConfettiColors : SparkColors);
            for (var i = 0; i < count; i++)
            {
                var p = _particles.Get();
                var rt = p.rectTransform;
                rt.anchoredPosition = local;
                var size = kind == EffectKind.Confetti ? Random.Range(7f, 12f) : Random.Range(4f, 8f);
                rt.sizeDelta = kind == EffectKind.Confetti ? new Vector2(size, size * 0.6f) : new Vector2(size, size);
                rt.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
                p.color = palette[Random.Range(0, palette.Length)];
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var speed = kind == EffectKind.Confetti ? Random.Range(140f, 320f) : Random.Range(40f, 120f);
                var target = local + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
                if (kind == EffectKind.Confetti) target.y -= Random.Range(80f, 240f);
                var duration = kind == EffectKind.Confetti ? Random.Range(0.9f, 1.4f) : Random.Range(0.4f, 0.7f);
                DOTween.Sequence().SetLink(p.gameObject)
                    .Append(rt.DOAnchorPos(target, duration).SetEase(Ease.OutCubic))
                    .Join(rt.DORotate(new Vector3(0, 0, Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360))
                    .Join(p.DOFade(0f, duration).SetEase(Ease.InQuad))
                    .OnComplete(() => _particles.Release(p));
            }
        }

        /// <summary>
        /// Floats a praise word ("Lovely!", "Tidy shop!") up from a world position.
        /// </summary>
        /// <param name="worldPosition">Where the word starts.</param>
        /// <param name="text">The word.</param>
        /// <param name="isCombo">Combo words are bigger and gold.</param>
        public void Praise(Vector3 worldPosition, string text, bool isCombo)
        {
            var label = Instantiate(_praisePrefab, _area);
            var rt = label.rectTransform;
            rt.anchoredPosition = _area.InverseTransformPoint(worldPosition);
            label.SetText(text);
            label.fontSize = isCombo ? 30f : 24f;
            label.color = isCombo ? new Color(0.93f, 0.71f, 0.25f) : Color.white;
            rt.localScale = Vector3.one * 0.6f;
            DOTween.Sequence().SetLink(label.gameObject)
                .Append(rt.DOScale(1f, 0.25f).SetEase(Ease.OutBack))
                .Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 60f, 1.1f).SetEase(Ease.OutCubic))
                .Insert(0.7f, label.DOFade(0f, 0.4f))
                .OnComplete(() => Destroy(label.gameObject));
        }

        private void Awake()
        {
            _particles = new ObjectPool<Image>(
                () =>
                {
                    var img = Instantiate(_particlePrefab, _area);
                    img.raycastTarget = false;
                    return img;
                },
                img =>
                {
                    img.gameObject.SetActive(true);
                    img.transform.SetAsLastSibling();
                },
                img =>
                {
                    img.DOKill();
                    img.rectTransform.DOKill();
                    img.gameObject.SetActive(false);
                },
                img =>
                {
                    if (img != null) Destroy(img.gameObject);
                },
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                true,
#else
                false,
#endif
                PoolCapacity, PoolMax);
            _particlePrefab.gameObject.SetActive(false);
            _praisePrefab.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _particles?.Clear();
        }
    }
}
