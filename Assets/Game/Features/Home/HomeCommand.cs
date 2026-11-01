namespace Game.Features.Home
{
    /// <summary>
    /// Navigation requests from the home screen.
    /// </summary>
    public enum HomeCommand
    {
        /// <summary>Start the next level.</summary>
        Play,

        /// <summary>Open the album.</summary>
        Album,

        /// <summary>The album is still locked.</summary>
        AlbumLocked,

        /// <summary>Open settings.</summary>
        Settings,

        /// <summary>Open the reviewers panel.</summary>
        Reviewers,
    }
}
