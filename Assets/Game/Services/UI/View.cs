using DG.Tweening;
using UniRx;
using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// Thin <see cref="MonoBehaviour"/> view bound to a view model; holds per-bind subscriptions so it can be
    /// rebound (pooled list items, reopened windows).
    /// </summary>
    /// <typeparam name="TViewModel">The view model type.</typeparam>
    public abstract class View<TViewModel> : MonoBehaviour where TViewModel : class
    {
        private CompositeDisposable _bindings = new CompositeDisposable();

        /// <summary>
        /// The bound view model; null while unbound.
        /// </summary>
        protected TViewModel ViewModel { get; private set; }

        /// <summary>
        /// Subscriptions of the current binding; cleared by <see cref="Unbind"/>.
        /// </summary>
        protected CompositeDisposable Bindings => _bindings;

        /// <summary>
        /// Binds the view to <paramref name="viewModel"/>, unbinding any previous one first.
        /// </summary>
        /// <param name="viewModel">View model to display; the caller keeps ownership.</param>
        public void Bind(TViewModel viewModel)
        {
            Unbind();
            ViewModel = viewModel;
            OnBind(viewModel);
        }

        /// <summary>
        /// Drops all subscriptions and kills this view's tweens (tweens created with <c>SetId(this)</c>).
        /// </summary>
        public void Unbind()
        {
            if (ViewModel == null) return;

            _bindings.Dispose();
            _bindings = new CompositeDisposable();
            DOTween.Kill(this);
            OnUnbind();
            ViewModel = null;
        }

        /// <summary>
        /// Subscribes UI elements to the view model; store every subscription in <see cref="Bindings"/>.
        /// </summary>
        /// <param name="viewModel">The view model being bound.</param>
        protected abstract void OnBind(TViewModel viewModel);

        /// <summary>
        /// Extra cleanup when unbinding (listeners added in code, pooled children); called before the view model
        /// reference is cleared.
        /// </summary>
        protected virtual void OnUnbind()
        {
        }

        /// <summary>
        /// Unbinds when the object is destroyed.
        /// </summary>
        protected virtual void OnDestroy() => Unbind();
    }
}
