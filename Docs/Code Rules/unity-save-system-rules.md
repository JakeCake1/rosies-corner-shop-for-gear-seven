# Unity Save System Rules: Player Profile Persistence

Format: each rule is `WHEN <trigger> → DO <action>`. Apply a rule only when its trigger matches the current code or task. Prefer the simplest solution that meets the project's needs (YAGNI). Context: Unity, C#, saving a player profile to a local file.

---

## Data Model

- **WHEN** designing what gets saved → **DO** create a separate plain data class (DTO), e.g. `PlayerProfileData`, with only serializable fields. Never serialize `MonoBehaviour`, `GameObject`, `ScriptableObject` or other `UnityEngine.Object` instances.
- **WHEN** save data must reference assets (items, skins, levels) → **DO** store stable string IDs (`"sword_iron"`), not asset references, instance IDs or list indices. Resolve IDs to assets through a registry/database at load time.
- **WHEN** runtime game state and save data start to mix → **DO** keep them separate: runtime models hold logic, the DTO holds data. Map between them explicitly (`ToData()` / `FromData()`).
- **WHEN** a field holds a derived value (total stats, cached counts) → **DO NOT** save it; recompute it after loading.
- **WHEN** saving dates and times → **DO** store UTC as ticks (`long`) or ISO 8601 string. Never save local time.
- **WHEN** saving numbers as text → **DO** serialize with `CultureInfo.InvariantCulture` so `1.5` doesn't become `1,5` on some locales.
- **WHEN** fields are added to the DTO → **DO** give every field a safe default so old saves without that field still load correctly.

---

## Serialization Format

- **WHEN** choosing a format for a typical profile → **DO** use JSON: readable, debuggable, easy to migrate.
- **WHEN** the data contains `Dictionary`, polymorphic types, properties or nullable types → **DO** use Newtonsoft Json (`com.unity.nuget.newtonsoft-json`); `JsonUtility` doesn't support them.
- **WHEN** the data is simple and flat and performance matters → **DO** `JsonUtility` is acceptable (fast, no dependencies), but only public fields or `[SerializeField]` fields of `[Serializable]` classes.
- **WHEN** saves are large (MBs) or saved very often → **DO** consider a binary format (MessagePack-CSharp, MemoryPack). Keep a debug option to dump the save as JSON.
- **WHEN** using Newtonsoft with polymorphism → **DO NOT** use `TypeNameHandling.All`/`Auto` on untrusted data without a `SerializationBinder` that whitelists allowed types; it's a deserialization security risk.
- **WHEN** building with IL2CPP and code stripping → **DO** protect serialized types from stripping (`[Preserve]`, `link.xml`) and test loading in a real build, not just the Editor.
- **NEVER** use `BinaryFormatter`: it's insecure and obsolete.

---

## Storage Location

- **WHEN** choosing where to write the file → **DO** use `Application.persistentDataPath`. Never write to `Application.dataPath` or `StreamingAssets` (read-only on most platforms).
- **WHEN** building the file path → **DO** use `Path.Combine(Application.persistentDataPath, "profile.json")`, never string concatenation with `/` or `\`.
- **WHEN** storing a full profile → **DO NOT** use `PlayerPrefs`: it has size limits, no structure, and on Windows it lives in the registry. Use `PlayerPrefs` only for small device settings (volume, language, graphics).
- **WHEN** the game supports several profiles or slots → **DO** use one file per slot (`profile_0.json`, `profile_1.json`) plus a small index/metadata file for the slot list.
- **WHEN** targeting WebGL → **DO** call `FS.syncfs` via a `.jslib` plugin after writing (or use a library that does it), or the data may not persist to IndexedDB. In recent Unity versions check whether auto-sync is enabled.
- **WHEN** targeting consoles → **DO** use the platform's save API (required by certification), behind the same `ISaveStorage` interface.

---

## Reliable Writes

- **WHEN** writing the save file → **DO** write atomically: write to a temp file (`profile.json.tmp`), flush, then replace the main file. A crash or power loss mid-write must never leave a half-written main file.
- **WHEN** replacing the main file → **DO** keep a backup of the previous version (`profile.json.bak`) via `File.Replace(temp, main, backup)`. If `File.Replace` is not supported on a platform, fall back to delete + `File.Move` and keep the backup manually.
- **WHEN** loading → **DO** try the main file; if it's missing, empty, fails to parse or fails the checksum, try the backup; if both fail, create a new profile and log the error (and report it to analytics/crash reporting).
- **WHEN** detecting corruption or tampering is important → **DO** store a checksum/hash (e.g. SHA-256 or CRC32) of the payload alongside it and verify it on load.
- **WHEN** a load fails → **DO NOT** immediately overwrite the broken file with a fresh profile. Move it aside (`profile.corrupt_<timestamp>.json`) so data can be recovered or investigated.
- **WHEN** the device might be out of space → **DO** catch `IOException` on write, keep the previous save intact, and notify the player if appropriate.

---

## Versioning and Migrations

- **WHEN** creating the save format → **DO** include a `version` integer field in the root of the save from day one.
- **WHEN** the data structure changes (field renamed, type changed, split) → **DO** increment the version and add a migration step `vN → vN+1`. Run migrations in order on load until the data reaches the current version.
- **WHEN** writing migrations → **DO** operate on a raw representation (`JObject` with Newtonsoft) so old fields can be read even after they're removed from the DTO.
- **WHEN** a save has a version newer than the game supports (player downgraded or cloud save from a newer build) → **DO NOT** load and overwrite it; show a message or fall back safely.
- **WHEN** changing the format → **DO** keep sample saves of every old version in the test folder and test that each one migrates and loads.

---

## When to Save

- **WHEN** targeting mobile → **DO** save in `OnApplicationPause(true)` and `OnApplicationFocus(false)`. `OnApplicationQuit` is not reliably called on iOS/Android when the OS kills the app.
- **WHEN** important progress happens (purchase, level completed, reward received) → **DO** save immediately after it, not only at exit.
- **WHEN** data changes frequently (currency ticks, stats) → **DO** use a dirty flag plus debounce/autosave interval (e.g. every 10–30 s if dirty) instead of writing on every change.
- **WHEN** a save is requested while another save is in progress → **DO** serialize writes through one queue/lock; never let two writes touch the same file at once. Coalesce pending requests into one write with the latest data.
- **WHEN** in-app purchases are involved → **DO** save after granting the purchase and before confirming/consuming it with the store, so a crash can't lose a paid item.

---

## Performance and Threading

- **WHEN** serialization or file I/O causes frame hitches → **DO** take a snapshot of the data on the main thread (`ToData()`), then serialize and write on a background thread (`Task.Run` / `Awaitable.BackgroundThreadAsync()` in Unity 6 / UniTask).
- **WHEN** working on a background thread → **DO NOT** touch any Unity API (`Application.persistentDataPath`, `Debug.Log` is fine, but most others aren't). Cache the path on the main thread at startup.
- **WHEN** loading at startup → **DO** load the profile during the Bootstrap/Loading state, before gameplay systems need it; gameplay systems receive the loaded data, they never read files themselves.
- **WHEN** saves grow large → **DO** split rarely changing and frequently changing data into separate files, or compress (GZip) the payload.

---

## Architecture

- **WHEN** structuring the code → **DO** split responsibilities: `ISaveStorage` (raw read/write bytes or strings per key), `ISaveSerializer` (object ↔ string/bytes), `SaveService` / `ProgressService` (owns the current profile, versioning, autosave).
- **WHEN** many systems contribute to the profile → **DO** use reader/writer interfaces (`IProgressReader.Load(PlayerProfileData)`, `IProgressWriter.Save(PlayerProfileData)`) and let the service iterate registered systems, instead of the save service knowing about every system.
- **WHEN** systems need the save service → **DO** inject it via DI; don't use a static singleton.
- **WHEN** the platform or storage changes (local file, cloud, console API, PlayerPrefs for WebGL) → **DO** add a new `ISaveStorage` implementation; game code doesn't change.
- **WHEN** writing tests → **DO** use an in-memory `ISaveStorage` so save/load/migration logic is testable without the file system.

---

## Security and Anti-Cheat

- **WHEN** deciding on protection → **DO** accept that local saves can always be edited by a determined player. Protect only to the level the game needs.
- **WHEN** casual tampering must be discouraged (single-player) → **DO** add an HMAC signature of the payload and optionally light encryption (AES). Reject or flag saves with an invalid signature.
- **WHEN** the economy is competitive, multiplayer or involves real money → **DO** keep authoritative data (currency, purchases, rankings) on a server. The local save is only a cache.
- **WHEN** storing encryption keys in the client → **DO NOT** assume they are secret; they can be extracted. Never store passwords, tokens or personal data in plain text in the save.

---

## Cloud Sync

- **WHEN** adding cloud saves (Unity Cloud Save, Google Play Games, iCloud, Steam Cloud) → **DO** keep the local file as the primary copy and sync it, rather than replacing local saving.
- **WHEN** local and cloud saves differ → **DO** resolve conflicts with metadata (`savedAtUtc`, `version`, progress level, device ID): automatically pick the clearly further progress, or ask the player when it's ambiguous.
- **WHEN** a cloud save arrives → **DO** run it through the same validation and migration pipeline as a local save.

---

## Privacy and Player Control

- **WHEN** the player requests data deletion (GDPR/CCPA, "reset progress") → **DO** delete the main file, backups, temp files and cloud copies.
- **WHEN** saves may contain personal data → **DO** store only what's needed and document it in the privacy policy.

---

## Reference Skeleton

```csharp
[Serializable]
public sealed class PlayerProfileData
{
    public int version = SaveConstants.CurrentVersion;
    public long savedAtUtcTicks;
    public string playerName = "Player";
    public int softCurrency;
    public int level = 1;
    public List<string> unlockedItemIds = new();
}

public static class SaveConstants
{
    public const int CurrentVersion = 1;
}

public interface ISaveStorage
{
    bool TryRead(string key, out string content);
    void Write(string key, string content);
    void Delete(string key);
}

public sealed class FileSaveStorage : ISaveStorage
{
    private readonly string _root;

    // Construct on the main thread: Application.persistentDataPath is a Unity API.
    public FileSaveStorage() => _root = Application.persistentDataPath;

    private string PathFor(string key) => Path.Combine(_root, key + ".json");

    public bool TryRead(string key, out string content)
    {
        string main = PathFor(key);
        if (TryReadFile(main, out content)) return true;
        return TryReadFile(main + ".bak", out content);
    }

    public void Write(string key, string content)
    {
        string main = PathFor(key);
        string temp = main + ".tmp";
        string backup = main + ".bak";

        using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.Write(content);
            writer.Flush();
            stream.Flush(true);
        }

        if (File.Exists(main))
        {
            try
            {
                File.Replace(temp, main, backup);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(main, backup, overwrite: true);
                File.Delete(main);
                File.Move(temp, main);
            }
        }
        else
        {
            File.Move(temp, main);
        }
    }

    public void Delete(string key)
    {
        string main = PathFor(key);
        foreach (var path in new[] { main, main + ".bak", main + ".tmp" })
            if (File.Exists(path)) File.Delete(path);
    }

    private static bool TryReadFile(string path, out string content)
    {
        content = null;
        if (!File.Exists(path)) return false;
        content = File.ReadAllText(path, Encoding.UTF8);
        return !string.IsNullOrWhiteSpace(content);
    }
}

public sealed class ProfileService
{
    private const string Key = "profile";
    private readonly ISaveStorage _storage;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public PlayerProfileData Profile { get; private set; }
    public bool IsDirty { get; private set; }

    public ProfileService(ISaveStorage storage) => _storage = storage;

    public void Load()
    {
        Profile = _storage.TryRead(Key, out var json) && TryDeserialize(json, out var data)
            ? Migrate(data)
            : new PlayerProfileData();
    }

    public void MarkDirty() => IsDirty = true;

    public async Task SaveAsync()
    {
        // Snapshot on the main thread, write on a background thread.
        Profile.savedAtUtcTicks = DateTime.UtcNow.Ticks;
        string json = JsonConvert.SerializeObject(Profile);
        IsDirty = false;

        await _writeLock.WaitAsync();
        try { await Task.Run(() => _storage.Write(Key, json)); }
        finally { _writeLock.Release(); }
    }

    private static bool TryDeserialize(string json, out PlayerProfileData data)
    {
        try
        {
            data = JsonConvert.DeserializeObject<PlayerProfileData>(json);
            return data != null;
        }
        catch (JsonException)
        {
            data = null;
            return false;
        }
    }

    private static PlayerProfileData Migrate(PlayerProfileData data)
    {
        // Apply vN -> vN+1 steps in order here as the format evolves.
        data.version = SaveConstants.CurrentVersion;
        return data;
    }
}
```

Note: the skeleton loads the backup automatically if the main file is missing or empty. For full corruption handling, verify a checksum and fall back to the backup when parsing fails, as described in "Reliable Writes".
