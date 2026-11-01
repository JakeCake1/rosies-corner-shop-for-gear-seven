using System.Collections.Generic;
using System.IO;
using Game.Services.Save;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// In-memory <see cref="ISaveStorage"/> for tests; can simulate a full disk.
    /// </summary>
    internal sealed class InMemorySaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Main = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Backup = new Dictionary<string, string>();
        public readonly List<string> Quarantined = new List<string>();
        public readonly List<string> Deleted = new List<string>();
        public bool IsFull;

        public string ReadMain(string key) => Main.TryGetValue(key, out var v) ? v : null;

        public string ReadBackup(string key) => Backup.TryGetValue(key, out var v) ? v : null;

        public void Write(string key, string content)
        {
            if (IsFull) throw new IOException("Disk full");

            if (Main.TryGetValue(key, out var old)) Backup[key] = old;
            Main[key] = content;
        }

        public void Quarantine(string key)
        {
            Main.Remove(key);
            Quarantined.Add(key);
        }

        public void Delete(string key)
        {
            Main.Remove(key);
            Backup.Remove(key);
            Deleted.Add(key);
        }
    }
}
