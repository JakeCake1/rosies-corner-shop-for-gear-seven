using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace Game.Flow
{
    /// <summary>
    /// Loads the Home and Gameplay scenes through Addressables (single mode, so the previous scene and its
    /// scope are unloaded).
    /// </summary>
    public sealed class SceneLoader
    {
        /// <summary>Address of the home scene.</summary>
        public const string HomeScene = "scene/Home";

        /// <summary>Address of the gameplay scene.</summary>
        public const string GameplayScene = "scene/Gameplay";

        /// <summary>
        /// Loads a scene, replacing the current one.
        /// </summary>
        /// <param name="address">Scene address.</param>
        /// <param name="ct">Cancels waiting.</param>
        /// <returns>Completes when the scene is active and its scope is built.</returns>
        /// <exception cref="System.OperationCanceledException">Waiting was cancelled.</exception>
        public async UniTask LoadAsync(string address, CancellationToken ct)
        {
            // Single mode unloads the previous scene, and Addressables releases that scene's handle itself.
            await Addressables.LoadSceneAsync(address, LoadSceneMode.Single).ToUniTask(cancellationToken: ct);
        }
    }
}
