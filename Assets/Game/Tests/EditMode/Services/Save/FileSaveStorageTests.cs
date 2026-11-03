using System.IO;
using Game.Services.Save;
using NUnit.Framework;

namespace Game.Tests.EditMode.Services.Save
{
    public sealed class FileSaveStorageTests
    {
        private const string Key = "profile";

        private string _top;
        private string _root;
        private FileSaveStorage _storage;

        [SetUp]
        public void SetUp()
        {
            _top = Path.Combine(Path.GetTempPath(), "rcs_save_" + Path.GetRandomFileName());
            _root = Path.Combine(_top, "nested");
            _storage = new FileSaveStorage(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_top))
            {
                Directory.Delete(_top, true);
            }
        }

        [Test]
        public void ReadMain_WhenNothingWritten_ReturnsNull()
        {
            Assert.That(_storage.ReadMain(Key), Is.Null);
            Assert.That(_storage.ReadBackup(Key), Is.Null);
        }

        [Test]
        public void Write_WhenRootMissing_CreatesFolderAndStoresContent()
        {
            _storage.Write(Key, "{\"a\":1}");

            Assert.That(_storage.ReadMain(Key), Is.EqualTo("{\"a\":1}"));
            Assert.That(_storage.ReadBackup(Key), Is.Null);
        }

        [Test]
        public void Write_WithNonAsciiText_RoundTripsAsUtf8()
        {
            const string text = "Rosie Магазин";

            _storage.Write(Key, text);

            Assert.That(_storage.ReadMain(Key), Is.EqualTo(text));
        }

        [Test]
        public void Write_WhenMainExists_KeepsPreviousVersionAsBackup()
        {
            _storage.Write(Key, "one");

            _storage.Write(Key, "two");

            Assert.That(_storage.ReadMain(Key), Is.EqualTo("two"));
            Assert.That(_storage.ReadBackup(Key), Is.EqualTo("one"));
        }

        [Test]
        public void Write_Always_LeavesNoTempFileBehind()
        {
            _storage.Write(Key, "one");
            _storage.Write(Key, "two");

            Assert.That(Directory.GetFiles(_root, "*.tmp"), Is.Empty);
        }

        [Test]
        public void Write_ForDifferentKeys_KeepsThemSeparate()
        {
            _storage.Write("a", "first");
            _storage.Write("b", "second");

            Assert.That(_storage.ReadMain("a"), Is.EqualTo("first"));
            Assert.That(_storage.ReadMain("b"), Is.EqualTo("second"));
        }

        [Test]
        public void Quarantine_WhenMainExists_MovesItAsideWithItsContent()
        {
            _storage.Write(Key, "broken");

            _storage.Quarantine(Key);

            var aside = Directory.GetFiles(_root, Key + ".corrupt_*.json");
            Assert.That(_storage.ReadMain(Key), Is.Null);
            Assert.That(aside, Has.Length.EqualTo(1));
            Assert.That(File.ReadAllText(aside[0]), Is.EqualTo("broken"));
        }

        [Test]
        public void Quarantine_WhenMainMissing_DoesNothing()
        {
            Assert.DoesNotThrow(() => _storage.Quarantine(Key));
        }

        [Test]
        public void Delete_WhenMainAndBackupExist_RemovesBoth()
        {
            _storage.Write(Key, "one");
            _storage.Write(Key, "two");

            _storage.Delete(Key);

            Assert.That(_storage.ReadMain(Key), Is.Null);
            Assert.That(_storage.ReadBackup(Key), Is.Null);
        }

        [Test]
        public void Delete_WhenNothingStored_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _storage.Delete(Key));
        }
    }
}
