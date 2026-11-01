namespace Game.Services.Save
{
    /// <summary>
    /// Raw text storage per key; the platform-specific part of saving (file system, console API, cloud).
    /// </summary>
    /// <remarks>Implementations may be called from a background thread and must not touch Unity APIs.</remarks>
    public interface ISaveStorage
    {
        /// <summary>
        /// Reads the main copy of a key.
        /// </summary>
        /// <param name="key">Save slot key, e.g. "profile".</param>
        /// <returns>The stored text, or null when missing.</returns>
        string ReadMain(string key);

        /// <summary>
        /// Reads the backup kept from the previous successful write.
        /// </summary>
        /// <param name="key">Save slot key.</param>
        /// <returns>The backup text, or null when missing.</returns>
        string ReadBackup(string key);

        /// <summary>
        /// Writes atomically and keeps the previous main copy as the backup.
        /// </summary>
        /// <param name="key">Save slot key.</param>
        /// <param name="content">Full text to store.</param>
        /// <exception cref="System.IO.IOException">The device is out of space or the file is locked; the
        /// previous save stays intact.</exception>
        void Write(string key, string content);

        /// <summary>
        /// Moves a broken main copy aside so it can be investigated instead of being overwritten.
        /// </summary>
        /// <param name="key">Save slot key.</param>
        void Quarantine(string key);

        /// <summary>
        /// Deletes the main copy, backup and temp files (reset progress).
        /// </summary>
        /// <param name="key">Save slot key.</param>
        void Delete(string key);
    }
}
