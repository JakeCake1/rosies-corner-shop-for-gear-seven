using System;
using System.Threading;

namespace Game.Services.UI
{
    /// <summary>
    /// Bookkeeping of one window that <see cref="UIService"/> has open on a layer stack: the window, its view model
    /// and the subscriptions that close it.
    /// </summary>
    internal sealed class OpenWindow
    {
        /// <summary>
        /// Creates the record when <see cref="UIService"/> opens a window.
        /// </summary>
        /// <param name="window">The opened window; disposed by the UI service when it closes.</param>
        /// <param name="viewModel">The window's view model; disposed together with the window.</param>
        public OpenWindow(IWindow window, ViewModel viewModel)
        {
            Window = window;
            ViewModel = viewModel;
        }

        /// <summary>The opened window.</summary>
        public IWindow Window { get; }

        /// <summary>The window's view model; owned by the UI service while the window is open.</summary>
        public ViewModel ViewModel { get; }

        /// <summary>Subscription to the view model's close request; disposed when the window closes.</summary>
        public IDisposable CloseSubscription { get; set; }

        /// <summary>Closes the window when the opener's token is cancelled; disposed when the window closes.</summary>
        public CancellationTokenRegistration OpenerRegistration { get; set; }

        /// <summary>True while the hide animation runs, so a second close request is ignored.</summary>
        public bool IsClosing { get; set; }
    }
}
