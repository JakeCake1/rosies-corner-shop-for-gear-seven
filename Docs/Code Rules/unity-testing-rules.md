# Unity Testing Rules: Logic and Configs

Format: each rule is `WHEN <trigger> → DO <action>`. Tools: Unity Test Framework (NUnit), EditMode and PlayMode tests. Stack: VContainer, UniTask, UniRx, DOTween, Addressables. Don't add mocking or coverage packages (NSubstitute, Moq, Code Coverage) without asking; if the project already has one, use it.

---

## What to test

- **WHEN** adding or changing logic in `Features/` or `Services/` (rules, calculations, state transitions, ViewModels, services, save migrations) → **DO** add or update EditMode tests in the same change.
- **WHEN** adding or changing a `ScriptableObject` config type → **DO** add validation for it and a config test that runs it over every asset of that type (see **Config tests**).
- **WHEN** fixing a bug in logic → **DO** first write a test that reproduces it and fails, then fix the code so it passes.
- **WHEN** code is a thin View (UI Toolkit `ElementView`), a Unity API wrapper, a trivial property or a private method → **DO NOT** test it directly; test behavior through the public API of the class that uses it.
- **WHEN** behavior depends on Unity lifecycle, physics, scene loading or real `MonoBehaviour` interaction (pool integration, collisions) → **DO** use a PlayMode test, kept small; everything else is EditMode.

---

## Making logic testable

- **WHEN** writing game logic → **DO** keep it in plain C# classes with dependencies injected through the constructor (VContainer), so tests create them with `new` and fakes. Logic inside `MonoBehaviour` is not unit-testable.
- **WHEN** logic needs time, randomness or the current date → **DO** inject an interface (`ITimeProvider`, `IRandomService`) instead of calling `Time.time`, `Random.Range` or `DateTime.UtcNow` directly; tests pass a controllable fake.
- **WHEN** logic needs files, network, Addressables or PlayerPrefs → **DO** depend on the service interface (`ISaveStorage`, `IAssetProvider`); tests use an in-memory fake.
- **WHEN** a ViewModel uses throttling, delays or tweens → **DO** keep time-based behavior in the View (or behind an injected service) so ViewModel tests stay synchronous and deterministic.

---

## Unit tests for logic

### Structure and naming

- **WHEN** creating a test class → **DO** name it `<ClassUnderTest>Tests` and mirror the production folder (`Tests/EditMode/Features/Shop/ShopViewModelTests.cs`).
- **WHEN** naming a test method → **DO** use `Method_WhenCondition_ExpectedResult` (`TrySpend_WhenNotEnoughCoins_KeepsBalance`). The name must describe the behavior without reading the body.
- **WHEN** writing a test body → **DO** follow Arrange / Act / Assert, separated by blank lines; one behavior per test.
- **WHEN** a test needs loops, `if` or complex setup → **DO** move the setup into a builder or helper (`ProfileBuilder`, `FakeWallet`) or split the test; tests contain no branching logic.
- **WHEN** several inputs check the same behavior → **DO** use `[TestCase(...)]` or `[TestCaseSource(...)]` instead of copy-pasted tests.

### Assertions

- **WHEN** asserting → **DO** use the NUnit constraint model: `Assert.That(actual, Is.EqualTo(expected))`, `Is.True`, `Is.Empty`, `Has.Count.EqualTo(3)`, `Throws.TypeOf<ArgumentException>()`.
- **WHEN** comparing floats or vectors → **DO** use a tolerance: `Is.EqualTo(0.5f).Within(1e-4f)`.
- **WHEN** an assertion can fail with an unclear message → **DO** add a message with the context (`$"{config.name}: price is {price}"`).
- **WHEN** code under test logs an expected error → **DO** declare it with `LogAssert.Expect(LogType.Error, ...)`; otherwise an unexpected error log fails the test.

### Isolation and determinism

- **WHEN** writing any unit test → **DO** keep it independent (no shared mutable state between tests, no order dependency), deterministic and fast (EditMode tests in milliseconds).
- **WHEN** tests need fresh objects → **DO** create them in `[SetUp]` and dispose them in `[TearDown]` (ViewModels, `CompositeDisposable`, `CancellationTokenSource`, created GameObjects and `ScriptableObject` instances via `Object.DestroyImmediate`).
- **WHEN** a test would touch the real file system, network, `PlayerPrefs`, real time or `Thread.Sleep` → **DO NOT**; use fakes and a controllable time provider.
- **WHEN** a dependency is needed → **DO** write a small hand-written fake (`internal sealed class FakeWallet : IWallet`) in the test assembly; prefer fakes with simple recorded state over interaction mocks.
- **WHEN** testing a class → **DO** construct it directly with fakes; **DO NOT** build the VContainer container in unit tests.

### UniRx

- **WHEN** testing reactive output → **DO** subscribe, collect values into a `List<T>`, act, then assert the whole sequence (`Is.EqualTo(new[] { false, true })`). Dispose the subscription in the test or `[TearDown]`.
- **WHEN** a `ReactiveProperty` is asserted → **DO** remember it emits its current value on subscribe; include it in the expected sequence.

### UniTask

- **WHEN** testing async code → **DO** use `[UnityTest]` returning `IEnumerator` with `UniTask.ToCoroutine(async () => { ... })`.
- **WHEN** testing cancellation → **DO** pass a `CancellationTokenSource`, cancel it, and assert `Throws` / `ThrowsAsync` of `OperationCanceledException` (or the `SuppressCancellationThrow` result).
- **WHEN** async code waits for time → **DO** drive it with an injected time provider or a zero duration; don't wait real seconds in EditMode tests.

### DOTween and Addressables

- **WHEN** logic triggers tweens or loads assets → **DO** test the logic with fake services; don't run real tweens or Addressables loads in unit tests.

### Composition (VContainer)

- **WHEN** registrations are complex → **DO** keep them in `IInstaller` classes and add one EditMode test per scope that builds a `ContainerBuilder`, installs them with fake platform services, calls `Build()` and resolves the entry points and states. This catches missing registrations before Play mode. Verify the VContainer API against the installed version.

---

## Config tests

### Validation inside the config

- **WHEN** creating a `ScriptableObject` config → **DO** implement `IValidatableConfig.Validate(List<string> errors)` in the config itself; tests, `OnValidate` and editor tools all call the same method (DRY).
- **WHEN** writing `Validate` → **DO** check, where they apply:
  - required references are assigned (prefabs, sprites, nested configs);
  - IDs are non-empty and well-formed (`snake_case`, no spaces);
  - numbers are in range (price > 0, cooldown >= 0, chance in 0..1, min <= max);
  - lists are non-empty and have no duplicate entries;
  - `AssetReference` fields have a valid key (`RuntimeKeyIsValid()`);
  - enums aren't left at a `None`/default value where a choice is required.
- **WHEN** an error is added → **DO** include the asset name, field and actual value (`"{name}: Price must be positive, got {price}."`).

### Tests over all assets

- **WHEN** testing configs → **DO** load every asset of the type in an EditMode test with `AssetDatabase.FindAssets("t:<Type>")` and run validation via `[TestCaseSource]`, so each asset is a separate test case named after it.
- **WHEN** a config type must have assets → **DO** add a test that at least one asset exists, so a moved or renamed folder doesn't make the suite pass silently.
- **WHEN** configs reference each other or IDs must be globally unique → **DO** add cross-asset tests: IDs unique across all assets of a type, referenced IDs exist (a shop item's `itemId` exists in the item database), level/reward tables are complete.
- **WHEN** an `AssetReference` must be loadable at runtime → **DO** check in the Editor test that its GUID is an Addressables entry (`AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid) != null`).
- **WHEN** game balance has invariants (rewards grow with level, upgrade cost increases, drop chances sum to 1) → **DO** encode them as config tests, not as comments.
- **WHEN** save data uses default values or migrations → **DO** test that a new `PlayerProfileData` has valid defaults and that every stored sample of an old save version migrates and loads (see the save system rules).

### Layout tests (UI Toolkit)

- **WHEN** adding or changing a window, a UXML layout or the names a view queries → **DO** keep an EditMode test that, for every `WindowView<T>` subclass, finds the Addressables entry `ui/<WindowType>` and loads it as a `VisualTreeAsset`; a missing or renamed layout then fails in tests instead of at runtime.
- **WHEN** a view queries elements by name constants → **DO** test that each queried name exists in its layout: instantiate the `VisualTreeAsset` (`CloneTree()`), attach the view and assert that no `Q<T>(name)` returned `null` (or read the `...Name` constants via reflection and query each one).
- **WHEN** a USS file or a UXML layout references assets (`url("project://...")`, `<Style src=...>`, templates) → **DO** rely on the asset import (no missing-asset warnings in the console after a refresh) and fix broken paths immediately; layouts are not generated, so nothing regenerates them.
- **WHEN** a layout must match a mockup → **DO** verify it manually in Play mode at the 390×844 reference resolution against the mockup screenshot; pixel comparisons are not automated tests.

---

## Test assemblies and location

- **WHEN** creating tests → **DO** put them in `Tests/EditMode/` and `Tests/PlayMode/`, mirroring the production folders (`Tests/EditMode/Features/Shop/`, `Tests/EditMode/Services/Save/`).
- **WHEN** creating the EditMode test `.asmdef` → **DO** set it to the Editor platform only, mark it as a test assembly (references `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll` via override references, define constraint `UNITY_INCLUDE_TESTS`), and reference the production assemblies under test plus `UniTask`, `UniRx`, `VContainer` and `Unity.Addressables.Editor` if config tests need it.
- **WHEN** creating the PlayMode test `.asmdef` → **DO** use the same setup without restricting it to the Editor platform.
- **WHEN** a test needs `internal` members → **DO** add `[assembly: InternalsVisibleTo("Game.Tests.EditMode")]` to the production assembly; don't make members `public` only for tests.
- **WHEN** a test needs to set private `[SerializeField]` fields of a config → **DO** use a test builder that writes them via `SerializedObject` in EditMode, or an `internal` init method; don't add public setters for tests.

---

## PlayMode tests

- **WHEN** writing a PlayMode test → **DO** create only the objects it needs in code (or load a minimal test scene), and destroy them in `[UnityTearDown]`.
- **WHEN** a PlayMode test waits → **DO** wait for a condition with a timeout (`UniTask.WaitUntil` + `CancellationTokenSource(TimeSpan)`), not for a fixed number of frames or seconds.
- **WHEN** a PlayMode test is slow or flaky → **DO** move the logic it checks into an EditMode test and keep the PlayMode test for the integration only.

---

## Running tests

- **WHEN** finishing a change → **DO** run EditMode tests (Test Runner window or batch mode) and make sure all pass before reporting the task as done.
- **WHEN** running in CI or from the command line → **DO** use batch mode: `Unity -batchmode -projectPath <path> -runTests -testPlatform EditMode -testResults <file>.xml` (and `PlayMode` separately).
- **WHEN** configs must never ship broken → **DO** also run config validation before builds (an `IPreprocessBuildWithReport` that calls the same `Validate` methods and fails the build on errors).

---

## Code style in tests

- **WHEN** writing tests → **DO** follow the code style rules, with one exception: test classes and test methods don't need XML doc comments; the test name is the documentation. Fakes and builders shared across many tests get a short `<summary>`.

---

## Reference Skeleton

```csharp
// ---------- Production: config with built-in validation ----------
/// <summary>
/// Implemented by configs that can check their own data; used by config tests, OnValidate and build validation.
/// </summary>
public interface IValidatableConfig
{
    /// <summary>
    /// Adds a message for every invalid value; leaves the list unchanged when the config is valid.
    /// </summary>
    /// <param name="errors">Collects errors across many configs; never cleared here.</param>
    void Validate(List<string> errors);
}

/// <summary>
/// Shop entry shown in the shop window; looked up by <see cref="Id"/> from purchases and saves.
/// </summary>
[CreateAssetMenu(menuName = "Game/Shop/Shop Item Config")]
public sealed class ShopItemConfig : ScriptableObject, IValidatableConfig
{
    [SerializeField] private AssetReferenceSprite _icon;
    [SerializeField] private string _id;
    [SerializeField] private int _price;

    /// <summary>Icon loaded through Addressables when the shop window opens.</summary>
    public AssetReferenceSprite Icon => _icon;

    /// <summary>Stable key stored in saves; must stay unchanged after release.</summary>
    public string Id => _id;

    /// <summary>Price in soft currency; always positive.</summary>
    public int Price => _price;

    /// <inheritdoc/>
    public void Validate(List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(_id)) errors.Add($"{name}: Id is empty.");
        if (_price <= 0) errors.Add($"{name}: Price must be positive, got {_price}.");
        if (_icon == null || !_icon.RuntimeKeyIsValid()) errors.Add($"{name}: Icon reference is missing.");
    }
}

// ---------- EditMode: config tests ----------
public sealed class ShopItemConfigTests
{
    private static IEnumerable<ShopItemConfig> AllConfigs() =>
        AssetDatabase.FindAssets("t:" + nameof(ShopItemConfig))
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<ShopItemConfig>);

    [Test]
    public void Assets_Exist()
    {
        Assert.That(AllConfigs(), Is.Not.Empty);
    }

    [TestCaseSource(nameof(AllConfigs))]
    public void Validate_ReturnsNoErrors(ShopItemConfig config)
    {
        var errors = new List<string>();

        config.Validate(errors);

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    [TestCaseSource(nameof(AllConfigs))]
    public void Icon_IsAddressable(ShopItemConfig config)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;

        var entry = settings.FindAssetEntry(config.Icon.AssetGUID);

        Assert.That(entry, Is.Not.Null, $"{config.name}: icon is not marked as Addressable.");
    }

    [Test]
    public void Ids_AreUnique()
    {
        var duplicates = AllConfigs()
            .GroupBy(config => config.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.That(duplicates, Is.Empty);
    }
}

// ---------- EditMode: logic tests ----------
public sealed class WalletTests
{
    private Wallet _wallet;

    [SetUp]
    public void SetUp()
    {
        _wallet = new Wallet(initialCoins: 100);
    }

    [Test]
    public void TrySpend_WhenEnoughCoins_DecreasesBalance()
    {
        bool spent = _wallet.TrySpend(30);

        Assert.That(spent, Is.True);
        Assert.That(_wallet.Coins.Value, Is.EqualTo(70));
    }

    [TestCase(101)]
    [TestCase(1000)]
    public void TrySpend_WhenNotEnoughCoins_KeepsBalance(int amount)
    {
        bool spent = _wallet.TrySpend(amount);

        Assert.That(spent, Is.False);
        Assert.That(_wallet.Coins.Value, Is.EqualTo(100));
    }
}

// ---------- EditMode: ViewModel test with a fake and UniRx ----------
public sealed class ShopViewModelTests
{
    private FakeWallet _wallet;
    private ShopViewModel _viewModel;

    [SetUp]
    public void SetUp()
    {
        _wallet = new FakeWallet(coins: 50);
        _viewModel = new ShopViewModel(_wallet);
    }

    [TearDown]
    public void TearDown()
    {
        _viewModel.Dispose();
    }

    [Test]
    public void CanBuy_WhenCoinsReachPrice_BecomesTrue()
    {
        var values = new List<bool>();
        using (_viewModel.CanBuy.Subscribe(values.Add))
        {
            _wallet.SetCoins(100);
        }

        Assert.That(values, Is.EqualTo(new[] { false, true }));
    }
}

/// <summary>
/// In-memory wallet for tests; lets a test set the balance directly.
/// </summary>
internal sealed class FakeWallet : IWallet
{
    private readonly ReactiveProperty<int> _coins;

    public FakeWallet(int coins)
    {
        _coins = new ReactiveProperty<int>(coins);
    }

    public IReadOnlyReactiveProperty<int> Coins => _coins;

    public void SetCoins(int value) => _coins.Value = value;

    public bool TrySpend(int amount)
    {
        if (amount > _coins.Value) return false;
        _coins.Value -= amount;
        return true;
    }
}

// ---------- EditMode: async test with UniTask ----------
public sealed class ProfileServiceTests
{
    [UnityTest]
    public IEnumerator LoadAsync_WhenSaveExists_RestoresCurrency() => UniTask.ToCoroutine(async () =>
    {
        var storage = new InMemorySaveStorage();
        storage.Write("profile", "{\"version\":1,\"softCurrency\":250}");
        var service = new ProfileService(storage);

        await service.LoadAsync(CancellationToken.None);

        Assert.That(service.Profile.softCurrency, Is.EqualTo(250));
    });
}
```

Note: `Wallet`, `IWallet`, `InMemorySaveStorage` and `ProfileService.LoadAsync` are illustrative; adapt them to the project's actual types. The `IValidatableConfig` interface lives in a shared service area (e.g. `Services/Configs/`) once more than one config type uses it.
