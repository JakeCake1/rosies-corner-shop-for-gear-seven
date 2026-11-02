using DG.Tweening;
using Game.Services.UI.Elements;
using UniRx;

namespace Game.Services.UI
{
    /// <summary>
    /// A view bound to one ViewModel at a time: subscriptions live in <see cref="Bindings"/> and are disposed,
    /// together with the view's tweens and runtime-created elements, on <see cref="Unbind"/>.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type.</typeparam>
    public abstract class View<TViewModel> : ElementView where TViewModel : class
    {
        private CompositeDisposable _bindings = new CompositeDisposable();

        /// <summary>The bound ViewModel, or null.</summary>
        protected TViewModel ViewModel { get; private set; }

        /// <summary>Subscriptions of the current binding.</summary>
        protected CompositeDisposable Bindings => _bindings;

        /// <summary>
        /// Binds the view; an existing binding is released first.
        /// </summary>
        /// <param name="viewModel">ViewModel to show.</param>
        public void Bind(TViewModel viewModel)
        {
            Unbind();
            ViewModel = viewModel;
            OnBind(viewModel);
        }

        /// <summary>
        /// Releases the binding: subscriptions, tweens (<c>SetId(this)</c>) and spawned elements.
        /// </summary>
        public void Unbind()
        {
            if (ViewModel == null)
            {
                return;
            }

            _bindings.Dispose();
            _bindings = new CompositeDisposable();
            DOTween.Kill(this);
            OnUnbind();
            ClearSpawned();
            ViewModel = null;
        }

        /// <summary>
        /// Subscribes the view to <paramref name="viewModel"/>.
        /// </summary>
        /// <param name="viewModel">ViewModel to show.</param>
        protected abstract void OnBind(TViewModel viewModel);

        /// <summary>
        /// Resets view-specific state on unbind.
        /// </summary>
        protected virtual void OnUnbind()
        {
        }
    }
}
