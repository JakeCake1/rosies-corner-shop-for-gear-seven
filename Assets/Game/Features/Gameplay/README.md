# Gameplay

- **Owner:** Evgenii Popov (EVG-7); ported from the HTML prototype by the Claude agent.
- **Entry point:** `GameplayState` loads `scene/Gameplay`; `GameplayLifetimeScope` builds `LevelSession` and
  starts `GameplayPresenter`, which reads the level from `GameplayRequest`.
- **Dependencies:** wallet, album, level progress, onboarding, experiments (profile chunks), its own
  `Boosters/BoosterInventory` chunk (project scope), content catalog, level curves, UI/dialogs/overlay, audio, haptics, rewarded ads,
  settings screen, game flow (all through `Game.Services` interfaces).
- **Lifetime:** gameplay scene scope; disposed when the scene unloads (back home or next level).
- **Invariants:** rules live only in `Model/LevelRules` + `LevelGenerator` and must stay identical to the prototype
  (`PrototypeParityTests`); views never mutate `LevelState`; a basket ships only from `LevelSession`; two baskets
  never want the same type; the counter never exceeds its capacity.
