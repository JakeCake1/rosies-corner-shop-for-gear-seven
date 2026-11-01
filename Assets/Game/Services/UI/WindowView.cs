using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// Base of every window: fade in/out of a <see cref="CanvasGroup"/> with a soft "pop" of the card, and a close
    /// request stream the UI service listens to.
    /// </summary>
    /// <typeparam name="TViewModel">The window's view model.</typeparam>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class WindowView<TViewModel> : View<TViewModel>, IWindow where TViewModel : ViewModel
    {
        private const float PopFromScale = 0.86f;

        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Card that pops in; leave empty for full-screen windows that only fade.")]
        [SerializeField] private RectTransform _card;
        [SerializeField] private float _fadeDuration = 0.2f;

        private readonly Subject<IWindow> _closeRequested = new Subject<IWindow>();

        /// <inheritdoc/>
        public abstract UILayer Layer { get; }

        /// <inheritdoc/>
        public virtual bool IsClosedByBlocker => false;

        /// <inheritdoc/>
        public GameObject Root => gameObject;

        /// <inheritdoc/>
        public IObservable<IWindow> CloseRequested => _closeRequested;

        /// <inheritdoc/>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0f;
            var seq = DOTween.Sequence().SetUpdate(true).SetLink(gameObject).SetId(this)
                .Append(_canvasGroup.DOFade(1f, _fadeDuration));
            if (_card != null)
            {
                _card.localScale = Vector3.one * PopFromScale;
                seq.Join(_card.DOScale(1f, _fadeDuration * 1.6f).SetEase(Ease.OutBack));
            }

            await seq.ToUniTask(TweenCancelBehaviour.Kill, ct);
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        /// <inheritdoc/>
        public async UniTask HideAsync(CancellationToken ct)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            await _canvasGroup.DOFade(0f, _fadeDuration * 0.75f)
                .SetUpdate(true).SetLink(gameObject).SetId(this)
                .ToUniTask(TweenCancelBehaviour.Kill, ct);
        }

        /// <inheritdoc/>
        public void Release() => Unbind();

        /// <summary>
        /// Asks the UI service to close this window (close buttons call it).
        /// </summary>
        protected void RequestClose() => _closeRequested.OnNext(this);

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            _closeRequested.OnCompleted();
            _closeRequested.Dispose();
        }
    }
}
