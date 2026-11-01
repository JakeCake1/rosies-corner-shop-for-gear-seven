using System;
using System.Collections.Generic;
using Game.Services.Progress;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Services.Audio
{
    /// <summary>
    /// Default <see cref="IAudioService"/>: synthesized clips played through a small pool of audio sources on a
    /// persistent object.
    /// </summary>
    public sealed class AudioService : IAudioService, IDisposable
    {
        private const int Voices = 8;
        private const float MusicVolume = 0.35f;

        private readonly IProgressService _progress;
        private readonly Dictionary<SoundId, AudioClip> _clips;
        private readonly AudioSource[] _voices = new AudioSource[Voices];
        private readonly AudioSource _music;
        private readonly GameObject _host;
        private int _next;

        /// <summary>
        /// Creates the audio host object and synthesizes all clips (a few milliseconds at startup).
        /// </summary>
        /// <param name="progress">Holds the sound and music settings.</param>
        public AudioService(IProgressService progress)
        {
            _progress = progress;
            _host = new GameObject("[Audio]");
            Object.DontDestroyOnLoad(_host);
            for (var i = 0; i < Voices; i++)
            {
                _voices[i] = _host.AddComponent<AudioSource>();
                _voices[i].playOnAwake = false;
            }

            _music = _host.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = MusicVolume;
            _music.clip = SynthClips.CreateMusicLoop();
            _clips = SynthClips.CreateEffects();
        }

        /// <inheritdoc/>
        public void Play(SoundId id, float pitch = 1f)
        {
            if (!_progress.Data.IsSoundOn || !_clips.TryGetValue(id, out var clip)) return;

            var source = _voices[_next];
            _next = (_next + 1) % Voices;
            source.pitch = pitch;
            source.PlayOneShot(clip);
        }

        /// <inheritdoc/>
        public void PlayTap(string silhouette)
        {
            switch (silhouette)
            {
                case "box": Play(SoundId.TapBox); break;
                case "can": Play(SoundId.TapCan); break;
                case "bottle": Play(SoundId.TapBottle); break;
                case "stick": Play(SoundId.TapStick); break;
                case "round": Play(SoundId.TapRound); break;
                default: Play(SoundId.TapSoft); break;
            }
        }

        /// <inheritdoc/>
        public void ApplySettings()
        {
            if (_progress.Data.IsMusicOn)
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
            else if (_progress.Data.IsMusicOn) _music.UnPause();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_host != null) Object.Destroy(_host);
        }
    }
}
