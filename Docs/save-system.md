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
  emits `Changed` for views that combine several chunks (home screen), saves everything on app pause, focus loss
  and quit, and resets the chunks not kept on reset. Saves are never cancelled, not even when the service is
  disposed at app shutdown.
- `SaveService` (`ISaveService`) handles one chunk at a time: JSON (Newtonsoft, invariant culture), a `version`
  field next to the chunk's fields, migrations, merged background writes. `FileSaveStorage` writes atomically with
  a `.bak` copy of the previous version.

## When writes happen

- **After a change:** `SaveAsync` serializes on the main thread and writes on the thread pool. Every call gets its
  own pool task; whichever task holds the write lock (a monitor) writes everything queued so far, oldest first, so
  chunks reach the disk in the order they were saved (the wallet before the album that marks its bonus done).
  Nothing waits for the main thread, so queued writes still finish while the player loop is stopped. Overlapping
  saves of one chunk are merged: only the latest snapshot is written.
- **App pause, focus loss, quit:** `IProfileService.SaveAll` calls `ISaveService.Save` for every chunk. It writes on
  the calling thread and returns after the data is on disk, because the OS may kill a paused process at any moment.
  It replaces a queued background write of the same chunk and waits for a running one. A chunk that throws is
  logged and skipped; the others are still saved.
- **Write errors** (`IOException`, `UnauthorizedAccessException`) are logged and the previous file stays intact.

## Files

`Application.persistentDataPath/profile_<key>.json` (+ `.bak`, `.tmp` during a write; a broken file is moved to
`profile_<key>.corrupt_<utc>.json`). Example `profile_wallet.json`:

```json
{
  "version": 1,
  "coins": 150
}
```

Loading a chunk: main file → backup → new player's defaults. A broken chunk falls back on its own; the other chunks
are not affected. A chunk written by a newer build is refused (moved aside) instead of being overwritten; an empty
file counts as broken and is moved aside too. If moving it aside fails, the chunk is **read-only for the session**
so the first save can't overwrite it.

A file that **exists but can't be read** (`IOException`, `UnauthorizedAccessException`) is not the same as a missing
file: the data may be intact. The chunk starts with defaults but is **read-only for the session**: `SaveAsync` and
`Save` skip it (one warning per chunk), so the next start can still read the stored data. Restoring from the backup
does not lift this, and reset progress is not persisted for such a chunk either.

## Migrations

- **Per chunk:** bump `Version` on the owner and handle the old version in `Migrate(JObject data, int fromVersion)`;
  add a sample of the old format to `Tests/EditMode/Services/Save`.
- **Single-file profile (builds before the chunked profile):** when no chunk file exists, `profile.json` is read and every chunk
  takes its own fields from it (chunk field names match the old ones). The chunks are written immediately and
  `profile.json` is deleted only after all of them were written. A chunk whose fields can't be converted (wrong type)
  starts with defaults and the others are migrated as usual; `profile.json` is then moved aside (`profile.corrupt_<time>.json`) for investigation, so it can't refill a chunk that
  goes missing later. A chunk that is read-only for the session starts with defaults instead of old data. If
  `profile.json` itself can't be read, the chunks are read-only for the session, like any unreadable file.

## Adding a chunk

1. A `[Preserve]` data class with `[JsonProperty]` fields and safe defaults (don't use the field name `version`).
2. An owner `sealed class X : ProfileChunk<XData>, IX` with a unique, never-renamed `Key`; call `MarkChanged()`
   after each change; override `IsKeptOnReset` if "reset progress" must keep it.
3. Register it in `ProjectLifetimeScope.RegisterProfileChunks` with `AsImplementedInterfaces()`.
4. EditMode tests for the owner's logic.
