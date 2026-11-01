# Save system: profile chunks

The player profile is split into **chunks**, one per area of responsibility. Each chunk is owned by the service
that uses its data, is saved to its own file and has its own format version. Rules: `Docs/Code Rules/unity-save-system-rules.md`.

## Chunks

| Key | Owner (interface) | Folder | Fields (JSON) | Kept on reset |
|---|---|---|---|---|
| `wallet` | `IWalletService` | `Services/Wallet` | `coins` | no |
| `levels` | `ILevelProgressService` | `Services/Levels` | `stars`, `failedLv` | no |
| `album` | `IAlbumService` | `Services/Album` | `found`, `albumNew`, `famDone` | no |
| `boosters` | `IBoosterInventory` | `Features/Gameplay/Boosters` | `boost`, `pre`, `got` | no |
| `onboarding` | `IOnboardingService` | `Services/Onboarding` | `flags`, `tips` | no |
| `shop` | `IShopProfile` | `Features/Home` | `shopName` | no |
| `settings` | `ISettingsService` | `Services/Settings` | `sound`, `music`, `haptic` | yes |
| `experiments` | `IExperimentsService` | `Services/Experiments` | `curve`, `offer` | yes |

Owners live in `Services/` when a service or several features read the data (assemblies forbid `Services` →
`Features` references), and in the feature folder when only that feature does.

## How it fits together

- `IProfileChunk` is the shared interface; owners derive from `ProfileChunk<TData>` (typed data, `Restore`,
  `MarkChanged`). Data changes only through the owner's methods.
- `ProfileService` (`IProfileService`) receives every registered chunk from VContainer as `IReadOnlyList<IProfileChunk>`.
  It loads them in the LoadProgress state, saves **only the chunk that changed** after its owner calls `MarkChanged`,
  emits `Changed` for views that combine several chunks (home screen), saves everything on app pause and resets
  the chunks not kept on reset.
- `SaveService` (`ISaveService`) handles one chunk at a time: JSON (Newtonsoft, invariant culture), a `version`
  field next to the chunk's fields, migrations, merged background writes. `FileSaveStorage` writes atomically with
  a `.bak` copy of the previous version.

## Files

`Application.persistentDataPath/profile_<key>.json` (+ `.bak`, `.tmp` during a write; a broken file is moved to
`profile_<key>.corrupt_<utc>.json`). Example `profile_wallet.json`:

```json
{
  "version": 1,
  "coins": 150
}
```

Loading a chunk: main file → backup → new player's defaults. A broken or unreadable chunk falls back on its own;
the other chunks are not affected. A chunk written by a newer build is refused (moved aside) instead of being
overwritten.

## Migrations

- **Per chunk:** bump `Version` on the owner and handle the old version in `Migrate(JObject data, int fromVersion)`;
  add a sample of the old format to `Tests/EditMode/Services/Save`.
- **Single-file profile (builds before EVG-12):** when no chunk file exists, `profile.json` is read and every chunk
  takes its own fields from it (chunk field names match the old ones). The chunks are written immediately and
  `profile.json` is deleted only after all of them were written.

## Adding a chunk

1. A `[Preserve]` data class with `[JsonProperty]` fields and safe defaults (don't use the field name `version`).
2. An owner `sealed class X : ProfileChunk<XData>, IX` with a unique, never-renamed `Key`; call `MarkChanged()`
   after each change; override `IsKeptOnReset` if "reset progress" must keep it.
3. Register it in `ProjectLifetimeScope.RegisterProfileChunks` with `AsImplementedInterfaces()`.
4. EditMode tests for the owner's logic.
