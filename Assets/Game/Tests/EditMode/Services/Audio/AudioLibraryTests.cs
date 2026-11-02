using System;
using System.Collections.Generic;
using System.Linq;
using Game.Services.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Tests.EditMode.Services.Audio
{
    public sealed class AudioLibraryTests
    {
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in _created)
            {
                Object.DestroyImmediate(obj);
            }

            _created.Clear();
        }

        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(3, 3)]
        [TestCase(6, 6)]
        [TestCase(9, 6)]
        public void Ship_ReturnsClipOfComboStepClampedToRange(int combo, int expectedStep)
        {
            var ship = Clips("ship", AudioLibrary.ShipSteps);
            var library = Build(AllEffects(), ship, Clip("music"));

            var clip = library.Ship(combo);

            Assert.That(clip, Is.SameAs(ship[expectedStep]));
        }

        [Test]
        public void Get_ReturnsClipOfEachSound()
        {
            var effects = AllEffects();
            var library = Build(effects, Clips("ship", AudioLibrary.ShipSteps), Clip("music"));

            var clips = effects.Keys.Select(library.Get).ToList();

            Assert.That(clips, Is.EqualTo(effects.Values.ToList()));
        }

        [Test]
        public void Validate_WhenComplete_ReportsNoErrors()
        {
            var library = Build(AllEffects(), Clips("ship", AudioLibrary.ShipSteps), Clip("music"));
            var errors = new List<string>();

            library.Validate(errors);

            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void Validate_WhenSoundMissing_ReportsIt()
        {
            var effects = AllEffects();
            effects.Remove(SoundId.Snip);
            var library = Build(effects, Clips("ship", AudioLibrary.ShipSteps), Clip("music"));
            var errors = new List<string>();

            library.Validate(errors);

            Assert.That(errors, Has.Some.Contains("no entry for Snip"));
        }

        [Test]
        public void Validate_WhenShipStepsOrMusicMissing_ReportsBoth()
        {
            var library = Build(AllEffects(), Clips("ship", AudioLibrary.ShipSteps - 1), null);
            var errors = new List<string>();

            library.Validate(errors);

            Assert.That(errors, Has.Some.Contains("ship clips").And.Some.Contains("music clip is missing"));
        }

        private Dictionary<SoundId, AudioClip> AllEffects() =>
            Enum.GetValues(typeof(SoundId)).Cast<SoundId>().ToDictionary(id => id, id => Clip(id.ToString()));

        private List<AudioClip> Clips(string prefix, int count) =>
            Enumerable.Range(0, count).Select(i => Clip(prefix + i)).ToList();

        private AudioClip Clip(string clipName)
        {
            var clip = AudioClip.Create(clipName, 1, 1, 44100, false);
            _created.Add(clip);
            return clip;
        }

        private AudioLibrary Build(Dictionary<SoundId, AudioClip> effects, List<AudioClip> ship, AudioClip music)
        {
            var library = ScriptableObject.CreateInstance<AudioLibrary>();
            _created.Add(library);
            var so = new SerializedObject(library);
            var entries = so.FindProperty("_effects");
            entries.arraySize = effects.Count;
            var i = 0;
            foreach (var kv in effects)
            {
                var entry = entries.GetArrayElementAtIndex(i++);
                entry.FindPropertyRelative("_id").intValue = (int)kv.Key;
                entry.FindPropertyRelative("_clip").objectReferenceValue = kv.Value;
            }

            var shipProperty = so.FindProperty("_ship");
            shipProperty.arraySize = ship.Count;
            for (var k = 0; k < ship.Count; k++)
            {
                shipProperty.GetArrayElementAtIndex(k).objectReferenceValue = ship[k];
            }

            so.FindProperty("_music").objectReferenceValue = music;
            so.ApplyModifiedPropertiesWithoutUndo();
            return library;
        }
    }
}
