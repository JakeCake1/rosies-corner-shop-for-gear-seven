using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Services.UI
{
    /// <summary>
    /// What the UI service needs from any window, independent of its view model type.
    /// </summary>
    public interface IWindow
    {
        /// <summary>Layer the window opens on.</summary>
        UILayer Layer { get; }

        /// <summary>Whether tapping the dimmed blocker behind this popup closes it.</summary>
        bool IsClosedByBlocker { get; }

        /// <summary>The window's root object.</summary>
        GameObject Root { get; }

        /// <summary>
        /// Emits when the window asks to be closed (its close button); the UI service handles it.
        /// </summary>
        IObservable<IWindow> CloseRequested { get; }

        /// <summary>
        /// Plays the show transition; input is blocked until it ends.
        /// </summary>
        /// <param name="ct">Cancelled when the window closes early; kills the tween.</param>
        /// <returns>Completes when the window is interactive.</returns>
        /// <exception cref="OperationCanceledException">The transition was cancelled.</exception>
        UniTask ShowAsync(CancellationToken ct);

        /// <summary>
        /// Plays the hide transition with input blocked.
        /// </summary>
        /// <param name="ct">Cancelled on scene change; kills the tween.</param>
        /// <returns>Completes when the window is invisible.</returns>
        /// <exception cref="OperationCanceledException">The transition was cancelled.</exception>
        UniTask HideAsync(CancellationToken ct);

        /// <summary>
        /// Unbinds the view; the UI service then disposes the view model and destroys the view.
        /// </summary>
        void Release();
    }
}
