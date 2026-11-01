# Unity Architecture Rules: GoF, GRASP, SOLID, DRY, YAGNI, Game State Machine

Format: each rule is `WHEN <trigger> → DO <action>`. Apply a rule only when its trigger matches the current code or task. Prefer the simplest rule that solves the problem (see YAGNI). Context: Unity, C#.

---

## GoF Design Patterns

### Creational

- **WHEN** objects of the same type are frequently created and destroyed (bullets, VFX, enemies) → **DO** use **Object Pool**: keep ready instances and take them from the pool instead of calling `Instantiate`/`Destroy`. Use `UnityEngine.Pool.ObjectPool<T>`. Follow the full rules in the **Object Pooling** section below.
- **WHEN** creating an object requires type selection logic or dependency setup → **DO** use **Factory / Factory Method**: move creation into a factory (`EnemyFactory.Create(type)`).
- **WHEN** an object is assembled from many optional parameters → **DO** use **Builder**: step-by-step construction with clear methods instead of a constructor with 10 arguments.
- **WHEN** you need copies of a configured object with small differences → **DO** use **Prototype**: clone a template (prefab, `ScriptableObject` config) and change only the differences.
- **WHEN** exactly one global service is needed (audio, saves) → **DO** register it in a DI container (Zenject/VContainer) or a Service Locator. Use **Singleton** only if DI is not available in the project.

### Structural

- **WHEN** a third-party SDK or legacy code doesn't fit your interface → **DO** use **Adapter**: a wrapper that maps the foreign API to your interface (`IAnalytics` → Firebase/AppMetrica).
- **WHEN** a subsystem is complex but clients need only a few operations → **DO** use **Facade**: one class with simple methods over the subsystem.
- **WHEN** behavior must be added to an object without inheritance (buffs, damage modifiers) → **DO** use **Decorator**: wrap the object in layers, each adding its own behavior.
- **WHEN** objects form a tree and a node and a group are handled the same way (UI, inventory with bags) → **DO** use **Composite**: a shared interface for leaf and container.
- **WHEN** thousands of objects share identical data (trees, tiles) → **DO** use **Flyweight**: move shared data into a shared object (`ScriptableObject`, shared mesh/material).
- **WHEN** access to an object is expensive or needs control (lazy loading, caching) → **DO** use **Proxy**: an intermediary with the same interface.

### Behavioral

- **WHEN** several systems must react to an event without knowing about each other → **DO** use **Observer**: C# `event`/`Action`, an event bus, or reactive properties (R3/UniRx). Always unsubscribe in `OnDisable`/`OnDestroy`.
- **WHEN** an action must be undone, redone, queued or recorded (undo, replays, input) → **DO** use **Command**: encapsulate the action in an object with `Execute`/`Undo`.
- **WHEN** an object's behavior changes sharply by mode (Idle/Run/Attack) and `switch`/`if` chains keep growing → **DO** use **State**: each state is a separate class; the object delegates logic to the current state.
- **WHEN** there are interchangeable algorithms (AI types, movement modes, damage formulas) → **DO** use **Strategy**: an algorithm interface with implementations swapped at runtime.
- **WHEN** an algorithm has the same steps but individual steps differ → **DO** use **Template Method**: the base class defines the order, subclasses override the steps.
- **WHEN** objects communicate "everyone with everyone" and dependencies are tangled (UI windows) → **DO** use **Mediator**: a central mediator manages the interaction.
- **WHEN** state must be saved and restored (saves, checkpoints) → **DO** use **Memento**: a state snapshot separated from the object.
- **WHEN** a request must be processed by several handlers in order (damage through armor → shield → HP) → **DO** use **Chain of Responsibility**: each handler processes the request or passes it on.
- **WHEN** a collection must be traversed without exposing its structure → **DO** use **Iterator**: `IEnumerable<T>`; in hot code, use a custom struct enumerator to avoid allocations.
- **WHEN** operations must be added over a stable type hierarchy (export, level validation) → **DO** use **Visitor**: the operation lives in a separate class; elements accept the visitor.

---

## Object Pooling

Stack-specific: pools are created through VContainer, prefabs come from Addressables via `IAssetProvider`, async work uses UniTask. UI is UI Toolkit, so pooled UI elements use `ElementPool<T>` (see "Pooling UI elements"); the GameObject rules below apply to scene objects.

### When to pool

- **WHEN** objects of one type are spawned and removed often during gameplay (bullets, projectiles, hit VFX, enemies, pickups, damage numbers, audio sources, recycled UI list items) → **DO** pool them instead of `Instantiate`/`Destroy`.
- **WHEN** an object is created once or rarely (player, level root, a window opened once per session) → **DO NOT** pool it (YAGNI); a pool only adds state-reset bugs there.
- **WHEN** unsure whether pooling is needed → **DO** check the Profiler first: look for `Instantiate`/`Destroy` spikes and GC Alloc in the gameplay loop.

### Which pool to use

- **WHEN** pooling GameObjects/components → **DO** wrap `UnityEngine.Pool.ObjectPool<T>` in a typed pool class (one pool per prefab). Don't write a pool from scratch.
- **WHEN** pooling UI Toolkit elements (board items, shelves, list rows) → **DO** use `ElementPool<T>` (it wraps `ObjectPool<T>`) with views derived from `PooledElementView`.
- **WHEN** a temporary collection is needed inside a method → **DO** use `ListPool<T>.Get(out var list)` / `HashSetPool<T>` / `DictionaryPool<TKey, TValue>` with `using` so it's returned automatically.
- **WHEN** a temporary array buffer is needed → **DO** use `ArrayPool<T>.Shared.Rent` / `Return` (remember the rented array can be larger than requested).
- **WHEN** pooling plain C# objects (commands, events, data packets) → **DO** use `GenericPool<T>` or `ObjectPool<T>` with a reset in `actionOnRelease`.

### Creation and ownership

- **WHEN** creating pools → **DO** go through an injected `IPoolFactory` / `PoolService` registered in VContainer. Gameplay code asks for a pool by prefab (`AssetReferenceGameObject` or config key), never builds one itself.
- **WHEN** a pool instantiates an object → **DO** use `IObjectResolver.Instantiate(prefab, container)` in `createFunc` so `[Inject]` methods run once at creation. Never use `Object.Instantiate` for pooled objects with dependencies.
- **WHEN** the prefab comes from Addressables → **DO** load it once via `IAssetProvider` when the pool is created and release the handle when the pool is disposed. Don't use `Addressables.InstantiateAsync` per spawn.
- **WHEN** choosing the pool's lifetime → **DO** match it to the scope that uses it: gameplay pools are registered in the scene `LifetimeScope` and disposed with the scene; only truly global pools (UI toasts, audio) live in the project scope.
- **WHEN** a pool is disposed → **DO** call `pool.Clear()` (destroys inactive objects) and make sure active objects are either returned or destroyed; no pooled object may outlive its scope.
- **WHEN** organizing the hierarchy → **DO** parent inactive pooled objects under one container per pool (`[Pool] Bullet`); don't leave them at the scene root.

### Configuration

- **WHEN** creating a pool → **DO** set `defaultCapacity` to the typical count and `maxSize` to a hard cap; objects released above `maxSize` are destroyed. Take both from a `ScriptableObject` pool config, not from magic numbers in code.
- **WHEN** constructing `ObjectPool<T>` → **DO** pass `collectionCheck: true` only in the Editor/Development builds (`Application.isEditor` or `#if UNITY_EDITOR || DEVELOPMENT_BUILD`) to catch double releases; disable it in release builds.
- **WHEN** spawns happen in bursts (wave start, explosion) → **DO** prewarm the pool during the loading state: create `defaultCapacity` objects in batches with `await UniTask.Yield(ct)` between batches so loading doesn't freeze.

### Spawn / despawn lifecycle

- **WHEN** a pooled object needs setup or cleanup → **DO** implement `IPoolable` with `OnSpawned()` and `OnDespawned()`; the pool calls them in `actionOnGet` / `actionOnRelease`.
- **WHEN** an object is taken from the pool → **DO** pass per-spawn data through an explicit method (`bullet.Launch(position, direction, damage)`), not via re-injection or public fields set afterward.
- **WHEN** an object returns to the pool → **DO** reset everything it changed in `OnDespawned()`:
  - transform (position, rotation, scale, parent if it was reparented);
  - `Rigidbody`: `linearVelocity` and `angularVelocity` to zero (Unity 6 API), `isKinematic` back to default;
  - `ParticleSystem.Clear()`, `TrailRenderer.Clear()`;
  - DOTween: kill its tweens (`DOTween.Kill(this)` / `transform.DOKill()`); `SetLink` doesn't help because pooled objects aren't destroyed;
  - UniRx: dispose the per-spawn `CompositeDisposable`;
  - UniTask: cancel and dispose the per-spawn `CancellationTokenSource`;
  - gameplay state (health, timers, flags, target references) and `Animator` (`Rebind()` / reset triggers).
- **WHEN** a pooled object holds references to other objects (target, owner) → **DO** clear them on despawn so they don't keep dead objects alive or act on stale targets.
- **WHEN** deactivating a pooled GameObject → **DO** use `SetActive(false)` in `actionOnRelease`.

### Pooling UI elements

- **WHEN** a UI element type spawns and despawns often → **DO** derive its view from `PooledElementView` and create an `ElementPool<T>(create, defaultCapacity, maxSize)` in the view that owns those elements; `create` builds the element (from UXML or code) and attaches the view.
- **WHEN** an element is released → **DO** let the pool remove it from the hierarchy and kill its tweens; in `OnDespawned()` reset everything the view changed (inline styles such as `translate`, `scale`, `opacity`, `left`/`top`, sprites, USS classes, callbacks). `PooledElementView.ResetTransform` resets the transform styles.
- **WHEN** an element is taken from the pool → **DO** pass per-spawn data through an explicit method (`item.Setup(jarId, sprite, isWrapped, onTap)`) and add it to its parent at that moment; a released element is never left in the tree hidden.
- **WHEN** the owning view is disposed (scene unload, screen closed) → **DO** release active elements and dispose the pool.

### Returning objects

- **WHEN** a pooled object's life ends → **DO** return it via its pool (`pool.Release(obj)` or `obj.Despawn()` that calls its owning pool). Never call `Destroy` on a pooled object.
- **WHEN** an object should return after a time (VFX, damage numbers) → **DO** use `await UniTask.Delay(duration, cancellationToken: spawnToken)` then release, or `ParticleSystem` `Stop Action = Callback` → release in `OnParticleSystemStopped`. Don't use `Invoke`/coroutines.
- **WHEN** an object can be released from several places (hit + lifetime timeout) → **DO** guard with an `_isActive` flag so it's released exactly once; double release corrupts the pool.
- **WHEN** code keeps a reference to a pooled object → **DO** treat it as invalid after release; store a spawn ID/version if another system needs to check that it's still the same instance.

### Reference skeleton

```csharp
public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}

public sealed class GameObjectPool<T> : IDisposable where T : Component
{
    private readonly ObjectPool<T> _pool;
    private readonly Transform _container;

    public GameObjectPool(IObjectResolver resolver, T prefab, Transform container,
        int defaultCapacity, int maxSize)
    {
        _container = container;
        _pool = new ObjectPool<T>(
            createFunc: () => resolver.Instantiate(prefab, _container),
            actionOnGet: item =>
            {
                item.gameObject.SetActive(true);
                if (item is IPoolable p) p.OnSpawned();
            },
            actionOnRelease: item =>
            {
                if (item is IPoolable p) p.OnDespawned();
                item.gameObject.SetActive(false);
                item.transform.SetParent(_container, false);
            },
            actionOnDestroy: item => { if (item != null) Object.Destroy(item.gameObject); },
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            collectionCheck: true,
#else
            collectionCheck: false,
#endif
            defaultCapacity: defaultCapacity,
            maxSize: maxSize);
    }

    public T Get() => _pool.Get();
    public void Release(T item) => _pool.Release(item);

    public async UniTask PrewarmAsync(int count, CancellationToken ct, int batchSize = 10)
    {
        var buffer = ListPool<T>.Get();
        try
        {
            for (int i = 0; i < count; i++)
            {
                buffer.Add(_pool.Get());
                if ((i + 1) % batchSize == 0) await UniTask.Yield(ct);
            }
        }
        finally
        {
            foreach (var item in buffer) _pool.Release(item);
            ListPool<T>.Release(buffer);
        }
    }

    public void Dispose() => _pool.Clear();
}

public sealed class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private TrailRenderer _trail;

    private GameObjectPool<Bullet> _owner;
    private CancellationTokenSource _lifetimeCts;
    private bool _isActive;

    public void SetOwner(GameObjectPool<Bullet> owner) => _owner = owner;

    public void Launch(Vector3 position, Vector3 velocity, float lifetime)
    {
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
        _rigidbody.linearVelocity = velocity;
        ReturnAfter(lifetime, _lifetimeCts.Token).Forget();
    }

    public void OnSpawned()
    {
        _isActive = true;
        _lifetimeCts = new CancellationTokenSource();
    }

    public void OnDespawned()
    {
        _isActive = false;
        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();
        _lifetimeCts = null;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _trail.Clear();
        transform.DOKill();
    }

    private void OnCollisionEnter(Collision _) => Despawn();

    private async UniTaskVoid ReturnAfter(float seconds, CancellationToken ct)
    {
        if (await UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: ct)
                .SuppressCancellationThrow()) return;
        Despawn();
    }

    private void Despawn()
    {
        if (!_isActive) return; // guard against double release
        _owner.Release(this);
    }
}
```

Note: create `GameObjectPool<T>` through a pool factory registered in the scene `LifetimeScope`; load the prefab via `IAssetProvider` before constructing the pool and release it when the pool is disposed. Verify the `ObjectPool<T>` constructor parameters and `Rigidbody.linearVelocity` against the installed Unity version.

---

## GRASP

- **WHEN** it's unclear which class should perform a task → **DO** apply **Information Expert**: assign the task to the class that has the required data (the class that knows the armor calculates damage).
- **WHEN** it's unclear who should create an object → **DO** apply **Creator**: the creator is whoever contains, aggregates or closely uses the created object (a spawner creates enemies).
- **WHEN** input or UI reaches directly into game logic → **DO** apply **Controller**: an intermediate class receives system events and delegates them to domain objects.
- **WHEN** changing one class forces changes in many others → **DO** apply **Low Coupling**: reduce dependencies with interfaces, events and DI.
- **WHEN** a class does many unrelated things → **DO** apply **High Cohesion**: split it into classes with one focused responsibility each.
- **WHEN** behavior differs by type and `switch (type)` keeps growing → **DO** apply **Polymorphism**: replace branching with subtypes or strategies.
- **WHEN** logic belongs to no domain object (saving, networking, analytics) → **DO** apply **Pure Fabrication**: create a service class (`SaveService`).
- **WHEN** two components must not depend on each other directly → **DO** apply **Indirection**: introduce an intermediary (interface, event, mediator).
- **WHEN** code breaks when an implementation is replaced (platform, SDK, data source) → **DO** apply **Protected Variations**: hide the point of variation behind a stable interface.

---

## SOLID

- **WHEN** a class changes for several different reasons, or a `MonoBehaviour` exceeds a few hundred lines → **DO** apply **S — Single Responsibility**: split it (e.g. `Player` → `PlayerMovement`, `PlayerHealth`, `PlayerInput`).
- **WHEN** a new feature requires editing working code and adding another `case` → **DO** apply **O — Open/Closed**: extend through new classes (inheritance, strategies, `ScriptableObject` configs) without modifying existing ones.
- **WHEN** a subclass throws `NotImplementedException` or breaks the base class's expectations → **DO** apply **L — Liskov Substitution**: fix the hierarchy so the subclass works wherever the base works; otherwise use composition.
- **WHEN** classes implement interface methods they don't need → **DO** apply **I — Interface Segregation**: split into narrow interfaces (`IDamageable`, `IInteractable`, `IMovable`).
- **WHEN** a class creates its own dependencies (`new`, `FindObjectOfType`, `.Instance`) → **DO** apply **D — Dependency Inversion**: depend on abstractions and receive them from outside (constructor, `[Inject]`, Inspector).

---

## DRY (Don't Repeat Yourself)

- **WHEN** the same logic is copied in 2–3 places → **DO** extract it into a method, base class, utility or component.
- **WHEN** code in two or more Views or windows is repeated or very similar (element queries, show/hide, close, spawning, pooling resets) → **DO** extract it into a parent class right away (`ElementView`, `View<TViewModel>`, `WindowView<TViewModel>`, `PooledElementView`, or a new intermediate base); for views the rule of three below does not apply.
- **WHEN** the same numbers and settings are scattered across code and prefabs → **DO** move them into a `ScriptableObject` config or constants.
- **WHEN** the same object setup is repeated manually → **DO** use prefabs and Prefab Variants for scene objects, and UXML templates (`<ui:Template>`/`<ui:Instance>`) plus shared USS classes for UI.
- **WHEN** code only looks similar but changes for different reasons → **DO NOT** merge it: this is not duplicated knowledge. Follow the rule of three: generalize on the third repetition.

---

## YAGNI (You Aren't Gonna Need It)

- **WHEN** tempted to build a system "for the future" or "just in case" → **DO NOT** build it. Implement only what the current task requires.
- **WHEN** an abstraction or interface has one implementation and no plans for a second → **DO** use the concrete class; introduce the abstraction when a second implementation or tests appear.
- **WHEN** adding configurability nobody asked for (10 Inspector parameters) → **DO** keep only what is actually used.
- **WHEN** choosing between "flexible" and "simple" → **DO** choose simple, with clean code that is easy to extend once the need is real.

---

## Game State Machine

- **WHEN** the game goes through global stages (Bootstrap → Loading → Menu → Gameplay → Pause → GameOver) → **DO** create a `GameStateMachine` where each stage is a state class with `Enter()` / `Exit()`.
- **WHEN** transition logic is spread across managers and flags (`isPaused`, `isLoading`) → **DO** centralize transitions via `stateMachine.Enter<LoadLevelState>()` and remove the flags.
- **WHEN** a state needs parameters (which level to load) → **DO** use payload states: `IPayloadedState<TPayload>` with `Enter(TPayload payload)`.
- **WHEN** a stage change requires loading a scene, showing a loading screen or initializing services → **DO** do it in the state's `Enter()`; put cleanup and unsubscribing in `Exit()`.
- **WHEN** a state must update every frame → **DO** add `IUpdatableState` with `Tick()`, called from a single `MonoBehaviour` runner.
- **WHEN** states need services → **DO** pass dependencies through a state factory or DI, not through singletons.
- **WHEN** a character's or AI's behavior also depends on mode → **DO** use a separate FSM per entity (the same State pattern); for complex AI, use a hierarchical FSM or a Behaviour Tree.

Reference skeleton:

```csharp
public interface IExitableState { void Exit(); }
public interface IState : IExitableState { void Enter(); }
public interface IPayloadedState<TPayload> : IExitableState { void Enter(TPayload payload); }
public interface IUpdatableState { void Tick(); }

public sealed class GameStateMachine
{
    private readonly Dictionary<Type, IExitableState> _states;
    private IExitableState _current;

    public GameStateMachine(IEnumerable<IExitableState> states) =>
        _states = states.ToDictionary(s => s.GetType());

    public void Enter<TState>() where TState : class, IState =>
        ChangeState<TState>().Enter();

    public void Enter<TState, TPayload>(TPayload payload)
        where TState : class, IPayloadedState<TPayload> =>
        ChangeState<TState>().Enter(payload);

    public void Tick() => (_current as IUpdatableState)?.Tick();

    private TState ChangeState<TState>() where TState : class, IExitableState
    {
        _current?.Exit();
        var state = (TState)_states[typeof(TState)];
        _current = state;
        return state;
    }
}
```
