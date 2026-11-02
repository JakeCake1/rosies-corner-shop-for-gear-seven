using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Flow
{
    /// <summary>
    /// Opens the settings dialog; shared by the home screen and the gameplay HUD.
    /// </summary>
    public interface ISettingsScreen
    {
        /// <summary>
        /// Shows settings: sound, music and vibration switches, rename, reset progress, and in a level the
        /// restart and back-to-shop buttons.
        /// </summary>
        /// <param name="restartLevel">Restart action shown inside a level; null on the home screen.</param>
        /// <param name="ct">
        /// The opener's lifetime (the home screen's showing or the current level); cancelling it closes the dialog
        /// and a rename prompt opened from it.
        /// </param>
        /// <returns>Completes when the dialog closes.</returns>
        /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
        UniTask OpenAsync(Action restartLevel, CancellationToken ct);
    }
}
