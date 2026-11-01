using System;
using Game.Services.Save;
using UniRx;

namespace Game.Services.Progress
{
    /// <summary>
    /// The player's persistent progress at runtime: coins, stars, album, boosters, settings and one-time flags.
    /// All features read and change progress through this service; it saves after every change it is told about.
    /// </summary>
    public interface IProgressService
    {
        /// <summary>
        /// The loaded profile. Read freely; change it only through this service's methods, or call
        /// <see cref="Save"/> after changing a field directly.
        /// </summary>
        PlayerProfileData Data { get; }

        /// <summary>
        /// Current coins; emits on every change (HUD and home coin chips bind to it).
        /// </summary>
        IReadOnlyReactiveProperty<int> Coins { get; }

        /// <summary>
        /// Emits after any change (stars, album, boosters, settings); views that show derived state refresh on it.
        /// </summary>
        IObservable<Unit> Changed { get; }

        /// <summary>
        /// Loads the profile from disk; called once by the LoadProgress state.
        /// </summary>
        void Load();

        /// <summary>
        /// Requests a save of the current data (background write; overlapping requests merge).
        /// </summary>
        void Save();

        /// <summary>
        /// Adds coins and saves.
        /// </summary>
        /// <param name="amount">Coins to add; must be non-negative.</param>
        void AddCoins(int amount);

        /// <summary>
        /// Spends coins if the balance allows and saves.
        /// </summary>
        /// <param name="amount">Price in coins.</param>
        /// <returns>False (and nothing changes) when there are not enough coins.</returns>
        bool TrySpend(int amount);

        /// <summary>
        /// Returns how many of an in-level booster the player owns.
        /// </summary>
        /// <param name="id">"pick", "tidy" or "shuffle".</param>
        /// <returns>Owned count.</returns>
        int GetBooster(string id);

        /// <summary>
        /// Changes an owned booster count and saves.
        /// </summary>
        /// <param name="id">Booster id.</param>
        /// <param name="delta">Change; the count never goes below zero.</param>
        void AddBooster(string id, int delta);

        /// <summary>
        /// Returns whether a one-time flag is set (onboarding, milestones).
        /// </summary>
        /// <param name="flag">Flag name, e.g. "freeBasket".</param>
        /// <returns>True once <see cref="SetFlag"/> was called for it.</returns>
        bool HasFlag(string flag);

        /// <summary>
        /// Sets a one-time flag and saves.
        /// </summary>
        /// <param name="flag">Flag name.</param>
        void SetFlag(string flag);

        /// <summary>
        /// Records a completed level, keeping the best stars, and saves.
        /// </summary>
        /// <param name="level">Level number in the active curve.</param>
        /// <param name="stars">Stars earned (1..3).</param>
        void CompleteLevel(int level, int stars);

        /// <summary>
        /// Adds a shipped item type to the album. Pays the department bonus when the department becomes complete.
        /// </summary>
        /// <param name="itemId">Catalog id.</param>
        /// <returns>The department id whose bonus was just paid, or null.</returns>
        string RegisterFound(int itemId);

        /// <summary>
        /// Erases progress but keeps the settings and the reviewer choices (curve, offer variant).
        /// </summary>
        void ResetProgress();

        /// <summary>
        /// Notifies listeners of a change made directly to <see cref="Data"/> and saves.
        /// </summary>
        void NotifyChanged();
    }
}
