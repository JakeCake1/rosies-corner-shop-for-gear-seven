namespace Game.Features.Gameplay.Session
{
    /// <summary>
    /// Result of <see cref="LevelSession.Undo"/>.
    /// </summary>
    public enum UndoResult
    {
        /// <summary>The last move was taken back.</summary>
        Done,

        /// <summary>No move to undo yet ("Nothing to undo yet").</summary>
        NothingToUndo,

        /// <summary>Out of undos; the buy offer should open.</summary>
        NoUndosLeft,
    }
}
