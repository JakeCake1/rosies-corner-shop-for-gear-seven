# Audio

The prototype has no audio files: it synthesizes every sound with Web Audio. EVG-9 replaced the C# approximation of
that synth with WAVs rendered from the prototype's own code (`Rosies_Corner_Shop_sounds.zip` on Linear EVG-9, see its
`SOUNDS.md`).

## Assets

- `Assets/Game/Services/Audio/Clips/`: the archive's `game_mix` set (44.1 kHz, 16-bit, mono). All clips share one
  gain, so the prototype's relative loudness is baked in and every clip plays at volume 1. `tap_unused` is left
  out: the prototype never plays it.
- Import settings: effects are ADPCM, *Decompress On Load* (no latency on tap); `music_loop` is Vorbis,
  *Compressed In Memory*, loaded in the background. The music loops seamlessly at 9.28 s.
- `Assets/Game/Services/Audio/AudioLibrary.asset` (`AudioLibrary`, Addressables key `config/audio`, group
  *AlwaysLoaded*): one clip per `SoundId`, seven `basket_ship_combo0…6` clips and the music.
  `AudioLibrary.Validate` checks that every `SoundId` has exactly one clip, there are seven ship clips and the
  music is set; `ConfigTests` runs it.

## Runtime

`AudioService` (project scope) owns a persistent `[Audio]` object with the only `AudioListener`, eight round-robin
effect voices and the music source. `BootstrapState` loads the library together with the item catalog; sounds
requested earlier are skipped. `LoadProgressState` starts the music (`ApplySettings`), the Settings screen toggles
it, and `AppLifecycleListener` pauses it in the background.

## Events → sounds

| Moment | Call | Where |
|---|---|---|
| Tap on an item | `PlayTap(silhouette)` → `TapSounds.For` (`box`, `can`, `bottle`, `stick`, otherwise `round`) | `LevelSession.Tap` |
| Tap on the hammer or a paper bag | `SoundId.TapRound` | `LevelSession.Tap` |
| Item lands in a basket / on the counter | `Clink` / `Counter` when its flight ends (not for items sliding along the counter) | `BoardView.Fly` |
| Basket ships | `PlayShip(combo)`: combo step 0…6, later steps reuse step 6 | `LevelSession.ShipAsync` |
| Empty shelf folds away | `Fold` | `BoardView.RenderShelves` |
| New shelf drops in | `Roll` when its drop starts; level start plays one `Roll` from `LevelSession.Start` | `BoardView.RenderShelves` |
| Key reaches the twine | `Snip` | `BoardView.FlyKey` |
| Hammer knocks the boards off | `Snip` + `Fold` | `BoardView.PlayEvents` |
| Booster, undo, extra basket, shelf thaws | `Magic` | `LevelSession` |
| Coins spent or earned, win-screen stars | `Coin` | `GameplayPresenter`, `LevelSession`, `StartBoostersPrompt` |
| Level won, album intro, shop renamed | `Win` | `LevelSession`, `AlbumScreen`, `ShopNamePrompt` |
| Level lost, tap refused (counter full) | `Fail` | `LevelSession` |

Sounds of the move itself are played by `LevelSession`; sounds tied to an animation moment are played by
`BoardView`, so they stay in sync with what the player sees.

## Adding a sound

1. Put the WAV in `Services/Audio/Clips/` with the import settings above.
2. Add a `SoundId` value with a `<summary>` saying when it plays.
3. Add an entry for it to `AudioLibrary.asset` (the config test fails until you do).
4. Call `IAudioService.Play(SoundId.X)` from the code that owns the moment, and add a row to the table above.
