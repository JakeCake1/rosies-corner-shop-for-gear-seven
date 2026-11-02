using System.Threading;
using Cysharp.Threading.Tasks;

namespace Game.Services.Localization
{
    /// <summary>
    /// Player-facing text from the Unity Localization string tables: the <c>UI</c> table (screens, dialogs, toasts,
    /// tips, level curve texts) and the <c>Items</c> table (item and department names).
    /// </summary>
    /// <remarks>
    /// Lookups are synchronous and valid only after <see cref="InitializeAsync"/> completed (the bootstrap state
    /// awaits it before any screen opens). A missing entry returns the key itself and logs a warning, so a typo
    /// shows up on screen instead of crashing.
    /// </remarks>
    public interface ILocalizationService
    {
        /// <summary>
        /// Initializes Unity Localization (selects the startup locale) and loads both string tables for that
        /// locale. Called once by the bootstrap state, after Addressables is initialized.
        /// </summary>
        /// <param name="ct">Cancelled on app shutdown.</param>
        /// <returns>Completes when every lookup can be answered synchronously.</returns>
        /// <exception cref="System.OperationCanceledException">Initialization was cancelled.</exception>
        UniTask InitializeAsync(CancellationToken ct);

        /// <summary>
        /// Returns the text of a <c>UI</c> table entry as is (no formatting, braces are kept).
        /// </summary>
        /// <param name="key">Entry key, one of the feature <c>*Texts</c> constants or a key stored in a config.</param>
        /// <returns>The localized text, or <paramref name="key"/> when the entry is missing.</returns>
        string Get(string key);

        /// <summary>
        /// Returns the text of a <c>UI</c> table entry with its <c>{0}</c>, <c>{1}</c>… placeholders filled
        /// (<see cref="string.Format(string, object[])"/> rules).
        /// </summary>
        /// <param name="key">Entry key whose text has as many placeholders as <paramref name="args"/>.</param>
        /// <param name="args">Values for the placeholders, in order; numbers use the locale's formatting.</param>
        /// <returns>The formatted text, or <paramref name="key"/> when the entry is missing.</returns>
        string Format(string key, params object[] args);

        /// <summary>
        /// Returns the display name of a catalog item (entry <c>item.&lt;id&gt;</c> of the <c>Items</c> table).
        /// </summary>
        /// <param name="itemId">Catalog id (<see cref="Content.ItemDefinition.Id"/>).</param>
        /// <returns>The localized item name.</returns>
        string GetItemName(int itemId);

        /// <summary>
        /// Returns the display name of a department (entry <c>family.&lt;id&gt;</c> of the <c>Items</c> table).
        /// </summary>
        /// <param name="family">Department id (<see cref="Content.ItemDefinition.Family"/>), e.g. "fresh".</param>
        /// <returns>The localized department name ("Fresh produce").</returns>
        string GetFamilyName(string family);
    }
}
