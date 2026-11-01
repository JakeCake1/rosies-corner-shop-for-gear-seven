using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace Game.Services.UI
{
    /// <summary>
    /// A window managed by <see cref="IUIService"/>.
    /// </summary>
    public interface IWindow
    {
        /// <summary>Layer the window is shown on.</summary>
        UILayer Layer { get; }

        /// <summary>Whether tapping the popup blocker or Back closes the window.</summary>
        bool IsClosedByBlocker { get; }

        /// <summary>Root element (removed from the panel when the window closes).</summary>
        VisualElement Root { get; }

        /// <summary>Emits when the window asks to be closed.</summary>
        IObservable<IWindow> CloseRequested { get; }

        /// <summary>Completes once the window has been closed and released.</summary>
        UniTask Closed { get; }

        /// <summary>
        /// Plays the opening transition; input is blocked until it ends.
        /// </summary>
        /// <param name="ct">Cancels the transition.</param>
        /// <returns>Completes when the window is interactive.</returns>
        UniTask ShowAsync(CancellationToken ct);

        /// <summary>
        /// Plays the closing transition.
        /// </summary>
        /// <param name="ct">Cancels the transition.</param>
        /// <returns>Completes when the window is hidden.</returns>
        UniTask HideAsync(CancellationToken ct);

        /// <summary>
        /// Unbinds the window after it is hidden.
        /// </summary>
        void Release();
    }
}
