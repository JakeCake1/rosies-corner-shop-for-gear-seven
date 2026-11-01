# Rosie's Corner Shop (Unity port)

Mobile sorting puzzle in a little grocery shop, ported from the Claude HTML prototype (Linear **EVG-7**) to
**Unity 6000.3.25f1** with VContainer, UniTask, UniRx, DOTween, Addressables and uGUI/TextMeshPro, following
`AGENTS.md` and `Docs/Code Rules/`.

## Open and run

1. Open the folder in Unity Hub (editor 6000.3.25f1).
2. First time only: **Tools → Rosie → 1. Import TMP Essentials**, then **Tools → Rosie → 2. Build Project**.
   The builder generates sprite import settings, TMP fonts, the item catalog and level curves (from the JSON in
   `Services/Content` and `Services/Levels`), all prefabs, the `Boot`, `Home` and `Gameplay` scenes, VContainer
   settings and Addressables groups (`Assets/Game/Bootstrap/Generated`, `Assets/Game/Bootstrap/Scenes`).
3. Open `Assets/Game/Bootstrap/Scenes/Boot.unity` and press Play (Game view 390×844 portrait recommended).
4. Code: open `rosies-corner-shop.sln` in Rider. Unity generates it (`com.unity.ide.rider`); if it is missing, use
   **Edit → Preferences → External Tools → Regenerate project files** (the `.sln`/`.csproj` files are git-ignored).

## Layout

| Folder | What |
|---|---|
| `Assets/Game/Features/Gameplay` | Level model (1:1 port of the prototype rules), session, board views, HUD, presenter, start boosters |
| `Assets/Game/Features/Home`, `Album`, `Settings`, `Reviewers`, `Ads` | Screens and dialogs |
| `Assets/Game/Services` | Assets, save/progress, content, levels, UI (windows, dialogs, overlay), audio, haptics, pooling, flow interfaces |
| `Assets/Game/Flow` | Game state machine: Bootstrap → LoadProgress → Home ⇄ Gameplay |
| `Assets/Game/Bootstrap` | VContainer scopes, entry point, editor builder and agent bridge |
| `Assets/Game/Tests/EditMode` | Rule parity with the prototype, rules, save, progress and config tests |
| `Packages/` | Embedded UniTask 2.5.11, UniRx 7.1.0 (patched for Unity 6.3: no `WWW`), VContainer 1.19.0, MCP for Unity 10.3.0 |
| `Tools/Langfuse` | Agent resource-consumption tracking and the Langfuse report |
| `Tools/Art` | Generator of the UI kit sprites the prototype drew with CSS |

## Notes and deviations

- DOTween 1.3.045 (free) lives in `Assets/Plugins/Demigiant` with a `DOTween.Modules` assembly definition.
- Audio is synthesized at startup (`SynthClips`), like the prototype's Web Audio synth; no audio files.
- Ads are a placeholder card (`FakeRewardedAdService`), as in the prototype.
- Item sprites are referenced directly by the addressable `ItemCatalog` (one always-loaded group) instead of 162
  `AssetReference`s.
- Kept prototype quirk: after level 9, "Continue" returns to the shop (album and naming milestones show there).
- `codebase-memory-mcp` indexes this repository for agents (`.mcp.json`); install it with the project's
  instructions (https://github.com/DeusData/codebase-memory-mcp) so the `codebase-memory-mcp` command is on PATH.
