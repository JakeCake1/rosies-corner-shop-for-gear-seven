using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.UI
{
    /// <summary>
    /// Opens and closes windows on their layers; owns the popup blocker and Back handling.
    /// </summary>
    public interface IUIService
    {
        /// <summary>Whether a popup is open.</summary>
        bool IsPopupOpen { get; }

        /// <summary>
        /// Loads the window layout (Addressables key <c>ui/{WindowType}</c>), binds it and plays its opening.
        /// An open window of the same type is closed first.
        /// </summary>
        /// <typeparam name="TWindow">Window view type.</typeparam>
        /// <typeparam name="TViewModel">ViewModel type.</typeparam>
        /// <param name="viewModel">ViewModel; disposed when the window closes.</param>
        /// <param name="ct">
        /// The opener's lifetime (its scene, screen or level): cancelling it stops loading, or closes the window if
        /// it is already open, so a window never outlives the code that handles its buttons.
        /// </param>
        /// <returns>The open window.</returns>
        /// <exception cref="System.OperationCanceledException">
        /// <paramref name="ct"/> was cancelled before the window finished opening; it is closed or never shown.
        /// </exception>
        UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
            where TWindow : WindowView<TViewModel>, new()
            where TViewModel : ViewModel;

        /// <summary>
        /// Closes a window.
        /// </summary>
        /// <param name="window">Window to close.</param>
        /// <returns>Completes when it is removed, also when it was already closing.</returns>
        UniTask CloseAsync(IWindow window);

        /// <summary>
        /// Closes every open window of a type.
        /// </summary>
        /// <typeparam name="TWindow">Window type.</typeparam>
        /// <returns>Completes when they are removed.</returns>
        UniTask CloseAsync<TWindow>() where TWindow : IWindow;

        /// <summary>
        /// Closes every window of a layer, top first.
        /// </summary>
        /// <param name="layer">Layer.</param>
        /// <returns>Completes when they are removed.</returns>
        UniTask CloseAllAsync(UILayer layer);

        /// <summary>
        /// Closes the top popup if it allows it (Android back / Escape).
        /// </summary>
        void Back();
    }
}
