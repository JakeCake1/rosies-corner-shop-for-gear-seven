# Unity Project Rules for AI Agents

Read this file first. It explains which rule file to load for a task and which rule wins when files disagree. All rule files are in `Docs/Code Rules/`.

Stack: **Unity 6000.3.25f1**, **VContainer**, **UniTask**, **UniRx**, **DOTween**, **Addressables**, **UI Toolkit** (UXML/USS).

---

## Files

| File | Use when |
|---|---|
| `Docs/Code Rules/unity-tech-stack-rules.md` | Always. Defines the allowed libraries and how to use them. |
| `Docs/Code Rules/unity-code-style-rules.md` | Always when writing or editing C# code. Naming, formatting, member ordering, comments. |
| `Docs/Code Rules/unity-project-structure-rules.md` | Creating files or folders, adding a feature or service, moving code, adding `.asmdef`s. |
| `Docs/Code Rules/unity-testing-rules.md` | Adding or changing logic in `Features/` or `Services/`, adding or changing configs, fixing bugs, writing tests. |
| `Docs/Code Rules/unity-architecture-rules.md` | Designing classes, choosing patterns, refactoring, object pooling, game flow and state machines. |
| `Docs/Code Rules/unity-ui-mvvm-rules.md` | Any UI work: screens, windows, popups, HUD, lists, UI service, UXML/USS layouts and matching them to the mockups. |
| `Docs/Code Rules/unity-save-system-rules.md` | Saving or loading player data, profiles, settings, migrations, cloud saves. |
| `Docs/Code Rules/unity-csharp-optimization-rules.md` | Hot code (`Update`, per-frame logic), performance fixes, rendering, physics, assets, build settings. |

Any task that spawns or removes objects repeatedly (projectiles, VFX, enemies, list items) must follow the **Object Pooling** section of `unity-architecture-rules.md`.

Load `unity-tech-stack-rules.md` for every task and `unity-code-style-rules.md` for every task that touches C# code, plus the files whose topic the task touches. Do not load files the task doesn't need.

---

## Priority

When rules conflict, follow this order (higher wins):

1. The user's explicit instruction in the current task.
2. `unity-tech-stack-rules.md`.
3. The topic file for the task (UI, save system, optimization).
4. `unity-architecture-rules.md`.

The topic files were written to be stack-agnostic, so they mention alternatives. In this project, resolve them as follows:

| Generic mention in topic files | Use in this project |
|---|---|
| Zenject, Service Locator, Singleton | VContainer |
| `Task`, `Awaitable`, coroutines, `Task.Run` | UniTask (`UniTask`, `UniTask.RunOnThreadPool`, `SwitchToThreadPool`) |
| R3, custom `ObservableProperty<T>`, C# events for UI state | UniRx (`ReactiveProperty<T>`, `IReadOnlyReactiveProperty<T>`, `CompositeDisposable`) |
| `Animator` for UI transitions, tweens in general | DOTween |
| `Resources`, direct `Instantiate` of prefabs | Addressables via `IAssetProvider`, instantiate via `IObjectResolver` |
| uGUI, `Canvas`, TextMeshPro, `MonoBehaviour` views | UI Toolkit: UXML/USS + plain C# `ElementView`s |

Code style (`unity-code-style-rules.md`) doesn't conflict with other files: it governs how code is written, the others govern what is written. It applies to all C# code, including code adapted from skeletons.

Reference skeletons in the topic files show structure only. When writing real code, apply the code style rules and adapt them to the stack above (e.g. `Task` → `UniTask`, `ObservableProperty<T>` → `ReactiveProperty<T>`, `List<IDisposable>` → `CompositeDisposable`).

---

## Working Rules

- Apply a rule only when its trigger matches the current code or task (`WHEN <trigger> → DO <action>`).
- Prefer the simplest solution that solves the task (YAGNI). Don't add patterns, abstractions or packages the task doesn't need.
- Change only what the task asks; don't refactor unrelated code.
- Don't add new third-party packages without asking.
- Verify library API signatures against the installed package versions before relying on them.
- When a rule can't be followed (missing package, legacy code), say so and explain the deviation.

---

## This project (Rosie's Corner Shop)

- **Spec:** Linear EVG-7 attachment `Rosies_Corner_Shop_behavior.md`; the original HTML prototype is the reference
  for every rule. The level model (`Features/Gameplay/Model`) is a 1:1 port and is checked step by step against the
  prototype by `PrototypeParityTests` (fixture recorded in Node from the prototype's own functions).
- **Code index:** the `codebase-memory-mcp` MCP server (configured in `.mcp.json`) holds the code graph; it is
  refreshed automatically after commits and at session start (`Docs/agent-workflow.md`), never by hand. Before the
  first symbol search of a session, load its tools with a single `ToolSearch` call:
  `select:mcp__codebase-memory-mcp__search_graph,mcp__codebase-memory-mcp__trace_path,mcp__codebase-memory-mcp__get_code_snippet,mcp__codebase-memory-mcp__search_code,mcp__codebase-memory-mcp__detect_changes`.

  | WHEN | DO |
  |---|---|
  | Who calls or uses a method, class or interface | `trace_path` / `search_graph` |
  | Where something is registered in VContainer, who implements an interface | `search_graph` on relations |
  | Impact of a signature change | `trace_path` + `detect_changes` |
  | Find a symbol by name, path unknown | `search_graph`, then `get_code_snippet` instead of reading the whole file |
  | Package API (UniRx, VContainer, DOTween) | `search_code` on the index |
  | Text, USS/UXML, localization keys, assets | `grep` (not in the graph) |
- **UI and scenes are authored assets** (EVG-8): every non-code asset is under `Assets/Game/Content` (EVG-10):
  scenes in `Content/Scenes`, UXML/USS in `Content/Screens/<Feature>` and `Content/Screens/Shared`, configs and JSON in
  `Content/Data/<Service or Feature>`, sprites and fonts in `Content/Visual`, sounds in `Content/Sounds`, prefabs in
  `Content/Prefabs`. Move assets through the Editor (GUIDs stay). Edit them directly; there is no
  editor code that builds scenes or UI. USS sizes are prototype CSS pixels (panel reference 390×844), and every
  layout is checked against the mockups in `screens.rar`.
- **Localization:** all player-facing text is in the Unity Localization string tables `UI` and `Items`
  (`Content/Data/Localization`, service in `Services/Localization`); code uses keys from the feature `*Texts` classes through `ILocalizationService`, never
  string literals. How to add a text: `Docs/localization.md`.
- **Agent bridge:** `Game.Bootstrap.Editor.Agent.AgentBridge` logs every compilation to `AgentBridge/compile.log`
  and executes commands written to `AgentBridge/command.txt` (`refresh`, `invoke Type.Method`, ...). Unity MCP
  (`com.coplaydev.unity-mcp`, stdio transport) is the preferred way to drive the Editor.
- **Resource tracking:** `Tools/Workflow` measures agent token usage per implementation attempt and sends it to
  Langfuse (`Tools/Langfuse` is the older per-phase report, run by hand).
- **Implementation workflow:** every implementation attempt is tied to a Linear issue (`/impl-start`, `/handoff`);
  rules in `CLAUDE.md`, setup in `Docs/agent-workflow.md`, scripts in `Tools/Workflow`.
