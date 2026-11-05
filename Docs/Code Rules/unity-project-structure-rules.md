# Unity Project Structure Rules

Format: each rule is `WHEN <trigger> → DO <action>`. Applies when creating files and folders, adding a feature or service, or moving code. This is the recommended layout for **new** code; it does not require renaming or moving existing folders.

---

## Baseline for new projects

Moving code and assets after the fact is expensive: the EVG-10 restructure of Rosie's Corner Shop (976 files) was
the most expensive single task of that project. A new project starts in its final layout instead.

- **WHEN** starting a new Unity project on this stack → **DO** run
  `python Tools/ProjectTemplate/scaffold.py <project> --root <Root> --with-rules` from this repository before the
  first line of code. It creates the folders and assembly definitions below and copies `Docs/Code Rules/` and the
  generic part of `AGENTS.md`. It never overwrites existing files, so it is safe to re-run.
- **WHEN** the scaffold and this file disagree → **DO** follow this file and update `scaffold.py` in the same
  change (its test is `Tools/ProjectTemplate/tests/test_scaffold.py`).
- **WHEN** a folder of the baseline stays empty → **DO** keep it; it marks where that kind of code or asset goes.
  Add feature folders, service folders and `Content/<Kind>/<Feature>` folders only when the first file needs them.

Baseline tree, under `Assets/<Root>/` (Rosie's Corner Shop uses `Game`):

```
Bootstrap/                 <Root>.Bootstrap.asmdef
    Composition/           — ProjectLifetimeScope, scene LifetimeScopes
    Editor/<Tool>/         — Editor-only tools, one Editor .asmdef each (added with the first tool)
Flow/                      <Root>.Flow.asmdef — GameStateMachine
    States/                — one state per file
Features/                  <Root>.Features.asmdef — Features/<Feature>/ added per feature
Services/                  <Root>.Services.asmdef
    Assets/                — IAssetProvider (Addressables)
    Pooling/               — ElementPool<T>, GameObjectPool<T>
    Save/                  — save storage and profile
    UI/                    — IUIService, View / ViewModel / WindowView base classes
        Elements/          — ElementView, VisualElement helpers and tweens
Content/
    Data/                  — Data/<Service or Feature>/ configs and JSON
    Prefabs/
    Scenes/
    Screens/Shared/        — Root.uxml, Game.uss, Kit.uss, PanelSettings; Screens/<Feature>/ per feature
    Sounds/
    Visual/Atlases/, Visual/Fonts/, Visual/UI/ — plus Visual/<Feature>/ per feature
Tests/
    EditMode/              <Root>.Tests.EditMode.asmdef — Features/, Flow/, Services/ mirror production
```

Assembly definitions (all `autoReferenced: false`):

| Assembly | References | Notes |
|---|---|---|
| `<Root>.Services` | stack only | never references Features, Flow or Bootstrap |
| `<Root>.Features` | Services + stack | |
| `<Root>.Flow` | Features, Services + stack | |
| `<Root>.Bootstrap` | Flow, Features, Services + stack | composition root, references everything |
| `<Root>.Tests.EditMode` | Services, Features, Flow + stack + test runner | Editor only, `UNITY_INCLUDE_TESTS`, `nunit.framework.dll` |
| `<Root>.Bootstrap.Editor.<Tool>` | what the tool needs | Editor only, one per tool |

Stack references: `UniTask`, `UniTask.Addressables`, `UniTask.DOTween`, `UniRx`, `VContainer`, `DOTween.Modules`,
`Unity.Addressables`, `Unity.ResourceManager`. Add a package assembly (`Unity.Localization`,
`Newtonsoft.Json.dll`, ...) to the assemblies that use it when the package is added.

Outside `Assets/<Root>/`, only what packages require: `Assets/Plugins/` (DOTween), `Assets/Resources/` for
`DOTweenSettings` only, `Assets/AddressableAssetsData/`.

---

## Top-level layout

```
Features/<Feature>/      — rules, state, UI logic, views and the factory of one feature
Services/<Capability>/   — reusable services (saves, audio, time, content loading, UI, pooling)
Bootstrap/Composition/   — composition root (VContainer lifetime scopes and registrations)
Flow/States/             — game flow states with explicit Enter/Exit
Content/                 — every non-code asset (see "Content" below); code folders hold only .cs, .asmdef and README.md cards
Tests/EditMode/, Tests/PlayMode/ — tests mirroring the production folders (see testing rules)
```

### Content

All non-code assets live under `Content/`, grouped by kind, then by the feature or service that uses them:

```
Content/
    Sounds/              — audio clips and the audio library config
    Visual/
        UI/Kit, UI/Icons — shared UI sprites
        Fonts/
        <Feature>/       — feature sprites (Gameplay/basket, Gameplay/shelf, ...)
        Items/<group>/   — item sprites
        Atlases/         — sprite atlases (see "Sprite atlases" in the optimization rules)
    Data/
        <Service or Feature>/ — ScriptableObject configs and their JSON sources (Items, Levels, Gameplay, Localization, Settings)
    Scenes/              — authored scenes (Boot, Home, Gameplay); never generated by editor code
    Screens/
        Shared/          — Root.uxml, shared USS (Game.uss, Kit.uss), theme, PanelSettings, shared windows
        <Feature>/       — the feature's UXML and USS
    Prefabs/             — prefabs (lifetime scopes, UI root)
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
        Elements/    — ElementView, VisualElement helpers and tweens
    Pooling/         — ElementPool<T>, PooledElementView (UI elements), GameObjectPool<T> (GameObjects)
Features/
    Shop/
        README.md            — feature card (large features only)
        ShopModel.cs
        ShopFactory.cs
        Window/              — ShopViewModel.cs, ShopWindow.cs and its child views
Content/
    Screens/Shop/            — ShopWindow.uxml (address "ui/ShopWindow"), Shop.uss
    Data/Shop/               — ShopConfig.asset
    Enemies/
        ...
```

---

## Where code goes

- **WHEN** code implements one feature's rules, state, UI logic, views or creation → **DO** put it in `Features/<Feature>/`. Name the feature folder in `PascalCase` after the domain concept (`Shop`, `Inventory`, `Enemies`), not after a technical layer.
- **WHEN** code is a capability several features can use (saving, audio, time, content loading, UI service, pooling, analytics) → **DO** put it in `Services/<Capability>/`, named after the capability, not the implementation (`Save`, not `JsonFileSaver`).
- **WHEN** code registers dependencies or builds containers (`LifetimeScope` subclasses, installers, entry point registration) → **DO** put it in `Bootstrap/Composition/`. Feature and service code never contains container setup beyond `[Inject]` methods.
- **WHEN** code is a game flow state (Bootstrap, LoadProgress, Menu, LoadLevel, Gameplay) → **DO** put it in `Flow/States/`, one state per file, each with explicit `Enter` / `Exit`. The state machine itself lives in `Flow/`.
- **WHEN** adding a non-code asset (layout, sprite, sound, config, JSON, scene, prefab, localization table) → **DO** put it under `Content/<Kind>/<Feature or Service>/` (see "Content"), never next to the code. Shared UI styles and the root layout live in `Content/Screens/Shared/`.
- **WHEN** moving or renaming an asset → **DO** move it through the Unity Editor (`AssetDatabase.MoveAsset` or the Project window) so its GUID, Addressables entry and references survive; then update path-based references by hand (USS `url("project://database/...")`, address strings that are paths).
- **WHEN** a feature or service folder holds more than a handful of classes → **DO** group them into subfolders by responsibility (`Board/`, `Hud/`, `Session/`, `Dialogs/`, `Elements/`), not by technical kind (`Views/`, `Interfaces/`). Classes that change together live together.
- **WHEN** a scene, layout, prefab or config is needed → **DO** author it as an asset and commit it. Don't add editor code that generates scenes, UI, prefabs or configs from code; edit the assets directly.
- **WHEN** a namespace is declared → **DO** match the folder (`Game.Features.Shop`, `Game.Services.Save`, `Game.Flow.States`, `Game.Bootstrap.Composition`), per the code style rules.

| What | Where |
|---|---|
| Model, ViewModel, View, window, factory of a feature | `Features/<Feature>/` |
| Feature-specific pool | `Features/<Feature>/` |
| Feature config, JSON data | `Content/Data/<Feature>/` |
| Feature UXML and USS | `Content/Screens/<Feature>/` |
| `Root.uxml`, shared USS, `PanelSettings` | `Content/Screens/Shared/` |
| Sprites, fonts, atlases | `Content/Visual/` |
| Sounds | `Content/Sounds/` |
| Prefabs | `Content/Prefabs/` |
| `IAssetProvider`, `ProfileService`, `IUIService`, `ElementPool<T>` | `Services/<Capability>/` |
| Scenes | `Content/Scenes/` |
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
- **WHEN** code is Editor-only (inspectors, tools, build scripts, agent tooling) → **DO** put it in an `Editor/` folder with its own Editor-platform `.asmdef`, inside the feature or service it belongs to. Editor code must not build scenes or UI (see "Where code goes").

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
