# Unity Code Style Rules

Format: each rule is `WHEN <trigger> → DO <action>`. Base: the standard Microsoft C# coding conventions, with the project-specific member ordering below. Applies to all new and edited C# code. Unity 6 uses C# 9: don't use features from C# 10+ (file-scoped namespaces, global usings, record structs).

---

## Naming

- **WHEN** naming types, methods, properties, events, constants, enums and enum values, namespaces → **DO** use `PascalCase`.
- **WHEN** naming interfaces → **DO** prefix with `I` (`IAssetProvider`).
- **WHEN** naming generic type parameters → **DO** use `T` or `T` + a descriptive name (`TViewModel`, `TPayload`).
- **WHEN** naming private and protected fields (instance and static) → **DO** use `_camelCase` (`_health`, `_assetProvider`). This includes `[SerializeField] private` fields.
- **WHEN** naming parameters and local variables → **DO** use `camelCase`.
- **WHEN** naming constants → **DO** use `PascalCase` (`MaxEnemies`), not `UPPER_CASE`.
- **WHEN** naming `bool` members → **DO** use a question-like prefix: `Is`, `Has`, `Can`, `Should` (`IsAlive`, `CanBuy`).
- **WHEN** naming async methods returning `UniTask` → **DO** add the `Async` suffix (`LoadAsync`). Unity messages and state `Enter`/`Exit` are exceptions.
- **WHEN** naming events → **DO** use a verb or past-tense phrase (`Died`, `CoinsChanged`); name handlers `On` + event (`OnDied`).
- **WHEN** naming reactive members → **DO** name the property after the value (`Coins`, not `CoinsProperty` or `CoinsRx`).
- **WHEN** a name needs an abbreviation → **DO** avoid it unless it's widely known (`UI`, `Id`, `Hp` only if used across the project). Two-letter acronyms are uppercase (`UI`), longer ones PascalCase (`Json`, `Http`).

---

## Files and Namespaces

- **WHEN** creating a type (class, struct, interface, enum, delegate) → **DO** put exactly one type per file and name the file after the type (`ShopViewModel.cs`). Don't nest types inside other types and don't declare a second type in the same file, even a small payload struct or an enum used by one class; give it its own file next to its user.
- **WHEN** declaring a namespace → **DO** match the folder structure under the root code folder (`Game.Features.Shop`, `Game.Services.Save`). Use block-scoped namespaces (C# 9).
- **WHEN** adding `using` directives → **DO** place them at the top of the file, outside the namespace, `System` namespaces first, then others alphabetically; remove unused ones.

---

## Formatting

- **WHEN** formatting code → **DO** use 4 spaces, no tabs; Allman braces (opening brace on its own line); one statement per line; max line length ~120 characters.
- **WHEN** writing `if`, `else`, `for`, `foreach`, `while`, `do` → **DO** always use braces, even for a one-line body or a guard clause (`if (target == null) { return; }` written on three lines in Allman style).
- **WHEN** a method or property body is a single expression → **DO** an expression body (`=>`) is allowed; otherwise use a block body.
- **WHEN** declaring a local variable → **DO** use `var` when the type is obvious from the right side (`var list = new List<int>()`); use the explicit type otherwise.
- **WHEN** creating objects with an obvious target type → **DO** target-typed `new()` is allowed for fields (`private readonly List<Enemy> _enemies = new();`).

---

## Methods and Class Size

- **WHEN** three or more lines in a method share one responsibility (build a list, configure an element, compute a score, play one effect) → **DO** extract them into a private method whose name states that responsibility, so the caller reads as a list of steps.
- **WHEN** the same or a similar block of three or more lines appears in several places → **DO** extract one method (with parameters for the differences) and call it from each place.
- **WHEN** a UI class (view, window, screen, presenter, view model, UI service) grows past ~200 lines → **DO** decompose it into smaller classes with one responsibility each (child views per element group, a controller per flow, a builder for a layout), so each file stays under 200 lines where possible. Non-UI classes follow the same aim but may exceed it when splitting would hurt readability (e.g. the 1:1 prototype port in `Features/Gameplay/Model`).

---

## Modifiers and Declarations

- **WHEN** declaring any member → **DO** always write the access modifier explicitly (`private` too).
- **WHEN** writing modifiers → **DO** use the order: access, `static`, `abstract`/`virtual`/`override`/`sealed`, `readonly`, `async` (`private static readonly`, `public override async`).
- **WHEN** a class isn't designed for inheritance → **DO** mark it `sealed`.
- **WHEN** a field is assigned only in the constructor or initializer → **DO** mark it `readonly`.
- **WHEN** a field must appear in the Inspector → **DO** use `[SerializeField] private`, never a `public` field.
- **WHEN** state must be exposed → **DO** use a property (`public int Health { get; private set; }`); no public mutable fields.
- **WHEN** a numeric or string literal has meaning (damage, delays, keys) → **DO** move it to a `const`, `static readonly` field or a `ScriptableObject` config; no magic numbers.
- **WHEN** a value is a tuning parameter that design may change (rewards, prices, counts, limits, gameplay timings) → **DO** load it from a config (`ScriptableObject` or JSON in `Content/Data`) instead of hardcoding it in code, even as a `const`. Keep `const` for values that are part of the code's contract: element names, USS classes, address keys, animation constants and the values that must match the prototype 1:1.
- **WHEN** referring to members of the current instance → **DO NOT** use `this.` unless it's needed to resolve ambiguity.

---

## Member Ordering

Members of a type are ordered by **kind**, then by **access modifier**, then by **type category**.

### 1. By kind

1. Constants
2. Static fields
3. Instance fields
4. Events
5. Properties
6. Constructors (and `[Inject] Construct` method for `MonoBehaviour`)
7. Methods

All data members (constants, fields, events, properties) are above the constructors; all methods are below them. There are no nested types (see "Files and Namespaces").

### 2. By access modifier (inside each kind)

`public` → `internal` → `protected internal` → `protected` → `private protected` → `private`.

- **WHEN** ordering methods → **DO** put all `public` methods first, then `protected`, then `private`.
- **WHEN** a `MonoBehaviour` has Unity messages (`Awake`, `OnEnable`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`, `OnDisable`, `OnDestroy`, collision/trigger callbacks) → **DO** declare them `private` and put them at the start of the `private` methods group, in lifecycle order. If a Unity message is `protected virtual` (in a base class), it goes at the start of the `protected` group.
- **WHEN** ordering methods with the same access → **DO** keep a caller above the methods it calls; keep overloads together.

### 3. By type category (fields and properties, inside each access group)

1. **Class** (reference types): classes, interfaces, delegates, arrays, collections, `UnityEngine.Object` types (`Transform`, `GameObject`, prefabs, configs, `VisualTreeAsset`), UI Toolkit elements (`VisualElement`, `Label`, `Button`), UniRx types (`ReactiveProperty<T>`, `Subject<T>`, `CompositeDisposable`).
2. **Struct** (non-primitive value types): `Vector3`, `Quaternion`, `Color`, `LayerMask`, `CancellationToken`, `DateTime`, `TimeSpan`, custom structs.
3. **Primitive**: `bool`, `byte`, `short`, `int`, `long`, `float`, `double`, `decimal`, `char`, `string`, enums.

`string` is counted as primitive and enums are counted with primitives for ordering purposes, even though `string` is a reference type and enums are value types.

### 4. Additional rules

- **WHEN** a type has `[SerializeField] private` fields → **DO** put them as the first subgroup of the `private` instance fields, ordered by type category, followed by non-serialized private fields, also ordered by type category.
- **WHEN** fields share kind, access and type category → **DO** put `readonly` fields before mutable ones.
- **WHEN** a group is empty → **DO** skip it; don't add region comments or `#region` blocks to mark groups.
- **WHEN** editing an existing file → **DO** place new members in their correct group; don't reorder the rest of the file unless the task asks for it.

---

## Unity-specific

- **WHEN** checking a `UnityEngine.Object` for null → **DO** use `== null` / `!= null` or implicit bool (`if (target)`). **DO NOT** use `?.`, `??` or `is null` on `UnityEngine.Object`: they bypass Unity's destroyed-object check.
- **WHEN** subscribing to events or streams → **DO** pair every subscription with an unsubscription/dispose in the same class (`OnDisable`/`OnDestroy`/`Dispose`/`Unbind`).
- **WHEN** a `MonoBehaviour` requires components → **DO** reference them through `[SerializeField]` fields; use `[RequireComponent]` when a component must exist on the same GameObject.

### UI Toolkit (C#, UXML, USS)

- **WHEN** a view refers to an element by name → **DO** declare `private const string <Element>Name = "kebab-name";` (`PlayName = "play"`, `CoinsName = "coins-value"`) and query it once; never repeat the string literal.
- **WHEN** a view toggles a USS class → **DO** declare `private const string <Meaning>Class = "class-name";` (`LockedAlbumClass = "kit-btn-grey"`, `WarningClass = "toast--warning"`).
- **WHEN** naming elements in UXML → **DO** use kebab-case `name` attributes, and give a name only to elements the code queries.
- **WHEN** naming USS classes → **DO** use BEM-like kebab-case scoped to the feature: block `home-play`, element `home-play__title`, modifier `toast--warning`; shared kit classes start with `kit-` (`kit-btn-wood`), icons with `icon-` (`icon-coin`).
- **WHEN** writing USS → **DO** start each file with a one-line comment saying what it styles and which mockup it follows; sizes are in prototype CSS pixels (`px`); colors come from the shared variables in `Game.uss` (`var(--ink)`) instead of repeated literals.
- **WHEN** a UXML element is decoration or a container that isn't tapped → **DO** set `picking-mode="Ignore"` on it.

---

## Comments

- **WHEN** writing any comment (XML docs, inline, `TODO`) → **DO** write it in English.
- **WHEN** code needs explanation → **DO** explain *why*, not *what*; prefer clear names over comments.
- **WHEN** leaving unfinished work → **DO** use `// TODO:` with a short reason; don't leave commented-out code.

### Public API documentation

- **WHEN** declaring any `public` member → **DO** add an XML doc comment (`///`). This covers public types (classes, structs, interfaces, enums, delegates), constructors, methods, properties, events, constants and fields, and every member of a public interface. Enum values get a `<summary>` when their meaning isn't obvious from the name.
- **WHEN** documenting a member → **DO** always include `<summary>`: one or two sentences in the third person, present tense, ending with a period ("Loads the asset by key and caches its handle."). Describe what it does and any important side effect, not how it's implemented.
- **WHEN** a method or constructor has parameters → **DO** add `<param name="...">` for each one; for generic types and methods add `<typeparam name="...">`.
- **WHEN** a method returns a value → **DO** add `<returns>`; for `UniTask<T>` describe the result value ("The loaded asset.").
- **WHEN** a method throws exceptions callers should handle, or can be cancelled → **DO** add `<exception cref="...">` (include `OperationCanceledException` for methods taking a `CancellationToken`).
- **WHEN** usage has constraints (main thread only, must call `Dispose`, must be released to a pool, valid only after `Bind`) → **DO** state them in `<remarks>`.
- **WHEN** referring to other types or members → **DO** use `<see cref="..."/>` and `<paramref name="..."/>`, not plain text.
- **WHEN** a member overrides a base member or implements an interface member and the behavior is the same → **DO** use `/// <inheritdoc/>` instead of repeating the text; write a full comment only if the behavior differs.
### Comment content: context, not the obvious

- **WHEN** writing any comment → **DO** reveal the context the code alone doesn't show. For a method, cover whichever of these apply:
  - **when and by whom** it's called (lifecycle point, caller system, state machine state);
  - **why** it exists or why it's done this way (a design decision, a workaround, a platform quirk);
  - **preconditions** (what must be initialized, loaded or bound first; main thread only);
  - **side effects** (saves the profile, fires events, starts tweens, releases assets, changes global state);
  - **interaction** with other systems (which services it calls, what listens to its events);
  - **lifetime and ownership** (who disposes the result, whether it must be returned to a pool or released).
- **WHEN** a comment only restates the member name, the signature or the code → **DO NOT** write it. Obvious text is forbidden: "Gets the health.", "Sets the value.", "Constructor.", "The position.", "Returns true if alive.", `i++; // increment i`.
- **WHEN** a parameter or return value description would just repeat its name and type → **DO** describe its meaning instead: units, valid range, who provides it, what `null` or `default` means.
- **WHEN** a member really has nothing to add beyond its name → **DO** still keep the required `<summary>`, but write the one non-obvious fact (units, when it changes, who owns it) instead of rephrasing the name.
- **WHEN** an inline comment describes *what* a block does → **DO** extract the block into a well-named method instead; keep inline comments for *why*.

Examples:

| Bad (obvious) | Good (context) |
|---|---|
| `/// <summary>Gets the health.</summary>` | `/// <summary>Current health in hit points, from 0 to the configured maximum. Emits on every change; the HUD bar and death check subscribe to it.</summary>` |
| `/// <summary>Saves the profile.</summary>` | `/// <summary>Writes the current profile to disk on a background thread. Called by autosave every 30 s when dirty and on app pause; concurrent calls are merged into one write.</summary>` |
| `/// <param name="ct">Cancellation token.</param>` | `/// <param name="ct">Cancelled when the gameplay scene unloads; cancellation kills the death tween.</param>` |
| `/// <summary>Called when the object is spawned.</summary>` | `/// <inheritdoc/>` (behavior matches `IPoolable.OnSpawned`) |
- **WHEN** a `[SerializeField] private` field isn't self-explanatory in the Inspector → **DO** add `[Tooltip("...")]` in English; XML docs on private members are optional.
- **WHEN** public API changes (signature, behavior, thrown exceptions) → **DO** update its XML comment in the same change.

---

## Reference Example

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UniRx;
using UnityEngine;
using VContainer;

namespace Game.Features.Enemies
{
    /// <summary>
    /// Pooled enemy with health that plays a death animation and notifies listeners when it dies.
    /// </summary>
    /// <remarks>
    /// Instances are created by the enemy pool; return them to the pool instead of destroying them.
    /// </remarks>
    public sealed class Enemy : MonoBehaviour, IPoolable
    {
        // Constants
        /// <summary>
        /// Soft currency granted for killing an enemy when its config doesn't override the reward.
        /// </summary>
        public const int DefaultReward = 10;
        private const float DeathFadeDuration = 0.3f;

        // Static fields
        private static readonly int DieHash = Animator.StringToHash("Die");

        // Instance fields: private [SerializeField] → class, struct, primitive
        [SerializeField] private Animator _animator;
        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Vector3 _hitOffset;
        [Tooltip("Health in hit points restored on every spawn.")]
        [SerializeField] private float _maxHealth = 100f;

        // Instance fields: private → class, struct, primitive
        private readonly ReactiveProperty<float> _health = new();
        private readonly Subject<Enemy> _died = new();
        private IEnemyTargetProvider _targetProvider;
        private CancellationTokenSource _spawnCts;
        private Vector3 _spawnPosition;
        private bool _isActive;

        // Properties: public → class, primitive
        /// <summary>
        /// Emits this enemy once, after its death animation has finished.
        /// </summary>
        public IObservable<Enemy> Died => _died;

        /// <summary>
        /// Current health in hit points, from 0 to the configured maximum. Emits on every change.
        /// </summary>
        public IReadOnlyReactiveProperty<float> Health => _health;

        /// <summary>
        /// Becomes false as soon as health hits zero, while the death animation may still be playing;
        /// targeting systems use it to skip dying enemies.
        /// </summary>
        public bool IsAlive => _health.Value > 0f;

        // Constructor-like injection
        /// <summary>
        /// Receives dependencies from VContainer. Called once when the pool creates the instance.
        /// </summary>
        /// <param name="targetProvider">Provides the target the enemy moves toward.</param>
        [Inject]
        public void Construct(IEnemyTargetProvider targetProvider)
        {
            _targetProvider = targetProvider;
        }

        // Methods: public
        /// <summary>
        /// Moves a freshly pooled enemy to its spawn point before it becomes visible to the player.
        /// Called by the wave spawner right after taking the instance from the pool.
        /// </summary>
        /// <param name="position">Spawn point chosen by the wave spawner; expected to be on the NavMesh.</param>
        public void Spawn(Vector3 position)
        {
            _spawnPosition = position;
            transform.position = position;
        }

        /// <inheritdoc/>
        public void OnSpawned()
        {
            _isActive = true;
            _health.Value = _maxHealth;
            _spawnCts = new CancellationTokenSource();
        }

        /// <inheritdoc/>
        public void OnDespawned()
        {
            _isActive = false;
            _spawnCts?.Cancel();
            _spawnCts?.Dispose();
            _spawnCts = null;
            _rigidbody.linearVelocity = Vector3.zero;
            transform.DOKill();
        }

        /// <summary>
        /// Reduces health and starts the death sequence when it reaches zero.
        /// </summary>
        /// <param name="amount">Damage in hit points; must be non-negative.</param>
        /// <remarks>Ignored while the enemy is in the pool.</remarks>
        public void TakeDamage(float amount)
        {
            if (!_isActive)
            {
                return;
            }

            _health.Value = Mathf.Max(0f, _health.Value - amount);
            if (!IsAlive)
            {
                DieAsync(_spawnCts.Token).Forget();
            }
        }

        // Methods: private — Unity messages first, in lifecycle order
        private void OnDestroy()
        {
            _health.Dispose();
            _died.Dispose();
        }

        private async UniTaskVoid DieAsync(CancellationToken ct)
        {
            _animator.SetTrigger(DieHash);
            await transform.DOScale(0f, DeathFadeDuration).ToUniTask(TweenCancelBehaviour.Kill, ct);
            _died.OnNext(this);
        }
    }
}
```

Note: the group comments in the example (`// Constants`, `// Methods: public` …) are for illustration only; don't add them in real code. The reference skeletons in other rule files show structure, not this style; when turning them into real code, apply these style rules.
