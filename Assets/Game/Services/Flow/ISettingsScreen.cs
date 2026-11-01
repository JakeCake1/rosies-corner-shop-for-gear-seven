using System;
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
        /// <returns>Completes when the dialog closes.</returns>
        UniTask OpenAsync(Action restartLevel);
    }
}
