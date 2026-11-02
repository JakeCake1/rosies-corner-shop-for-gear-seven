# Localization

All player-facing text comes from **Unity Localization** (`com.unity.localization` 1.5.13) string tables. Code and
configs hold only keys. The project has one locale, English (`en`); a new language is a new locale plus its
tables, with no code change.

## Assets (`Assets/Game/Content/Data/Localization`)

| Asset | What it is |
|---|---|
| `LocalizationSettings.asset` | Active Localization settings (registered in `EditorBuildSettings`). Startup selectors: command line, system language, then `en`. |
| `Locales/English (en).asset` | The English locale. |
| `Tables/UI*` | Collection `UI`: screens, dialogs, toasts, coach tips, boosters, praise words, level tips and curve labels. |
| `Tables/Items*` | Collection `Items`: item names `item.<id>` (162) and department names `family.<id>` (7). |

The package keeps the locale and the tables in its own Addressables groups (`Localization-Locales`,
`Localization-Assets-Shared`, `Localization-String-Tables-English`). Edit the tables in
**Window → Asset Management → Localization Tables**.

## Code

- `ILocalizationService` (`Services/Localization`, project scope) wraps `LocalizationSettings.StringDatabase`.
  `BootstrapState` awaits `InitializeAsync` after Addressables init; it loads both tables for the startup locale.
  After that every lookup is synchronous:
  - `Get(key)`: a `UI` entry as is;
  - `Format(key, args)`: a `UI` entry filled with `string.Format` (`{0}`, `{1}`…). Entries are plain strings, not
    Smart Strings;
  - `GetItemName(id)`, `GetFamilyName(family)`: `Items` entries.
  A missing key returns the key itself and logs a warning.
- Keys used by code are `internal const` fields of one `*Texts` class per feature (`HomeTexts`, `GameplayTexts`,
  `AlbumTexts`, `SettingsTexts`, `ReviewersTexts`, `AdsTexts`, `DialogTexts` in `Services/UI/Dialogs`). Each key's
  summary quotes the English text.
- Views never read the tables. View models and presenters resolve text and hand views ready strings. Static UXML
  labels have a `name` and get their text from the view model. The English `text` left in UXML is only a preview
  for UI Builder.
- Configs store keys: `LevelCurveConfig._labelKey`, `_subtitleKey` (`curve.<id>.*`) and `LevelDefinition._tipKey`
  (`tip.*`, empty for no tip). `ItemDefinition.Name` remains the English identifier the level generator uses for
  lookalike pairs. It is not shown to the player.

## Adding or changing a text

1. Add the entry to the `UI` table (key `feature.snake_case`, English text; `{0}`… for arguments).
2. Add a constant with the same key to the feature's `*Texts` class and use it through `ILocalizationService`.
3. Run the EditMode tests. `LocalizationKeysTests` fails if a constant or config key has no English text, if a
   `UI` or `Items` entry is unused, if an entry is a Smart String, or if an entry doesn't format with arguments.

Keep layouts tolerant of 30–40% longer text (`unity-ui-mvvm-rules.md`).
