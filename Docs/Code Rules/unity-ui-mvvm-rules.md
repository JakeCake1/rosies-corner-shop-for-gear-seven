# Unity UI Rules: MVVM and UIController

Format: each rule is `WHEN <trigger> → DO <action>`. Apply a rule only when its trigger matches the current code or task. Prefer the simplest solution that meets the project's needs (YAGNI). Context: Unity 6, C#, **UI Toolkit** (UXML + USS), UniRx, UniTask, DOTween. uGUI (`Canvas`, `RectTransform`, TextMeshPro) is not used.

---

## MVVM Layers

- **WHEN** creating any screen, window or widget → **DO** split it into three layers:
  - **Model**: game data and domain services (profile, inventory, economy). Knows nothing about UI.
  - **ViewModel**: plain C# class (not a `MonoBehaviour`). Exposes observable state and commands for one view. Knows nothing about UI Toolkit.
  - **View**: plain C# class derived from `ElementView` that wraps one `VisualElement` subtree (instantiated from UXML). Binds elements to the ViewModel. Contains no game logic. Views are not `MonoBehaviour`s.
- **WHEN** dependencies are wired → **DO** keep the direction `View → ViewModel → Model`. A ViewModel never references a View; a Model never references a ViewModel.
- **WHEN** a View contains `if` logic about game rules (can afford, is unlocked, cooldown) → **DO** move it to the ViewModel and expose the result as a property (`CanBuy`, `IsLocked`).
- **WHEN** a View needs formatted text (currency, timers, localized strings) → **DO** format in the ViewModel or a dedicated formatter service and expose a ready string; the View only displays it.
- **WHEN** the same data is shown in several views → **DO** let each view have its own ViewModel reading the same Model; don't share one ViewModel across unrelated views.
- **WHEN** a screen contains repeated items (shop items, inventory slots, list rows) → **DO** create a child ViewModel per item and a child View per item; the parent ViewModel exposes a collection of child ViewModels.

---

## ViewModel

- **WHEN** exposing state to the View → **DO** use UniRx `ReactiveProperty<T>`, exposed read-only (`IReadOnlyReactiveProperty<T>`) when the View must not write.
- **WHEN** the View triggers an action (button tap) → **DO** call a ViewModel method or command (`Buy()`, `OpenAlbum()`). Never call Model or services directly from the View.
- **WHEN** a command can be unavailable → **DO** expose its availability as an observable (`CanBuy`) and let the View bind `SetEnabled` / a USS class to it.
- **WHEN** the ViewModel subscribes to Model events or reactive streams → **DO** collect subscriptions in a `CompositeDisposable` and dispose them in `Dispose()`.
- **WHEN** the ViewModel needs services → **DO** inject them through the constructor (DI or a factory). No `FindObjectOfType`, no static singletons.
- **WHEN** writing tests → **DO** test ViewModels as plain C# with fake services; they must not require a scene, a panel or Play mode.
- **WHEN** the ViewModel needs to close its window or open another → **DO** call `IUIService` (injected) or raise an event (`CloseRequested`) the screen controller handles; don't reference other Views.

---

## View

- **WHEN** writing a View → **DO** keep it thin: element references, `Bind(viewModel)`, `Unbind()`, and visual reactions (animations, classes). Nothing else.
- **WHEN** a View needs its elements → **DO** query them once, right after the layout is attached (`OnAttached` / `OnLayoutAttached`), with `Q<T>(name)` and a `private const string ...Name` per element, and cache the references. Never query in `Bind`, per frame, or with scattered string literals.
- **WHEN** naming elements and classes in UXML/USS → **DO** use kebab-case element names (`coins-value`, `play-line`) and BEM-like classes scoped to the feature (`home-play`, `home-play__title`, `toast--warning`). Shared look lives in `Services/UI/Layouts` (`Game.uss`, `Kit.uss`, `Dialog.uss`); feature styles live next to the feature's UXML.
- **WHEN** binding → **DO** subscribe to ViewModel properties in `OnBind` and add every subscription to `Bindings` (`.AddTo(Bindings)`); `Unbind()` disposes them, kills the View's tweens and removes elements the View spawned. Bind button taps with `OnTap(button, vm.Action)` (`clicked` as an observable, throttled), never with lambdas added to `clicked` and forgotten.
- **WHEN** a View is reused with a new ViewModel (pooled list items, reopened windows) → **DO** call `Unbind()` before `Bind(newViewModel)` (`Bind` does it).
- **WHEN** a property maps 1:1 to an element (a label's text, a toggle) → **DO** a plain UniRx subscription is enough; Unity 6 runtime data binding (`dataSource`, `[CreateProperty]`) is not used in this project, so don't mix the two.
- **WHEN** a View plays animations or transitions → **DO** keep them in the View, using DOTween on `VisualElement` styles through `VisualElementTweens` (`DOFade`, `DOScale`, `DOTranslate`, …) with `.SetId(this)`; the ViewModel only exposes the state that triggers them.
- **WHEN** state changes the look (locked, tricky, selected) → **DO** toggle USS classes (`EnableInClassList`) instead of setting many inline styles from code. Inline styles from code are for values computed at runtime (positions, sizes, sprites).
- **WHEN** text updates frequently (timers, counters) → **DO** set `Label.text` only when the value changes (bind to a `ReactiveProperty`, which skips equal values).
- **WHEN** two or more Views or windows repeat the same code (querying, show/hide, close button, spawning helpers) → **DO** move it into the base classes (`ElementView` → `View<TViewModel>` → `WindowView<TViewModel>`, `PooledElementView`) instead of copying it.

---

## Creating Elements Only When Needed

- **WHEN** an element is shown only sometimes (toast, coach bubble, tutorial hand, dialog sections, lock buttons, booster buttons) → **DO** create it the first time it is shown and remove it from the hierarchy (`RemoveFromHierarchy()`) when it is hidden; keep the C# reference for reuse. Don't keep hidden elements in the tree with `display: none` as a substitute.
- **WHEN** a window is opened → **DO** load its layout (`VisualTreeAsset`) through Addressables and instantiate it at that moment; the scene and `Root.uxml` contain only the layer containers, never pre-built windows.
- **WHEN** elements of one kind spawn and despawn repeatedly (board items, shelves, baskets, list rows) → **DO** pool them with `ElementPool<T>` over `PooledElementView`; release returns them to the pool and removes them from the tree.
- **WHEN** a dialog body depends on data → **DO** build only the sections the ViewModel asks for (`Spawn(parent, element)`), so they are removed automatically on `Unbind()`.

---

## UIController / UI Service

- **WHEN** screens are opened and closed from game code → **DO** route everything through the single `IUIService` (`OpenAsync<TWindow, TViewModel>(vm, ct)`, `CloseAsync(window)`, `CloseAllAsync(layer)`). Game code never instantiates layouts or adds windows to the panel directly.
- **WHEN** creating the controller → **DO** keep it a plain C# service registered in DI. The only `MonoBehaviour` is `UIRoot`: it owns the `UIDocument`, exposes the layer containers and applies the safe area. Don't put window logic into `UIRoot`.
- **WHEN** windows can overlap → **DO** use the layers from `Root.uxml`: `Scene` (gameplay board), `HUD`, `Screens` (full-screen), `Popups` (modal, with the blocker), `Overlay` (toasts, coach, effects). Order is the order of the containers in `Root.uxml`.
- **WHEN** windows open on top of each other → **DO** keep a stack per layer (at least Screens and Popups). `Back()` (Android back button / Escape) closes the top popup if it allows it (`IsClosedByBlocker`).
- **WHEN** a modal popup is open → **DO** block input to lower layers with the full-screen blocker of the Popups layer; tapping it closes the popup only if that popup allows it (`IsClosedByBlocker`).
- **WHEN** a window needs data on open → **DO** pass a ViewModel created by the caller or a factory (`OpenAsync<AlbumWindow, AlbumViewModel>(new AlbumViewModel(...))`); never set public fields after opening.
- **WHEN** creating windows → **DO** load the layout by address `ui/<WindowType>` via `IAssetProvider`, instantiate it, create the View with `new`, inject it with `IObjectResolver.Inject`, `Attach` the layout, add it to its layer, `Bind` the ViewModel, then `await ShowAsync`.
- **WHEN** a window is closed → **DO** `await HideAsync`, `Release()` (unbind, complete `Closed`), dispose the ViewModel and remove the element from the panel.
- **WHEN** a window is already open and `Open` is called again → **DO** decide the policy explicitly per window: bring to front, refresh, or close the old one first (the default). Never create a duplicate silently.
- **WHEN** opening a window with an open/close animation → **DO** make `Open`/`Close` awaitable (`UniTask`) and block input during the transition (the window's input shield).
- **WHEN** the game changes global state (Home → Gameplay) → **DO** let the Game State Machine tell the UI service which screens to show/close in `Enter()`/`Exit()`; states don't touch views directly.
- **WHEN** several systems want to show popups at once (rewards, offers, notifications) → **DO** queue them in the UI/dialog service; show one at a time.
- **WHEN** windows need common behavior (close button, back handling, show/hide animation) → **DO** put it in `WindowView<TViewModel>`; windows inherit it.

---

## Layout, Resolution and Device Support

- **WHEN** setting up the panel → **DO** use one `PanelSettings` asset with `Scale With Screen Size`, the mockup's reference resolution (390×844 portrait) and `Match` 0.5. One USS pixel is then one CSS pixel of the prototype, so sizes and positions from the mockups/`styles.css` are written into USS as they are.
- **WHEN** laying out a screen → **DO** author it in UXML + USS; never build scenes, layouts or styles from editor scripts or at runtime from code. Use absolute positions where the mockup has a fixed frame (HUD, sign, counter) and flex rows/columns for repeated content (lists, buttons in a dialog).
- **WHEN** finishing any layout change → **DO** compare a Play-mode screenshot at 390×844 with the matching mockup (the project's mockup set; Rosie's Corner Shop: `screens.rar`): positions, sizes and spacing should match within a few pixels; text should not wrap where the mockup has one line.
- **WHEN** content moves in from outside its container (shelves dropping in, sliding panels) → **DO** clip with `overflow: hidden` on the container, as the prototype's CSS does, so it never draws over other UI.
- **WHEN** targeting phones with notches → **DO** let `UIRoot` apply `Screen.safeArea` to the safe container; keep backgrounds full-screen outside it.
- **WHEN** text is localized → **DO** use the Localization package (or a localization service) through the ViewModel; design layouts to tolerate 30–40% longer text.
- **WHEN** supporting gamepad or keyboard → **DO** focus the first element on window open and restore focus when returning via `Back()`.

---

## Performance

- **WHEN** an element isn't clickable (decor, labels, containers) → **DO** set `picking-mode="Ignore"`.
- **WHEN** animating → **DO** tween `translate`, `scale`, `rotate` and `opacity` (no relayout). Tween layout properties (`left`/`top`/`bottom`/`width`) only on absolutely positioned elements whose final layout value matters (a shelf dropping to its slot, a progress fill, an item flying on the flight layer).
- **WHEN** hiding a window → **DO** remove it from the panel; for elements toggled often inside a view, toggle a USS class or `display` rather than rebuilding them.
- **WHEN** showing long lists → **DO** use `ListView` (virtualized) or pooled row Views and rebind row ViewModels.
- **WHEN** a ViewModel exposes values updated every frame → **DO** throttle or only push changes when the value differs (`ReactiveProperty` does this by default).
- **WHEN** a style is shared by many elements → **DO** put it in a USS class instead of inline styles, so the style is resolved once.

---

## Reference Skeleton

The real classes live in `Assets/Game/Services/UI` (`ElementView`, `View<T>`, `WindowView<T>`, `UIService`, `UIRoot`) and `Assets/Game/Services/Pooling` (`ElementPool<T>`, `PooledElementView`). This skeleton shows their shape.

```csharp
// --- Base classes ---
public abstract class ViewModel : IDisposable
{
    protected readonly CompositeDisposable Disposables = new CompositeDisposable();
    public virtual void Dispose() => Disposables.Dispose();
}

/// Wraps one VisualElement subtree; queries and spawns elements for its subclasses.
public abstract class ElementView
{
    private readonly List<VisualElement> _spawned = new List<VisualElement>();

    public VisualElement Root { get; private set; }

    public void Attach(VisualElement root)
    {
        Root = root;
        OnAttached();
    }

    protected abstract void OnAttached();

    protected T Q<T>(string name) where T : VisualElement => Root.Q<T>(name);

    /// Adds an element that exists only while the view is bound; removed by ClearSpawned.
    protected T Spawn<T>(VisualElement parent, T element) where T : VisualElement
    {
        parent.Add(element);
        _spawned.Add(element);
        return element;
    }

    protected void ClearSpawned()
    {
        foreach (var element in _spawned) element.RemoveFromHierarchy();
        _spawned.Clear();
    }
}

public abstract class View<TViewModel> : ElementView where TViewModel : class
{
    private CompositeDisposable _bindings = new CompositeDisposable();

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
        ClearSpawned();
        ViewModel = null;
    }

    protected abstract void OnBind(TViewModel viewModel);
    protected virtual void OnUnbind() { }
}

public enum UILayer { Scene, HUD, Screens, Popups, Overlay }

/// Show/hide animation, input shield while animating, close request.
public abstract class WindowView<TViewModel> : View<TViewModel>, IWindow where TViewModel : ViewModel
{
    private readonly Subject<IWindow> _closeRequested = new Subject<IWindow>();

    public abstract UILayer Layer { get; }
    public IObservable<IWindow> CloseRequested => _closeRequested;

    public UniTask ShowAsync(CancellationToken ct)
    {
        Root.style.opacity = 0f;
        return Root.DOFade(1f, 0.2f).SetUpdate(true).SetId(this).ToUniTask(TweenCancelBehaviour.Kill, ct);
    }

    public UniTask HideAsync(CancellationToken ct) =>
        Root.DOFade(0f, 0.15f).SetUpdate(true).SetId(this).ToUniTask(TweenCancelBehaviour.Kill, ct);

    protected override void OnAttached() => OnLayoutAttached();
    protected abstract void OnLayoutAttached();
    protected void RequestClose() => _closeRequested.OnNext(this);
    protected void OnTap(Button button, Action action) =>
        button.OnTapAsObservable().Subscribe(_ => action()).AddTo(Bindings);
}

// --- Example: shop window (ShopWindow.uxml + Shop.uss next to the feature, address "ui/ShopWindow") ---
public sealed class ShopViewModel : ViewModel
{
    private const int Price = 100;
    private readonly ReactiveProperty<string> _coins = new ReactiveProperty<string>();

    public ShopViewModel(IWallet wallet)
    {
        wallet.Coins.Subscribe(c => _coins.Value = c.ToString()).AddTo(Disposables);
        CanBuy = wallet.Coins.Select(c => c >= Price).ToReadOnlyReactiveProperty().AddTo(Disposables);
        _coins.AddTo(Disposables);
    }

    public IReadOnlyReactiveProperty<string> Coins => _coins;
    public IReadOnlyReactiveProperty<bool> CanBuy { get; }

    public void Buy() { /* IWallet / IShopService */ }
}

public sealed class ShopWindow : WindowView<ShopViewModel>
{
    private const string CoinsName = "coins-value";
    private const string BuyName = "buy";
    private const string DisabledClass = "btn--disabled";

    private Label _coins;
    private Button _buy;

    public override UILayer Layer => UILayer.Screens;

    protected override void OnLayoutAttached()
    {
        _coins = Q<Label>(CoinsName);
        _buy = Q<Button>(BuyName);
    }

    protected override void OnBind(ShopViewModel vm)
    {
        vm.Coins.Subscribe(t => _coins.text = t).AddTo(Bindings);
        vm.CanBuy.Subscribe(x =>
        {
            _buy.SetEnabled(x);
            _buy.EnableInClassList(DisabledClass, !x);
        }).AddTo(Bindings);
        OnTap(_buy, vm.Buy);
    }
}

// --- UI service: loads the layout on open, one stack per layer ---
public sealed class UIService : IUIService
{
    private const string AddressPrefix = "ui/";
    private readonly IAssetProvider _assets;
    private readonly IObjectResolver _resolver; // infrastructure: injects views
    private readonly UIRoot _root;

    public async UniTask<TWindow> OpenAsync<TWindow, TViewModel>(TViewModel viewModel, CancellationToken ct)
        where TWindow : WindowView<TViewModel>, new()
        where TViewModel : ViewModel
    {
        var layout = await _assets.LoadAsync<VisualTreeAsset>(AddressPrefix + typeof(TWindow).Name, ct);
        var element = layout.Instantiate();
        element.Stretch();

        var window = new TWindow();
        _resolver.Inject(window);
        window.Attach(element);
        _root.GetLayer(window.Layer).Add(element);
        window.Bind(viewModel);
        await window.ShowAsync(ct);
        return window;
    }
}

// --- UIRoot: the only MonoBehaviour of the UI; holds the UIDocument (Root.uxml) and its layers ---
public sealed class UIRoot : MonoBehaviour
{
    [SerializeField] private UIDocument _document;

    public VisualElement GetLayer(UILayer layer) => _document.rootVisualElement.Q(LayerName(layer));

    private static string LayerName(UILayer layer) => layer.ToString().ToLowerInvariant();
}
```

Note: the skeleton leaves out stacks, the blocker, `Back()`, safe area and popup queueing for brevity; implement them as described in "UIController / UI Service" (see `UIService` and `UIRoot` in the project).
