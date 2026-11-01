using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Services.Audio
{
    /// <summary>
    /// Builds the game's sound effects and the "70s radio" music loop as <see cref="AudioClip"/>s from simple
    /// oscillators, mirroring the prototype's Web Audio synth (no audio files ship with the game).
    /// </summary>
    public static class SynthClips
    {
        private const int SampleRate = 22050;
        private const float StepSeconds = 0.29f;

        /// <summary>
        /// Generates all effect clips.
        /// </summary>
        /// <returns>One clip per <see cref="SoundId"/>.</returns>
        public static Dictionary<SoundId, AudioClip> CreateEffects()
        {
            return new Dictionary<SoundId, AudioClip>
            {
                [SoundId.TapBox] = Tone("tap_box", 0.08f, t => Noise(t) * 0.3f + Sine(180f, t) * 0.7f, 30f),
                [SoundId.TapCan] = Tone("tap_can", 0.12f, t => Square(620f, t) * 0.25f + Sine(1240f, t) * 0.3f, 25f),
                [SoundId.TapBottle] = Tone("tap_bottle", 0.25f, t => Sine(1320f, t) * 0.6f + Sine(1980f, t) * 0.25f, 12f),
                [SoundId.TapStick] = Tone("tap_stick", 0.05f, t => Noise(t) * 0.6f + Sine(900f, t) * 0.3f, 60f),
                [SoundId.TapRound] = Tone("tap_round", 0.12f, t => Sine(330f - t * 600f, t) * 0.8f, 22f),
                [SoundId.TapSoft] = Tone("tap_soft", 0.08f, t => Sine(440f, t) * 0.4f, 35f),
                [SoundId.Clink] = Tone("clink", 0.3f, t => Sine(1568f, t) * 0.45f + Sine(2349f, t) * 0.25f, 11f),
                [SoundId.Ship] = Arpeggio("ship", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.07f, 0.35f),
                [SoundId.Fold] = Tone("fold", 0.25f, t => Noise(t) * (1f - t * 3f) * 0.5f + Sine(140f, t) * 0.4f, 9f),
                [SoundId.Roll] = Tone("roll", 0.22f, t => Noise(t) * 0.25f * Mathf.Sin(t * 90f) + Sine(110f, t) * 0.35f, 8f),
                [SoundId.Snip] = Tone("snip", 0.12f, t => Noise(t) * 0.7f, 40f),
                [SoundId.Magic] = Arpeggio("magic", new[] { 783.99f, 987.77f, 1174.66f, 1567.98f, 1975.53f }, 0.05f, 0.4f),
                [SoundId.Coin] = Arpeggio("coin", new[] { 987.77f, 1318.51f }, 0.07f, 0.3f),
                [SoundId.Win] = Arpeggio("win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 783.99f, 1046.5f }, 0.12f, 0.6f),
                [SoundId.Fail] = Arpeggio("fail", new[] { 392f, 329.63f, 261.63f }, 0.13f, 0.4f, true),
            };
        }

        /// <summary>
        /// Generates the quiet music loop: chords C–Am–F–G, one step every 290 ms.
        /// </summary>
        /// <returns>A loopable clip.</returns>
        public static AudioClip CreateMusicLoop()
        {
            var chords = new[]
            {
                new[] { 261.63f, 329.63f, 392f }, new[] { 220f, 261.63f, 329.63f },
                new[] { 174.61f, 220f, 261.63f }, new[] { 196f, 246.94f, 293.66f },
            };
            const int stepsPerChord = 8;
            var steps = chords.Length * stepsPerChord;
            var stepSamples = (int)(StepSeconds * SampleRate);
            var data = new float[steps * stepSamples];
            for (var s = 0; s < steps; s++)
            {
                var chord = chords[s / stepsPerChord];
                var note = chord[s % chord.Length] * (s % 4 == 3 ? 2f : 1f);
                var bass = chord[0] / 2f;
                for (var i = 0; i < stepSamples; i++)
                {
                    var t = i / (float)SampleRate;
                    var env = Mathf.Exp(-t * 6f);
                    var v = Triangle(note, t) * 0.12f * env;
                    if (s % 2 == 0) v += Sine(bass, t) * 0.10f * Mathf.Exp(-t * 3f);
                    data[s * stepSamples + i] = v;
                }
            }

            var clip = AudioClip.Create("music_loop", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Tone(string name, float seconds, Func<float, float> wave, float decay)
        {
            var n = (int)(seconds * SampleRate);
            var data = new float[n];
            for (var i = 0; i < n; i++)
            {
                var t = i / (float)SampleRate;
                var attack = Mathf.Clamp01(t / 0.004f);
                data[i] = Mathf.Clamp(wave(t) * attack * Mathf.Exp(-t * decay), -1f, 1f) * 0.8f;
            }

            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Arpeggio(string name, float[] notes, float step, float tail, bool isSad = false)
        {
            var n = (int)((notes.Length * step + tail) * SampleRate);
            var data = new float[n];
            for (var k = 0; k < notes.Length; k++)
            {
                var start = (int)(k * step * SampleRate);
                for (var i = start; i < n; i++)
                {
                    var t = (i - start) / (float)SampleRate;
                    var wave = isSad ? Triangle(notes[k], t) : Sine(notes[k], t) * 0.7f + Sine(notes[k] * 2f, t) * 0.15f;
                    data[i] += wave * Mathf.Exp(-t * 7f) * Mathf.Clamp01(t / 0.004f) * 0.35f;
                }
            }

            var clip = AudioClip.Create(name, n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float hz, float t) => Mathf.Sin(2f * Mathf.PI * hz * t);

        private static float Square(float hz, float t) => Mathf.Sign(Sine(hz, t));

        private static float Triangle(float hz, float t) => 2f * Mathf.Abs(2f * (t * hz - Mathf.Floor(t * hz + 0.5f))) - 1f;

        private static float Noise(float t)
        {
            // Deterministic hash noise so clips are identical on every start.
            var x = Mathf.Sin(t * 12.9898f * SampleRate) * 43758.5453f;
            return (x - Mathf.Floor(x)) * 2f - 1f;
        }
    }
}
