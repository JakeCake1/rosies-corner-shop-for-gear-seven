using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.UI
{
    /// <summary>
    /// Opens and closes windows on layers. Game code never instantiates or enables windows itself.
    /// </summary>
    /// <remarks>
    /// Window prefabs are addressable under <c>ui/&lt;WindowTypeName&gt;</c>. Opening a window type that is already
    /// open replaces it (closes the old one first). Popups get a dimmed blocker behind the top one.
    /// </remarks>
    public interface IUIService
    {
        /// <summary>
        /// True while any popup is open; gameplay ignores taps on the board meanwhile.
        /// </summary>
        bool IsPopupOpen { get; }

        /// <summary>
        /// Loads, binds and shows a window.
        /// </summary>
        /// <typeparam name="TWindow">Window view type; its prefab address is <c>ui/</c> + type name.</typeparam>
        /// <typeparam name="TViewModel">The window's view model type.</typeparam>
        /// <param name="viewModel">View model to bind; ownership passes to the UI service, which disposes it on
        /// close.</param>
        /// <param name="ct">Cancels the show transition.</param>
        /// <returns>The shown window.</returns>
        /// <exception cref="System.OperationCanceledException">Opening was cancelled.</exception>
        UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
            where TWindow : WindowView<TViewModel>
            where TViewModel : ViewModel;

        /// <summary>
        /// Hides, unbinds and destroys a window and disposes its view model. Does nothing if it is not open.
        /// </summary>
        /// <param name="window">The window to close.</param>
        /// <returns>Completes after the hide transition.</returns>
        UniTask CloseAsync(IWindow window);

        /// <summary>
        /// Closes every open window of the given type.
        /// </summary>
        /// <typeparam name="TWindow">Window type.</typeparam>
        /// <returns>Completes when all are closed.</returns>
        UniTask CloseAsync<TWindow>() where TWindow : IWindow;

        /// <summary>
        /// Closes all windows on a layer, top first.
        /// </summary>
        /// <param name="layer">Layer to clear.</param>
        /// <returns>Completes when the layer is empty.</returns>
        UniTask CloseAllAsync(UILayer layer);

        /// <summary>
        /// Back navigation (Escape / Android back): closes the top popup if it allows closing.
        /// </summary>
        void Back();
    }
}
