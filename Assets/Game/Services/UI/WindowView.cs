using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Services.UI.Elements;
using UniRx;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// Base of every window: fade (and card pop) transitions, input shield while animating, close requests.
    /// The layout may contain an element named <c>card</c>; it pops in, the rest only fades.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    public abstract class WindowView<TViewModel> : View<TViewModel>, IWindow where TViewModel : ViewModel
    {
        /// <summary>Name of the optional card element that pops in.</summary>
        protected const string CardName = "card";

        private const float FadeSeconds = 0.2f;
        private const float PopFromScale = 0.86f;
        private const string ShieldClass = "window-shield";

        private readonly Subject<IWindow> _closeRequested = new Subject<IWindow>();
        private readonly UniTaskCompletionSource _closed = new UniTaskCompletionSource();
        private VisualElement _card;
        private VisualElement _shield;

        /// <inheritdoc/>
        public abstract UILayer Layer { get; }

        /// <inheritdoc/>
        public virtual bool IsClosedByBlocker => false;

        /// <inheritdoc/>
        public IObservable<IWindow> CloseRequested => _closeRequested;

        /// <inheritdoc/>
        public UniTask Closed => _closed.Task;

        /// <inheritdoc/>
        public async UniTask ShowAsync(CancellationToken ct)
        {
            SetShield(true);
            Root.style.opacity = 0f;
            var seq = DOTween.Sequence().SetUpdate(true).SetId(this).Append(Root.DOFade(1f, FadeSeconds));
            if (_card != null)
            {
                VisualElementTweens.SetScale(_card, UnityEngine.Vector2.one * PopFromScale);
                _ = seq.Join(_card.DOScale(1f, FadeSeconds * 1.6f).SetEase(Ease.OutBack));
            }

            // CancelAwait, not Kill: on app quit DOTween clears its tween lists before the scopes dispose and cancel
            // this token, and killing the stale tween then throws inside DOTween. Unbind kills the tweens by id.
            await seq.ToUniTask(TweenCancelBehaviour.CancelAwait, ct);
            SetShield(false);
        }

        /// <inheritdoc/>
        public async UniTask HideAsync(CancellationToken ct)
        {
            // A close during the opening would otherwise run both fades against each other. Killing the opening
            // completes ShowAsync, which lifts the shield, so the shield goes up after it.
            DOTween.Kill(this);
            SetShield(true);
            await Root.DOFade(0f, FadeSeconds * 0.75f).SetUpdate(true).SetId(this)
                .ToUniTask(TweenCancelBehaviour.CancelAwait, ct);
        }

        /// <inheritdoc/>
        public void Release()
        {
            Unbind();
            _closeRequested.OnCompleted();
            _closeRequested.Dispose();
            _closed.TrySetResult();
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            _card = Root.Q(CardName);
            _shield = new VisualElement { pickingMode = PickingMode.Position };
            _shield.AddToClassList(ShieldClass);
            OnLayoutAttached();
        }

        /// <summary>
        /// Queries the window's own elements; called once after the layout is attached.
        /// </summary>
        protected abstract void OnLayoutAttached();

        /// <summary>
        /// Asks the UI service to close this window.
        /// </summary>
        protected void RequestClose() => _closeRequested.OnNext(this);

        /// <summary>
        /// Binds a button tap to an action for the lifetime of the binding.
        /// </summary>
        /// <param name="button">Button.</param>
        /// <param name="action">Action to run.</param>
        protected void OnTap(Button button, Action action) =>
            button.OnTapAsObservable().Subscribe(_ => action()).AddTo(Bindings);

        private void SetShield(bool isOn)
        {
            if (isOn)
            {
                Root.Add(_shield);
                _shield.BringToFront();
            }
            else
            {
                _shield.RemoveFromHierarchy();
            }
        }
    }
}
