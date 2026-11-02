using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Services.Assets;
using Game.Services.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Services.Audio
{
    /// <summary>
    /// Default <see cref="IAudioService"/>: clips from the <see cref="AudioLibrary"/> played through a small pool of
    /// audio sources on a persistent object.
    /// </summary>
    public sealed class AudioService : IAudioService, IDisposable
    {
        private const int Voices = 8;

        private readonly ISettingsService _settings;
        private readonly IAssetProvider _assets;
        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private readonly AudioSource _music;
        private readonly GameObject _host;
        private AudioLibrary _library;
        private int _next;

        /// <summary>
        /// Creates the audio host object; clips arrive later with <see cref="LoadAsync"/>.
        /// </summary>
        /// <param name="settings">Sound and music on/off.</param>
        /// <param name="assets">Loads and owns the library handle.</param>
        public AudioService(ISettingsService settings, IAssetProvider assets)
        {
            _settings = settings;
            _assets = assets;
            _host = new GameObject("[Audio]");
            Object.DontDestroyOnLoad(_host);
            // The scenes' cameras have no listener: the game is 2D UI and all sounds come from this persistent host.
            _host.AddComponent<AudioListener>();
            for (var i = 0; i < Voices; i++)
            {
                _voices[i] = _host.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }

            _music = _host.AddComponent<AudioSource>();
            _music.playOnAwake = false;
            _music.loop = true;
        }

        /// <inheritdoc/>
        public async UniTask LoadAsync(CancellationToken ct)
        {
            _library = await _assets.LoadAsync<AudioLibrary>(AudioLibrary.AddressKey, ct);
            _music.clip = _library.Music;
        }

        /// <inheritdoc/>
        public void Play(SoundId id)
        {
            if (_library != null) PlayClip(_library.Get(id));
        }

        /// <inheritdoc/>
        public void PlayTap(string silhouette) => Play(TapSounds.For(silhouette));

        /// <inheritdoc/>
        public void PlayShip(int combo)
        {
            if (_library != null) PlayClip(_library.Ship(combo));
        }

        /// <inheritdoc/>
        public void ApplySettings()
        {
            if (_settings.IsMusicOn && _music.clip != null)
            {
                if (!_music.isPlaying) _music.Play();
            }
            else
            {
                _music.Stop();
            }
        }

        /// <inheritdoc/>
        public void SetPaused(bool isPaused)
        {
            if (isPaused) _music.Pause();
            else if (_settings.IsMusicOn) _music.UnPause();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_host != null) Object.Destroy(_host);
        }

        private void PlayClip(AudioClip clip)
        {
            if (!_settings.IsSoundOn || clip == null) return;

            // Round-robin voices so overlapping sounds (several items landing at once) don't cut each other off.
            var source = _voices[_next];
            _next = (_next + 1) % Voices;
            source.PlayOneShot(clip);
        }
    }
}
