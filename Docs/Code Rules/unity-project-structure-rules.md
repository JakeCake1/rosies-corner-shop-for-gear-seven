# Unity Project Structure Rules

Format: each rule is `WHEN <trigger> → DO <action>`. Applies when creating files and folders, adding a feature or service, or moving code. This is the recommended layout for **new** code; it does not require renaming or moving existing folders.

---

## Top-level layout

```
Features/<Feature>/      — rules, state, UI logic, views and the factory of one feature
Services/<Capability>/   — reusable services (saves, audio, time, content loading, UI, pooling)
Bootstrap/Composition/   — composition root (VContainer lifetime scopes and registrations)
Flow/States/             — game flow states with explicit Enter/Exit
Tests/EditMode/, Tests/PlayMode/ — tests mirroring the production folders (see testing rules)
```

Example tree (names are illustrative; place it under the project's existing root code folder):

```
Bootstrap/
    Composition/
        ProjectLifetimeScope.cs
        GameplayLifetimeScope.cs
Flow/
    GameStateMachine.cs
    States/
        BootstrapState.cs
        LoadProgressState.cs
        MenuState.cs
        GameplayState.cs
Services/
    Assets/          — IAssetProvider, AssetProvider (Addressables)
    Save/            — ISaveStorage, FileSaveStorage, ProfileService
    Audio/
    Time/
    UI/              — IUIService, UIService, UIRoot, View/ViewModel/WindowView base classes
    Pooling/         — GameObjectPool<T>, IPoolFactory
Features/
    Shop/
        README.md            — feature card (large features only)
        ShopModel.cs
        ShopViewModel.cs
        ShopWindow.cs
        ShopFactory.cs
        Prefabs/
        Configs/
    Enemies/
        ...
```

---

## Where code goes

- **WHEN** code implements one feature's rules, state, UI logic, views or creation → **DO** put it in `Features/<Feature>/`. Name the feature folder in `PascalCase` after the domain concept (`Shop`, `Inventory`, `Enemies`), not after a technical layer.
- **WHEN** code is a capability several features can use (saving, audio, time, content loading, UI service, pooling, analytics) → **DO** put it in `Services/<Capability>/`, named after the capability, not the implementation (`Save`, not `JsonFileSaver`).
- **WHEN** code registers dependencies or builds containers (`LifetimeScope` subclasses, installers, entry point registration) → **DO** put it in `Bootstrap/Composition/`. Feature and service code never contains container setup beyond `[Inject]` methods.
- **WHEN** code is a game flow state (Bootstrap, LoadProgress, Menu, LoadLevel, Gameplay) → **DO** put it in `Flow/States/`, one state per file, each with explicit `Enter` / `Exit`. The state machine itself lives in `Flow/`.
- **WHEN** a feature has its own prefabs, configs (`ScriptableObject`), sprites or Addressables entries → **DO** keep them inside that feature's folder, so the feature can be found, moved or removed as a unit.
- **WHEN** a namespace is declared → **DO** match the folder (`Game.Features.Shop`, `Game.Services.Save`, `Game.Flow.States`, `Game.Bootstrap.Composition`), per the code style rules.

| What | Where |
|---|---|
| Model, ViewModel, View, window, factory of a feature | `Features/<Feature>/` |
| Feature-specific pool, config, prefab | `Features/<Feature>/` |
| `IAssetProvider`, `ProfileService`, `IUIService`, `GameObjectPool<T>` | `Services/<Capability>/` |
| `ProjectLifetimeScope`, scene `LifetimeScope`s | `Bootstrap/Composition/` |
| `GameStateMachine` | `Flow/` |
| `BootstrapState`, `MenuState`, `GameplayState` | `Flow/States/` |
| Unit and config tests | `Tests/EditMode/<same path as production>` |
| Integration tests (lifecycle, physics, scenes) | `Tests/PlayMode/<same path as production>` |

---

## Moving code to shared folders

- **WHEN** code is used by only one feature → **DO** keep it in that feature's folder, even if it "might be reused later". Don't create shared folders or services in advance (YAGNI).
- **WHEN** a second feature actually needs the same code → **DO** move it to `Services/<Capability>/` (or the project's existing shared folder) at that moment, together with updating its namespace and references.
- **WHEN** two features need to talk to each other → **DO** go through a service interface or an event in `Services/`, not a direct reference from one feature's folder into another's internals.

---

## Assembly definitions

- **WHEN** deciding whether to add an `.asmdef` → **DO** add one only at a real dependency boundary: code that must not reference something else (e.g. services must not reference features), Editor-only code, tests, or a large area whose compile time matters. Don't create one `.asmdef` per folder by default.
- **WHEN** relying on the folder layout to keep dependencies clean → **DO NOT**: folders don't enforce boundaries. If a boundary must hold, enforce it with an `.asmdef` and its references.
- **WHEN** an `.asmdef` exists → **DO** keep dependency direction one-way: `Bootstrap` → `Flow` → `Features` → `Services`. `Services` never reference `Features`; `Features` never reference `Bootstrap`.
- **WHEN** creating an `.asmdef` → **DO** reference the stack libraries explicitly, as listed in the tech stack rules.
- **WHEN** code is Editor-only (inspectors, tools, build scripts) → **DO** put it in an `Editor/` folder with its own Editor-platform `.asmdef`, inside the feature or service it belongs to.

---

## Feature card

- **WHEN** a feature is large (its own lifetime scope, several screens or systems, used by other features, or more than ~10 files) → **DO** add a short `README.md` card in its folder and keep it up to date when the feature changes.
- **WHEN** writing the card → **DO** keep it to these five fields, in English, a line or two each:

```markdown
# <Feature>

- **Owner:** who is responsible for this feature (person or team).
- **Entry point:** where the feature starts (state, window, scope, service method).
- **Dependencies:** services and other features it uses.
- **Lifetime:** when it is created and disposed (project scope, scene scope, per window).
- **Invariants:** rules that must always hold (e.g. "coins never go below zero", "only one shop window open").
```

- **WHEN** a feature is small → **DO NOT** add a card; the folder name and XML docs are enough.

---

## Existing projects

- **WHEN** the project already has a different structure → **DO** keep it. Put new code in the existing area that fits it; use this layout only for new top-level areas or when the user asks for a restructure.
- **WHEN** a task touches existing code → **DO NOT** move or rename folders as a side effect; propose a restructure separately instead.
