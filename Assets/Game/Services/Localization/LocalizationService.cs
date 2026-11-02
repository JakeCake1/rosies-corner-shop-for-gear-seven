using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Game.Services.Localization
{
    /// <summary>
    /// Default <see cref="ILocalizationService"/> on top of <see cref="LocalizationSettings.StringDatabase"/>.
    /// </summary>
    /// <remarks>
    /// The tables are loaded once for the startup locale; the game has no language switch yet, so the service
    /// doesn't follow <c>SelectedLocaleChanged</c>. The string database owns the table handles and releases them
    /// itself, so the service keeps only the table references.
    /// </remarks>
    public sealed class LocalizationService : ILocalizationService
    {
        /// <summary>
        /// Name of the string table collection with screen, dialog, toast and tip texts.
        /// </summary>
        public const string UITable = "UI";

        /// <summary>
        /// Name of the string table collection with item and department names.
        /// </summary>
        public const string ItemsTable = "Items";

        /// <summary>
        /// Key prefix of item names in <see cref="ItemsTable"/>; the catalog id follows (<c>item.42</c>).
        /// </summary>
        public const string ItemKeyPrefix = "item.";

        /// <summary>
        /// Key prefix of department names in <see cref="ItemsTable"/>; the department id follows (<c>family.fresh</c>).
        /// </summary>
        public const string FamilyKeyPrefix = "family.";

        private StringTable _ui;
        private StringTable _items;

        /// <inheritdoc/>
        public async UniTask InitializeAsync(CancellationToken ct)
        {
            await LocalizationSettings.InitializationOperation.ToUniTask(cancellationToken: ct);
            var database = LocalizationSettings.StringDatabase;
            _ui = await database.GetTableAsync(UITable).ToUniTask(cancellationToken: ct);
            _items = await database.GetTableAsync(ItemsTable).ToUniTask(cancellationToken: ct);
        }

        /// <inheritdoc/>
        public string Get(string key) => Lookup(_ui, UITable, key, null);

        /// <inheritdoc/>
        public string Format(string key, params object[] args) => Lookup(_ui, UITable, key, args);

        /// <inheritdoc/>
        public string GetItemName(int itemId) => Lookup(_items, ItemsTable, ItemKeyPrefix + itemId, null);

        /// <inheritdoc/>
        public string GetFamilyName(string family) => Lookup(_items, ItemsTable, FamilyKeyPrefix + family, null);

        private static string Lookup(StringTable table, string tableName, string key, object[] args)
        {
            var entry = table != null ? table.GetEntry(key) : null;
            if (entry == null)
            {
                Debug.LogWarning($"Localization: no entry '{key}' in table '{tableName}'.");
                return key;
            }

            return args == null || args.Length == 0 ? entry.GetLocalizedString() : entry.GetLocalizedString(args);
        }
    }
}
