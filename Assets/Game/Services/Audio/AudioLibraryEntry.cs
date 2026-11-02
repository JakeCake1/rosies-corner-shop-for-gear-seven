using System;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// One sound effect of the <see cref="AudioLibrary"/> and its clip; edited in the library's Inspector list.
    /// </summary>
    [Serializable]
    public sealed class AudioLibraryEntry
    {
        [SerializeField] private SoundId _id;
        [SerializeField] private AudioClip _clip;

        /// <summary>The sound this clip plays for.</summary>
        public SoundId Id => _id;

        /// <summary>The rendered clip; never null in a valid library.</summary>
        public AudioClip Clip => _clip;
    }
}
