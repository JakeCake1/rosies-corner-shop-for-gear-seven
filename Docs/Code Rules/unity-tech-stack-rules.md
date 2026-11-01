# Unity Tech Stack Rules

Stack: **Unity 6000.3.25f1**, **VContainer**, **UniTask**, **UniRx**, **DOTween**, **Addressables**, **UI Toolkit**.

Format: each rule is `WHEN <trigger> → DO <action>`. Apply a rule only when its trigger matches the current code or task. Use only the libraries listed above; do not introduce alternatives (Zenject, R3, LeanTween, Resources, uGUI/TextMeshPro, coroutines for new code) unless explicitly asked.

---

## General

- **WHEN** writing asynchronous code → **DO** use UniTask. Do not use coroutines, `async Task`, or Unity's `Awaitable` in new code; keep one async model across the project.
- **WHEN** wiring dependencies → **DO** use VContainer. No static singletons, no `FindObjectOfType`/`FindAnyObjectByType`, no Service Locator.
- **WHEN** exposing observable state or events → **DO** use UniRx (`ReactiveProperty<T>`, `Subject<T>`, `ReactiveCollection<T>`). Prefer plain C# `event` only for very low-level, high-frequency code.
- **WHEN** animating UI or transforms → **DO** use DOTween. Do not use `Animator` for simple UI transitions.
- **WHEN** loading assets at runtime → **DO** use Addressables. Never use the `Resources` folder.
- **WHEN** building UI → **DO** use UI Toolkit: UXML layouts, USS styles and `VisualElement` views; no uGUI `Canvas` or TextMeshPro.
- **WHEN** organizing code → **DO** follow `unity-project-structure-rules.md` for folders and for where to place Assembly Definitions; in every `.asmdef`, reference the libraries' asmdefs explicitly (`VContainer`, `UniTask`, `UniTask.Addressables`, `UniTask.DOTween`, `UniRx`, `DOTween.Modules`, `Unity.Addressables`, `Unity.ResourceManager`).
- **WHEN** Enter Play Mode Options are enabled with domain reload disabled → **DO** avoid mutable static state; if unavoidable, reset it with `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.

---

## VContainer

### Scopes

- **WHEN** setting up the project → **DO** create a root `ProjectLifetimeScope` prefab (assign it in `VContainerSettings` as Root Lifetime Scope) for app-wide services: assets, saves, UI service, audio, game state machine.
- **WHEN** a scene has its own services (gameplay, level, HUD) → **DO** add a scene `LifetimeScope` whose parent is the project scope; register scene-only services there so they are disposed with the scene.
- **WHEN** a temporary context needs its own services (a window with complex dependencies, a battle instance) → **DO** create a child scope (`parentScope.CreateChild(builder => …)`) and dispose it when the context ends.

### Registration

- **WHEN** registering a plain C# service → **DO** `builder.Register<SaveService>(Lifetime.Singleton).AsImplementedInterfaces()` (add `.AsSelf()` only if the concrete type is needed). Depend on interfaces.
- **WHEN** a class needs startup, per-frame or async init logic → **DO** register it with `builder.RegisterEntryPoint<T>()` and implement `IInitializable`, `IStartable`, `IAsyncStartable`, `ITickable`, `ILateTickable`, `IFixedTickable` or `IDisposable` instead of creating a `MonoBehaviour` for it.
- **WHEN** a scene object must be injectable or injected → **DO** register it with `builder.RegisterComponent(serializedReference)` (assigned in the scope's Inspector) or `builder.RegisterComponentInHierarchy<T>()`. Prefer explicit Inspector references.
- **WHEN** a `MonoBehaviour` needs dependencies → **DO** use method injection: `[Inject] public void Construct(IFoo foo)`. Do not use field injection; constructors are not available for `MonoBehaviour`.
- **WHEN** a plain C# class needs dependencies → **DO** use constructor injection only. No `[Inject]` attributes on plain classes.
- **WHEN** configuration data is needed (`ScriptableObject` configs) → **DO** register the instance: `builder.RegisterInstance(_gameConfig)`, with the config assigned in the scope's Inspector.
- **WHEN** objects of a type are created at runtime with parameters → **DO** register a factory (`builder.RegisterFactory<TParam, TResult>(…)`) or a dedicated factory class; never resolve from `IObjectResolver` inside game logic.

### Instantiation

- **WHEN** instantiating a prefab that has `[Inject]` methods → **DO** use `IObjectResolver.Instantiate(prefab, parent)`, never `Object.Instantiate`; otherwise dependencies are not injected.
- **WHEN** an Addressables-loaded prefab must be injected → **DO** load the prefab asset via the asset provider, then `resolver.Instantiate(prefab, parent)`. Do not use `Addressables.InstantiateAsync` for injectable objects.
- **WHEN** an object already exists and needs injection → **DO** `resolver.InjectGameObject(go)`.
- **WHEN** `IObjectResolver` appears outside factories, scopes or bootstrap code → **DO** refactor: inject the concrete dependency or a factory instead.

---

## UniTask

- **WHEN** writing an async method → **DO** return `UniTask` / `UniTask<T>`. Never `async void` (except Unity event handlers you can't change); for fire-and-forget use `async UniTaskVoid` + `.Forget()`.
- **WHEN** calling an async method without awaiting it → **DO** call `.Forget()` explicitly so exceptions are logged; never discard the task silently.
- **WHEN** an async operation can outlive its owner → **DO** pass a `CancellationToken` through every async method. In `MonoBehaviour`s use `this.GetCancellationTokenOnDestroy()`; in services use a `CancellationTokenSource` cancelled in `Dispose()`.
- **WHEN** a method takes a `CancellationToken` → **DO** make it the last parameter and forward it to every inner await (`UniTask.Delay(ms, cancellationToken: ct)`, `handle.ToUniTask(cancellationToken: ct)`).
- **WHEN** cancellation happens → **DO** let `OperationCanceledException` propagate; catch it only at the top level (entry point / `Forget`) where cancellation is expected. Use `SuppressCancellationThrow()` when a bool result is cleaner.
- **WHEN** waiting for time → **DO** use `UniTask.Delay` (`DelayType.UnscaledDeltaTime` for paused-game UI), `UniTask.Yield`, `UniTask.NextFrame`, `UniTask.WaitUntil`. Avoid `Task.Delay`.
- **WHEN** running several independent operations → **DO** `await UniTask.WhenAll(a, b, c)`; for the first finished, `UniTask.WhenAny`.
- **WHEN** doing CPU-heavy or I/O work (serialization, file writes) → **DO** `await UniTask.SwitchToThreadPool()` / `UniTask.RunOnThreadPool(…)`, then `await UniTask.SwitchToMainThread()` before touching Unity APIs.
- **WHEN** awaiting Addressables or DOTween → **DO** use UniTask's integrations (`UniTask.Addressables`, `UniTask.DOTween`). Make sure the `UNITASK_DOTWEEN_SUPPORT` scripting define symbol is set for DOTween support.
- **WHEN** debugging leaks of pending tasks → **DO** use the UniTask Tracker window (Window → UniTask Tracker).

---

## UniRx

- **WHEN** a ViewModel or service exposes state → **DO** keep a private `ReactiveProperty<T>` and expose `IReadOnlyReactiveProperty<T>` publicly.
- **WHEN** exposing events → **DO** keep a private `Subject<T>` and expose `IObservable<T>`. Call `OnCompleted()`/`Dispose()` on subjects when the owner is disposed.
- **WHEN** subscribing in a `MonoBehaviour` → **DO** `.AddTo(this)` (disposes on destroy). In plain classes, add to a `CompositeDisposable` disposed in `Dispose()`.
- **WHEN** subscribing in a View that can be rebound (pooled, reopened) → **DO** use a per-bind `CompositeDisposable`, clear it in `Unbind()`; don't rely on `AddTo(this)` alone.
- **WHEN** handling button clicks → **DO** `button.OnClickAsObservable().ThrottleFirst(TimeSpan.FromMilliseconds(300)).Subscribe(_ => vm.Buy()).AddTo(_bindings)` to avoid double clicks.
- **WHEN** binding interactivity → **DO** `vm.CanBuy.Subscribe(x => _button.interactable = x).AddTo(_bindings)` (or `BindToButtonOnClick` with a `ReactiveCommand`).
- **WHEN** a value must be derived from several properties → **DO** `Observable.CombineLatest(a, b, (x, y) => …).ToReadOnlyReactiveProperty().AddTo(disposables)`.
- **WHEN** tempted to use `Observable.EveryUpdate()` / `ObserveEveryValueChanged` in many objects → **DO NOT**; use `ITickable` or push-based updates instead. They allocate and poll every frame.
- **WHEN** mixing UniRx and UniTask → **DO** convert with `observable.ToUniTask(useFirstValue: true, ct)` when you need to await one value; don't nest subscriptions inside async methods.
- Note: UniRx is no longer actively developed (its successor is R3). Keep using UniRx consistently in this project; do not mix in R3.

---

## DOTween

- **WHEN** the game starts → **DO** initialize once in bootstrap: `DOTween.Init(recycleAllByDefault: false, useSafeMode: true).SetCapacity(200, 50)`; tune capacity from the profiler.
- **WHEN** creating a tween on an object that can be destroyed → **DO** `.SetLink(gameObject)` so the tween is killed with it, avoiding `MissingReferenceException`.
- **WHEN** starting a new tween on a target that may already be animating → **DO** kill the previous one first (`target.DOKill()`) or keep a `Tween` field and `_tween?.Kill()`.
- **WHEN** a View is disabled, unbound or returned to a pool → **DO** kill its tweens (`DOTween.Kill(this)` with `.SetId(this)`, or killing stored references).
- **WHEN** UI must animate while the game is paused (`Time.timeScale = 0`) → **DO** `.SetUpdate(true)` (unscaled time).
- **WHEN** a tween is played repeatedly (button pulse, panel toggle) → **DO** create it once with `.SetAutoKill(false).Pause()`, then `Restart()`/`PlayForward()`/`PlayBackwards()`; kill it in `OnDestroy`.
- **WHEN** awaiting a tween → **DO** `await tween.ToUniTask(TweenCancelBehaviour.Kill, ct)` so cancellation kills it cleanly.
- **WHEN** combining steps → **DO** use `DOTween.Sequence()` with `Append`/`Join`/`Insert` instead of chained callbacks.
- **WHEN** animating UI → **DO** tween `VisualElement` styles through the `VisualElementTweens` extensions (`DOFade`, `DOScale`, `DOTranslate`, `DORotate`, `DOShake`), with `.SetId(this)` in views and `.SetUpdate(true)` for windows. Prefer `translate`/`scale`/`rotate`/`opacity` (no relayout); `DOPosition`/`DOBottom`/`DOWidthPercent` change layout and are for a few absolutely positioned elements only.
- **WHEN** tweens are created in `Update` → **DO NOT**; create tweens on events only.

---

## Addressables

- **WHEN** loading any asset → **DO** go through an injected `IAssetProvider` service, never call `Addressables` directly from gameplay or UI code.
- **WHEN** referencing assets from configs or components → **DO** use `AssetReference`, `AssetReferenceGameObject`, `AssetReferenceT<T>` or `AssetReferenceSprite`, not string paths. Use string keys/labels only for groups loaded together.
- **WHEN** loading an asset → **DO** keep its `AsyncOperationHandle`; the owner that loads is responsible for `Addressables.Release(handle)`. Every load must have a matching release.
- **WHEN** the same asset is requested by several systems → **DO** cache handles in `IAssetProvider` by key and release them at a defined point (scene unload / `CleanUp()`), instead of loading duplicates.
- **WHEN** a scene or window closes → **DO** release the assets it loaded; scene-scoped providers release on scope disposal.
- **WHEN** the app starts → **DO** `await Addressables.InitializeAsync()` in bootstrap before any load.
- **WHEN** using remote content → **DO** check `CheckForCatalogUpdates` / `UpdateCatalogs`, get size with `GetDownloadSizeAsync`, then `DownloadDependenciesAsync` with progress UI before loading; release the download handle afterward.
- **WHEN** loading scenes → **DO** `Addressables.LoadSceneAsync(key, LoadSceneMode.Single/Additive)` and unload additive scenes with `Addressables.UnloadSceneAsync(handle)`.
- **WHEN** organizing groups → **DO** group by load lifetime (always-loaded, per-level, per-feature, remote), not by asset type. Run the Analyze tool to find duplicated dependencies across bundles.
- **WHEN** an asset is in an Addressables group → **DO NOT** also reference it directly from a scene or non-addressable asset; it gets duplicated into the build.
- **WHEN** debugging leaks → **DO** use the Addressables Profiler module / Event Viewer to check that reference counts return to zero.

---

## UI Toolkit

- **WHEN** creating UI → **DO** follow MVVM: plain C# ViewModels and plain C# Views (`ElementView` → `View<TViewModel>` → `WindowView<TViewModel>`) that bind to a `VisualElement` with UniRx. No `MonoBehaviour` per view.
- **WHEN** laying out UI → **DO** author UXML for structure and USS for style, sizes and positions; one `PanelSettings` with `Scale With Screen Size` at the 390×844 reference resolution, so one USS pixel is one prototype CSS pixel. Never build scenes or UI from editor scripts.
- **WHEN** opening windows → **DO** use an injected `IUIService` (layers: Scene, HUD, Screens, Popups, Overlay). Window layouts are `VisualTreeAsset`s loaded via `IAssetProvider` (`ui/<WindowType>`) and instantiated only when the window opens.
- **WHEN** an element is needed only sometimes (toasts, coach bubbles, dialog blocks, locks, booster buttons) → **DO** create it when it is first shown and remove it from the hierarchy when hidden; do not keep hidden elements in the tree.
- **WHEN** referencing elements → **DO** query by name once in `OnAttached` (`Q<T>(name)` with a `const` name) and cache the reference; never query per frame.
- **WHEN** an element isn't clickable → **DO** set `picking-mode="Ignore"`.
- **WHEN** many identical elements spawn repeatedly (items, shelves, baskets) → **DO** pool them with `ElementPool<T>` / `PooledElementView`.
- **WHEN** animating elements → **DO** see the DOTween rules: prefer `translate`, `scale`, `rotate` and `opacity`; layout properties only for a few absolutely positioned elements.
- **WHEN** two or more views share code → **DO** move it into the base view classes.
- **WHEN** handling notched devices → **DO** apply `Screen.safeArea` to the safe layer (`UIRoot`).

---

## Game Flow

- **WHEN** implementing app flow (Bootstrap → LoadProgress → Menu → LoadLevel → Gameplay) → **DO** implement a `GameStateMachine` registered as a singleton in the project scope; states are plain classes created by VContainer with `UniTask Enter(CancellationToken)` / `UniTask Exit(CancellationToken)`.
- **WHEN** the app starts → **DO** start the state machine from an entry point (`IAsyncStartable` or `IStartable` + `Forget()`), not from a `MonoBehaviour.Start`.
- **WHEN** a state loads a scene → **DO** use `IAssetProvider`/scene loader with Addressables, show a loading curtain from the Overlay layer, and let the scene's `LifetimeScope` build its services.

---

## Reference Skeleton

```csharp
// ---------- Project scope ----------
public sealed class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] private UIRoot _uiRootPrefab;
    [SerializeField] private GameConfig _gameConfig;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(_gameConfig);
        builder.RegisterComponentInNewPrefab(_uiRootPrefab, Lifetime.Singleton).DontDestroyOnLoad();

        builder.Register<AssetProvider>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<UIService>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<GameStateMachine>(Lifetime.Singleton);
        builder.Register<BootstrapState>(Lifetime.Singleton);
        builder.Register<MenuState>(Lifetime.Singleton);

        builder.RegisterEntryPoint<GameEntryPoint>();
    }
}

public sealed class GameEntryPoint : IStartable
{
    private readonly GameStateMachine _stateMachine;
    public GameEntryPoint(GameStateMachine stateMachine) => _stateMachine = stateMachine;

    public void Start() => _stateMachine.Enter<BootstrapState>(CancellationToken.None).Forget();
}

// ---------- Game state machine ----------
public interface IGameState
{
    UniTask Enter(CancellationToken ct);
    UniTask Exit(CancellationToken ct);
}

public sealed class GameStateMachine
{
    private readonly IObjectResolver _resolver; // allowed here: infrastructure code
    private IGameState _current;

    public GameStateMachine(IObjectResolver resolver) => _resolver = resolver;

    public async UniTask Enter<TState>(CancellationToken ct) where TState : IGameState
    {
        if (_current != null) await _current.Exit(ct);
        _current = _resolver.Resolve<TState>();
        await _current.Enter(ct);
    }
}

public sealed class BootstrapState : IGameState
{
    private readonly IAssetProvider _assets;
    private readonly GameStateMachine _stateMachine;

    public BootstrapState(IAssetProvider assets, GameStateMachine stateMachine)
    {
        _assets = assets;
        _stateMachine = stateMachine;
    }

    public async UniTask Enter(CancellationToken ct)
    {
        await _assets.InitializeAsync(ct);
        await _stateMachine.Enter<MenuState>(ct);
    }

    public UniTask Exit(CancellationToken ct) => UniTask.CompletedTask;
}

// ---------- Asset provider ----------
public interface IAssetProvider
{
    UniTask InitializeAsync(CancellationToken ct);
    UniTask<T> LoadAsync<T>(object key, CancellationToken ct) where T : class;
    void Release(object key);
    void ReleaseAll();
}

public sealed class AssetProvider : IAssetProvider, IDisposable
{
    private readonly Dictionary<object, AsyncOperationHandle> _handles = new();

    public async UniTask InitializeAsync(CancellationToken ct) =>
        await Addressables.InitializeAsync().ToUniTask(cancellationToken: ct);

    public async UniTask<T> LoadAsync<T>(object key, CancellationToken ct) where T : class
    {
        if (key is AssetReference reference) key = reference.RuntimeKey;

        if (_handles.TryGetValue(key, out var cached))
        {
            if (!cached.IsDone) await cached.ToUniTask(cancellationToken: ct);
            return cached.Result as T;
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        _handles[key] = handle;
        try
        {
            return await handle.ToUniTask(cancellationToken: ct);
        }
        catch
        {
            _handles.Remove(key);
            if (handle.IsValid()) Addressables.Release(handle);
            throw;
        }
    }

    public void Release(object key)
    {
        if (key is AssetReference reference) key = reference.RuntimeKey;
        if (!_handles.Remove(key, out var handle)) return;
        if (handle.IsValid()) Addressables.Release(handle);
    }

    public void ReleaseAll()
    {
        foreach (var handle in _handles.Values)
            if (handle.IsValid()) Addressables.Release(handle);
        _handles.Clear();
    }

    public void Dispose() => ReleaseAll();
}

// ---------- MVVM base (UniRx) ----------
public abstract class ViewModel : IDisposable
{
    protected readonly CompositeDisposable Disposables = new();
    public virtual void Dispose() => Disposables.Dispose();
}

// UI Toolkit views are plain C# classes on a VisualElement subtree (see unity-ui-mvvm-rules.md for the full set).
public abstract class View<TViewModel> : ElementView where TViewModel : class
{
    private CompositeDisposable _bindings = new();
    protected TViewModel ViewModel { get; private set; }
    protected CompositeDisposable Bindings => _bindings;

    public void Bind(TViewModel viewModel)
    {
        Unbind();
        ViewModel = viewModel;
        OnBind(viewModel);
    }

    public void Unbind()
    {
        if (ViewModel == null) return;
        _bindings.Dispose();
        _bindings = new CompositeDisposable();
        DOTween.Kill(this);
        OnUnbind();
        ClearSpawned(); // elements spawned while bound leave the tree
        ViewModel = null;
    }

    protected abstract void OnBind(TViewModel viewModel);
    protected virtual void OnUnbind() { }
}

// ---------- Window with DOTween show/hide ----------
public enum UILayer { Scene, HUD, Screens, Popups, Overlay }

public abstract class WindowView<TViewModel> : View<TViewModel> where TViewModel : ViewModel
{
    private const float FadeSeconds = 0.2f;

    public abstract UILayer Layer { get; }

    public async UniTask ShowAsync(CancellationToken ct)
    {
        Root.pickingMode = PickingMode.Ignore; // no taps while animating
        Root.style.opacity = 0f;
        await Root.DOFade(1f, FadeSeconds).SetUpdate(true).SetId(this)
            .ToUniTask(TweenCancelBehaviour.Kill, ct);
        Root.pickingMode = PickingMode.Position;
    }

    public async UniTask HideAsync(CancellationToken ct)
    {
        Root.pickingMode = PickingMode.Ignore;
        await Root.DOFade(0f, FadeSeconds).SetUpdate(true).SetId(this)
            .ToUniTask(TweenCancelBehaviour.Kill, ct);
    }

    protected override void OnAttached() => OnLayoutAttached();
    protected abstract void OnLayoutAttached();
}

// ---------- Example window (ShopWindow.uxml at address "ui/ShopWindow") ----------
public sealed class ShopViewModel : ViewModel
{
    private const int Price = 100;
    private readonly ReactiveProperty<int> _coins = new();

    public ShopViewModel(IWallet wallet)
    {
        wallet.Coins.Subscribe(c => _coins.Value = c).AddTo(Disposables);
        CanBuy = _coins.Select(c => c >= Price).ToReadOnlyReactiveProperty().AddTo(Disposables);
        _coins.AddTo(Disposables);
    }

    public IReadOnlyReactiveProperty<int> Coins => _coins;
    public IReadOnlyReactiveProperty<bool> CanBuy { get; }

    public void Buy() { /* call IWallet / IShopService */ }
}

public sealed class ShopWindow : WindowView<ShopViewModel>
{
    private const string CoinsName = "coins-value";
    private const string BuyName = "buy";

    private Label _coins;
    private Button _buy;

    public override UILayer Layer => UILayer.Screens;

    protected override void OnLayoutAttached()
    {
        _coins = Root.Q<Label>(CoinsName);
        _buy = Root.Q<Button>(BuyName);
    }

    protected override void OnBind(ShopViewModel vm)
    {
        vm.Coins.Subscribe(c => _coins.text = c.ToString()).AddTo(Bindings);
        vm.CanBuy.Subscribe(x => _buy.SetEnabled(x)).AddTo(Bindings);
        _buy.OnClickAsObservable() // Button.clicked as an observable (ElementExtensions)
            .ThrottleFirst(TimeSpan.FromMilliseconds(300))
            .Subscribe(_ => vm.Buy())
            .AddTo(Bindings);
    }
}
```

Notes for the agent:
- `UNITASK_DOTWEEN_SUPPORT` must be in Scripting Define Symbols for `ToUniTask` on tweens; UniTask's Addressables support activates automatically when the Addressables package is installed.
- Verify VContainer API signatures (e.g. `IAsyncStartable`, `RegisterComponentInNewPrefab`) against the installed package version before relying on them.
- `ElementView`, `UIRoot` (owns the `UIDocument`), `UIService` (loads `ui/<WindowType>` layouts), `IWallet`, `GameConfig`, `MenuState` follow the UI and save-system rule files; implement them per those rules.
