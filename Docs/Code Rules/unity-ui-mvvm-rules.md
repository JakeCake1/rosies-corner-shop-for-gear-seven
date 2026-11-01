# Unity UI Rules: MVVM and UIController

Format: each rule is `WHEN <trigger> → DO <action>`. Apply a rule only when its trigger matches the current code or task. Prefer the simplest solution that meets the project's needs (YAGNI). Context: Unity, C#, uGUI or UI Toolkit.

---

## MVVM Layers

- **WHEN** creating any screen, window or widget → **DO** split it into three layers:
  - **Model**: game data and domain services (profile, inventory, economy). Knows nothing about UI.
  - **ViewModel**: plain C# class (not a `MonoBehaviour`). Exposes observable state and commands for one view. Knows nothing about Unity UI components.
  - **View**: `MonoBehaviour` (uGUI) or `VisualElement`/`UIDocument` (UI Toolkit). Binds UI elements to the ViewModel. Contains no game logic.
- **WHEN** dependencies are wired → **DO** keep the direction `View → ViewModel → Model`. A ViewModel never references a View; a Model never references a ViewModel.
- **WHEN** a View contains `if` logic about game rules (can afford, is unlocked, cooldown) → **DO** move it to the ViewModel and expose the result as a property (`CanBuy`, `IsLocked`).
- **WHEN** a View needs formatted text (currency, timers, localized strings) → **DO** format in the ViewModel or a dedicated formatter service and expose a ready string; the View only displays it.
- **WHEN** the same data is shown in several views → **DO** let each view have its own ViewModel reading the same Model; don't share one ViewModel across unrelated views.
- **WHEN** a screen contains repeated items (shop items, inventory slots, list rows) → **DO** create a child ViewModel per item and a child View per item; the parent ViewModel exposes a collection of child ViewModels.

---

## ViewModel

- **WHEN** exposing state to the View → **DO** use observable properties (`ReactiveProperty<T>` from R3, or a small custom `ObservableProperty<T>`). Expose them read-only (`IReadOnlyReactiveProperty<T>`) when the View must not write.
- **WHEN** the View triggers an action (button click) → **DO** call a ViewModel method or command (`BuyCommand`, `OnBuyClicked()`). Never call Model or services directly from the View.
- **WHEN** a command can be unavailable → **DO** expose its availability as an observable (`CanExecute`) and let the View bind `interactable` to it.
- **WHEN** the ViewModel subscribes to Model events or reactive streams → **DO** collect subscriptions in a `CompositeDisposable` and dispose them in `Dispose()`.
- **WHEN** the ViewModel needs services → **DO** inject them through the constructor (DI or a factory). No `FindObjectOfType`, no static singletons.
- **WHEN** writing tests → **DO** test ViewModels as plain C# with fake services; they must not require a scene or Play mode.
- **WHEN** the ViewModel needs to close its window or open another → **DO** call `IUIService` (injected) or raise an event the controller handles; don't reference other Views.

---

## View

- **WHEN** writing a View → **DO** keep it thin: references to UI elements, `Bind(viewModel)`, `Unbind()`, and nothing else.
- **WHEN** referencing UI elements in uGUI → **DO** assign them via `[SerializeField] private` fields in the Inspector. Never use `transform.Find`, `GetComponentInChildren` by name, or `GameObject.Find`.
- **WHEN** referencing elements in UI Toolkit → **DO** query them once in `Bind`/initialization (`root.Q<Button>("buy-button")`) and cache them; use name constants, not scattered string literals.
- **WHEN** binding → **DO** subscribe to ViewModel properties in `Bind()` and store every subscription; unsubscribe in `Unbind()` / `OnDestroy()`. Remove button listeners added in code (`onClick.RemoveListener`).
- **WHEN** a View is reused with a new ViewModel (pooled list items, reopened windows) → **DO** call `Unbind()` before `Bind(newViewModel)`.
- **WHEN** using UI Toolkit in Unity 6+ → **DO** consider runtime data binding (`dataSource`, `[CreateProperty]`, `INotifyBindablePropertyChanged`) instead of manual subscriptions for simple property-to-element bindings.
- **WHEN** a View plays animations or transitions → **DO** keep them in the View (or a separate animator component); the ViewModel only exposes state that triggers them.
- **WHEN** text updates frequently (timers, counters) → **DO** update only when the value changes; use `TMP_Text.SetText` with format arguments to avoid string allocations.

---

## UIController / UI Service

- **WHEN** screens are opened and closed from game code → **DO** route everything through a single `IUIService` / `UIController` (`Open<TWindow>()`, `Close<TWindow>()`, `Back()`). Game code never instantiates or enables windows directly.
- **WHEN** creating the controller → **DO** keep it a plain C# service registered in DI, with a scene `UIRoot` `MonoBehaviour` that only holds layer containers. Don't put window logic into `UIRoot`.
- **WHEN** windows can overlap → **DO** organize layers: `HUD`, `Screens` (full-screen), `Popups` (modal), `Overlay` (toasts, loading, tooltips). Each layer is a separate container/Canvas with a fixed sort order.
- **WHEN** windows open on top of each other → **DO** keep a stack per layer (at least for Screens and Popups). `Back()` closes the top window; the Android back button / Escape calls `Back()`.
- **WHEN** a modal popup is open → **DO** block input to lower layers with a full-screen blocker (dimmed background) owned by the Popups layer; clicking the blocker closes the popup only if that popup allows it.
- **WHEN** a window needs data on open → **DO** pass it as a typed payload (`Open<ItemInfoWindow, ItemInfoPayload>(payload)`) or create the ViewModel in the controller via a factory; never set public fields after opening.
- **WHEN** creating windows → **DO** use a window factory that loads prefabs (Addressables preferred, not `Resources`), creates the ViewModel with DI, and calls `view.Bind(viewModel)`.
- **WHEN** a window is closed → **DO** call `Unbind()`, dispose the ViewModel, then either destroy the view or return it to a cache. Cache frequently used windows (inventory, settings); destroy rarely used ones and release their Addressables handles.
- **WHEN** a window is already open and `Open` is called again → **DO** decide the policy explicitly per window: bring to front, refresh payload, or ignore. Never create a duplicate silently.
- **WHEN** opening a window with an open/close animation → **DO** make `Open`/`Close` awaitable (`Task`/`Awaitable`/`UniTask`) and block input during the transition.
- **WHEN** the game changes global state (Menu → Gameplay) → **DO** let the Game State Machine tell the UI service which screens to show/close in `Enter()`/`Exit()`; states don't touch views directly.
- **WHEN** several systems want to show popups at once (rewards, offers, notifications) → **DO** queue them in the UI service with priorities; show one at a time.
- **WHEN** windows need common behavior (close button, back handling, show/hide animation) → **DO** put it in a base `WindowView` class; windows inherit or compose it.

---

## Layout and Device Support

- **WHEN** setting up a Canvas → **DO** use `Canvas Scaler` with `Scale With Screen Size`, a reference resolution, and `Match` tuned for the target orientation.
- **WHEN** targeting phones with notches → **DO** wrap content in a `SafeArea` component that applies `Screen.safeArea` to its `RectTransform`; keep backgrounds full-screen outside it.
- **WHEN** text is localized → **DO** use the Localization package (or your localization service) through the ViewModel; design layouts to tolerate 30–40% longer text.
- **WHEN** supporting gamepad or keyboard → **DO** set the first selected element on window open and restore selection when returning via `Back()`.

---

## Performance

- **WHEN** a Canvas contains elements that change often → **DO** put them in a separate nested Canvas so changes don't rebuild static UI.
- **WHEN** an element isn't clickable → **DO** disable `Raycast Target`.
- **WHEN** hiding cached windows → **DO** disable the `Canvas` component or use `CanvasGroup` (`alpha = 0`, `blocksRaycasts = false`, `interactable = false`) instead of repeatedly toggling `SetActive`.
- **WHEN** showing long lists → **DO** use a virtualized/recycled scroll list with pooled item Views and rebind item ViewModels.
- **WHEN** a ViewModel exposes values updated every frame → **DO** throttle or only push changes when the value differs (`ReactiveProperty` does this by default).
- **WHEN** using Layout Groups on dynamic content → **DO** minimize them; for static layouts, set positions once and disable the Layout Group.

---

## Reference Skeleton

```csharp
// --- Observable property (replace with R3 ReactiveProperty if available) ---
public interface IReadOnlyObservable<out T>
{
    T Value { get; }
    IDisposable Subscribe(Action<T> onChanged); // invokes immediately with current value
}

public sealed class ObservableProperty<T> : IReadOnlyObservable<T>
{
    private T _value;
    private event Action<T> Changed;

    public ObservableProperty(T initial = default) => _value = initial;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value)) return;
            _value = value;
            Changed?.Invoke(value);
        }
    }

    public IDisposable Subscribe(Action<T> onChanged)
    {
        Changed += onChanged;
        onChanged(_value);
        return new Subscription(() => Changed -= onChanged);
    }

    private sealed class Subscription : IDisposable
    {
        private Action _dispose;
        public Subscription(Action dispose) => _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}

// --- Base classes ---
public abstract class ViewModel : IDisposable
{
    protected readonly List<IDisposable> Disposables = new();
    public virtual void Dispose()
    {
        foreach (var d in Disposables) d.Dispose();
        Disposables.Clear();
    }
}

public abstract class View<TViewModel> : MonoBehaviour where TViewModel : ViewModel
{
    private readonly List<IDisposable> _bindings = new();
    protected TViewModel ViewModel { get; private set; }

    public void Bind(TViewModel viewModel)
    {
        Unbind();
        ViewModel = viewModel;
        OnBind(viewModel);
    }

    public void Unbind()
    {
        if (ViewModel == null) return;
        OnUnbind();
        foreach (var b in _bindings) b.Dispose();
        _bindings.Clear();
        ViewModel = null;
    }

    protected void AddBinding(IDisposable binding) => _bindings.Add(binding);
    protected abstract void OnBind(TViewModel viewModel);
    protected virtual void OnUnbind() { }
    protected virtual void OnDestroy() => Unbind();
}

// --- Example: shop item ---
public sealed class ShopItemViewModel : ViewModel
{
    private readonly IWallet _wallet;
    private readonly ShopItemConfig _config;

    public string Title => _config.Title;
    public string PriceText => _config.Price.ToString(CultureInfo.InvariantCulture);
    public ObservableProperty<bool> CanBuy { get; } = new();

    public ShopItemViewModel(IWallet wallet, ShopItemConfig config)
    {
        _wallet = wallet;
        _config = config;
        Disposables.Add(_wallet.Coins.Subscribe(coins => CanBuy.Value = coins >= _config.Price));
    }

    public void Buy()
    {
        if (!CanBuy.Value) return;
        _wallet.Spend(_config.Price);
    }
}

public sealed class ShopItemView : View<ShopItemViewModel>
{
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _price;
    [SerializeField] private Button _buyButton;

    protected override void OnBind(ShopItemViewModel vm)
    {
        _title.SetText(vm.Title);
        _price.SetText(vm.PriceText);
        AddBinding(vm.CanBuy.Subscribe(can => _buyButton.interactable = can));
        _buyButton.onClick.AddListener(vm.Buy);
    }

    protected override void OnUnbind() => _buyButton.onClick.RemoveListener(ViewModel.Buy);
}

// --- UI service ---
public enum UILayer { HUD, Screens, Popups, Overlay }

public interface IWindow
{
    UILayer Layer { get; }
    void Show();
    void Hide();
    void Close(); // unbind + dispose ViewModel + destroy or cache
}

public interface IWindowFactory
{
    Task<IWindow> CreateAsync<TWindow>(Transform parent) where TWindow : IWindow;
}

public interface IUIService
{
    Task<TWindow> Open<TWindow>() where TWindow : class, IWindow;
    void Close(IWindow window);
    bool Back();
}

public sealed class UIService : IUIService
{
    private readonly IWindowFactory _factory;
    private readonly UIRoot _root;
    private readonly Dictionary<UILayer, Stack<IWindow>> _stacks = new();

    public UIService(IWindowFactory factory, UIRoot root)
    {
        _factory = factory;
        _root = root;
        foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            _stacks[layer] = new Stack<IWindow>();
    }

    public async Task<TWindow> Open<TWindow>() where TWindow : class, IWindow
    {
        foreach (var stack in _stacks.Values)
            foreach (var opened in stack)
                if (opened is TWindow existing) return existing; // policy: reuse open window

        var window = (TWindow)await _factory.CreateAsync<TWindow>(_root.ContainerFor(DefaultLayerOf<TWindow>()));
        var target = _stacks[window.Layer];
        if (target.Count > 0 && window.Layer == UILayer.Screens) target.Peek().Hide();
        target.Push(window);
        window.Show();
        return window;
    }

    public void Close(IWindow window)
    {
        var stack = _stacks[window.Layer];
        if (stack.Count == 0 || stack.Peek() != window) return; // only top window closes
        stack.Pop().Close();
        if (window.Layer == UILayer.Screens && stack.Count > 0) stack.Peek().Show();
    }

    public bool Back()
    {
        foreach (var layer in new[] { UILayer.Popups, UILayer.Screens })
        {
            var stack = _stacks[layer];
            if (stack.Count > (layer == UILayer.Screens ? 1 : 0)) { Close(stack.Peek()); return true; }
        }
        return false;
    }

    // Replace with a window config/registry lookup in a real project.
    private static UILayer DefaultLayerOf<TWindow>() => UILayer.Screens;
}

public sealed class UIRoot : MonoBehaviour
{
    [SerializeField] private RectTransform _hud, _screens, _popups, _overlay;

    public Transform ContainerFor(UILayer layer) => layer switch
    {
        UILayer.HUD => _hud,
        UILayer.Screens => _screens,
        UILayer.Popups => _popups,
        _ => _overlay
    };
}
```

Note: the skeleton keeps open/close animations, popup queueing and payloads out for brevity; add them as described in "UIController / UI Service". Replace `ObservableProperty<T>` with R3 `ReactiveProperty<T>` if the project already uses R3.
