using System;
using System.IO;
using System.Text;

namespace Game.Services.Save
{
    /// <summary>
    /// Stores saves as files under a root folder (normally <c>Application.persistentDataPath</c>) with atomic
    /// replace and a <c>.bak</c> copy of the previous version.
    /// </summary>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private readonly string _root;

        /// <summary>
        /// Creates the storage.
        /// </summary>
        /// <param name="root">Folder for save files, resolved on the main thread at startup so writes can run
        /// on a background thread.</param>
        public FileSaveStorage(string root)
        {
            _root = root;
        }

        /// <inheritdoc/>
        public string ReadMain(string key) => ReadIfExists(MainPath(key));

        /// <inheritdoc/>
        public string ReadBackup(string key) => ReadIfExists(MainPath(key) + ".bak");

        /// <inheritdoc/>
        public void Write(string key, string content)
        {
            Directory.CreateDirectory(_root);
            var main = MainPath(key);
            var temp = main + ".tmp";
            var backup = main + ".bak";
            using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                var bytes = Encoding.UTF8.GetBytes(content);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (File.Exists(main))
            {
                try
                {
                    File.Replace(temp, main, backup);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                    // Some platforms lack File.Replace: keep the backup by hand, then move.
                    File.Copy(main, backup, true);
                    File.Delete(main);
                }
            }

            File.Move(temp, main);
        }

        /// <inheritdoc/>
        public void Quarantine(string key)
        {
            var main = MainPath(key);
            if (!File.Exists(main))
            {
                return;
            }

            var aside = Path.Combine(_root, $"{key}.corrupt_{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            File.Move(main, aside);
        }

        /// <inheritdoc/>
        public void Delete(string key)
        {
            var main = MainPath(key);
            foreach (var path in new[] { main, main + ".bak", main + ".tmp" })
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private static string ReadIfExists(string path) => File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : null;

        private string MainPath(string key) => Path.Combine(_root, key + ".json");
    }
}
