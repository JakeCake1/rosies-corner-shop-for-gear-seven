using System.Collections.Generic;
using Game.Services.Save;

namespace Game.Tests.EditMode.Services.Save
{
    /// <summary>
    /// In-memory <see cref="ISaveStorage"/> for tests.
    /// </summary>
    internal sealed class InMemorySaveStorage : ISaveStorage
    {
        public readonly Dictionary<string, string> Main = new Dictionary<string, string>();
        public readonly Dictionary<string, string> Backup = new Dictionary<string, string>();
        public int Quarantined;

        public string ReadMain(string key) => Main.TryGetValue(key, out var v) ? v : null;

        public string ReadBackup(string key) => Backup.TryGetValue(key, out var v) ? v : null;

        public void Write(string key, string content)
        {
            if (Main.TryGetValue(key, out var old)) Backup[key] = old;
            Main[key] = content;
        }

        public void Quarantine(string key)
        {
            Main.Remove(key);
            Quarantined++;
        }

        public void Delete(string key)
        {
            Main.Remove(key);
            Backup.Remove(key);
        }
    }
}
