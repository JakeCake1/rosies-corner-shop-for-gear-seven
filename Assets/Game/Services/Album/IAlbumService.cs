using System.Collections.Generic;

namespace Game.Services.Album
{
    /// <summary>
    /// The album of item types shipped at least once and the department completion bonus; owns the <c>album</c>
    /// profile chunk.
    /// </summary>
    public interface IAlbumService
    {
        /// <summary>
        /// Catalog ids of found items, in discovery order (the home showcase shows the latest ones).
        /// </summary>
        IReadOnlyList<int> Found { get; }

        /// <summary>
        /// Items found since the album was last opened; the badge on the Album button.
        /// </summary>
        int NewCount { get; }

        /// <summary>
        /// Returns whether a department's completion bonus was paid.
        /// </summary>
        /// <param name="family">Department id.</param>
        /// <returns>True once every item of the department is found.</returns>
        bool IsFamilyCompleted(string family);

        /// <summary>
        /// Adds a shipped item type to the album; pays the department bonus to the wallet when the department
        /// becomes complete.
        /// </summary>
        /// <param name="itemId">Catalog id.</param>
        /// <returns>The department id whose bonus was just paid, or null.</returns>
        string RegisterFound(int itemId);

        /// <summary>
        /// Clears the "new items" badge (the album was opened).
        /// </summary>
        void MarkSeen();
    }
}
