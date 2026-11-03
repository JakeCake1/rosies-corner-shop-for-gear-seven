using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Audio;

namespace Game.Tests.EditMode.Features.Gameplay.Session
{
    /// <summary>
    /// Records the sounds a test triggered.
    /// </summary>
    internal sealed class FakeAudioService : IAudioService
    {
        public List<SoundId> Played { get; } = new List<SoundId>();

        public UniTask LoadAsync(CancellationToken ct) => UniTask.CompletedTask;

        public void Play(SoundId id) => Played.Add(id);

        public void PlayTap(string silhouette)
        {
        }

        public void PlayShip(int combo)
        {
        }

        public void ApplySettings()
        {
        }

        public void SetPaused(bool isPaused)
        {
        }
    }
}
