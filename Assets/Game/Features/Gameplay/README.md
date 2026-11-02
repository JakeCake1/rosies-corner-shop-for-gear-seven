# Gameplay

- **Owner:** Evgenii Popov (EVG-7); ported from the HTML prototype by the Claude agent.
- **Entry point:** `GameplayState` loads `scene/Gameplay`; `GameplayLifetimeScope` builds `LevelSession` and
  starts `GameplayPresenter`, which reads the level from `GameplayRequest`, generates it (`LevelSession.Prepare`),
  loads the sprites of its items (per-level Addressables groups) and starts it.
- **Structure:** `GameplayPresenter` only wires the parts: `Coach/` (tips, tutorial, idle hint), `Hud/` (HUD, booster
  bar, unlock gifts), `Offers/` (undo, booster and basket purchases), `Results/` (win and loss dialogs) share a
  `GameplayContext`. `Board/BoardView` orchestrates the counter, cabinet (shelves, stock tabs), basket ledge, item
  layer and rule-event effects. Tuning numbers come from `GameBalanceConfig` (`Content/Data/Balance`).
- **Dependencies:** wallet, album, level progress, onboarding, experiments (profile chunks), its own
  `Boosters/BoosterInventory` chunk (project scope), content catalog, level curves, UI/dialogs/overlay, audio, haptics, rewarded ads,
  settings screen, game flow (all through `Game.Services` interfaces).
- **Lifetime:** gameplay scene scope; disposed when the scene unloads (back home or next level).
- **Invariants:** rules live only in `Model/LevelRules` + `LevelGenerator` and must stay identical to the prototype
  (`PrototypeParityTests`); views never mutate `LevelState`; a basket ships only from `LevelSession`; two baskets
  never want the same type; the counter never exceeds its capacity.
