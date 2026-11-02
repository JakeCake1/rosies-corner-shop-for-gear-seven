using System.Collections.Generic;
using Game.Services.Content;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// All clips of the game: one per <see cref="SoundId"/>, the basket ship sound per combo step and the music loop.
    /// Loaded once at bootstrap through Addressables by <see cref="AudioService"/>.
    /// </summary>
    /// <remarks>
    /// The clips are the prototype's Web Audio synth rendered to WAV ("game_mix" set of EVG-9): they share one gain,
    /// so the prototype's relative loudness is baked in and every clip plays at full volume. Clips are referenced
    /// directly: they are small and always needed together, so per-clip handles would add bookkeeping only.
    /// </remarks>
    [CreateAssetMenu(menuName = "Game/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject, IValidatableConfig
    {
        /// <summary>
        /// Addressables key of the library asset.
        /// </summary>
        public const string AddressKey = "config/audio";

        /// <summary>
        /// Number of ship clips: combo steps 0..6, each two semitones higher (the prototype caps the rise at +12).
        /// </summary>
        public const int ShipSteps = 7;

        [SerializeField] private List<AudioLibraryEntry> _effects = new List<AudioLibraryEntry>();
        [Tooltip("Basket ship arpeggio for combo steps 0..6; later combos reuse the last clip.")]
        [SerializeField] private List<AudioClip> _ship = new List<AudioClip>();
        [Tooltip("Background music; must loop seamlessly.")]
        [SerializeField] private AudioClip _music;

        private Dictionary<SoundId, AudioClip> _byId;

        /// <summary>
        /// Background music loop; played by the audio service while music is on.
        /// </summary>
        public AudioClip Music => _music;

        /// <summary>
        /// Returns the clip of a sound effect.
        /// </summary>
        /// <param name="id">The sound.</param>
        /// <returns>The clip, or null when the library has none for <paramref name="id"/> (caught by validation).</returns>
        public AudioClip Get(SoundId id)
        {
            if (_byId == null)
            {
                _byId = new Dictionary<SoundId, AudioClip>();
                foreach (var entry in _effects)
                {
                    if (entry.Clip != null)
                    {
                        _byId[entry.Id] = entry.Clip;
                    }
                }
            }

            return _byId.TryGetValue(id, out var clip) ? clip : null;
        }

        /// <summary>
        /// Returns the basket ship clip for a combo step.
        /// </summary>
        /// <param name="combo">
        /// Consecutive ships without a counter drop, starting at 0; negative values count as 0 and values above
        /// the last step use the highest clip.
        /// </param>
        /// <returns>The clip, or null when the library has no ship clips.</returns>
        public AudioClip Ship(int combo) => _ship.Count == 0 ? null : _ship[Mathf.Clamp(combo, 0, _ship.Count - 1)];

        /// <inheritdoc/>
        public void Validate(List<string> errors)
        {
            var seen = new HashSet<SoundId>();
            foreach (var entry in _effects)
            {
                if (!seen.Add(entry.Id))
                {
                    errors.Add($"{name}: {entry.Id} is listed twice.");
                }
                if (entry.Clip == null)
                {
                    errors.Add($"{name}: {entry.Id} has no clip.");
                }
            }

            foreach (SoundId id in Enum.GetValues(typeof(SoundId)))
            {
                if (!seen.Contains(id))
                {
                    errors.Add($"{name}: no entry for {id}.");
                }
            }

            if (_ship.Count != ShipSteps)
            {
                errors.Add($"{name}: expected {ShipSteps} ship clips, got {_ship.Count}.");
            }
            for (var i = 0; i < _ship.Count; i++)
            {
                if (_ship[i] == null)
                {
                    errors.Add($"{name}: ship clip {i} is missing.");
                }
            }

            if (_music == null)
            {
                errors.Add($"{name}: music clip is missing.");
            }
        }
    }
}
