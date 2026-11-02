# Rosie's Corner Shop 2.0: art resources and layout

Extracted from the prototype (single-file HTML; all art is vector, drawn in code).

```
sprites/                SVG, standalone (gradients/patterns embedded)
  items/<family>/       162 grocery items, 40×64, file = <id>_<name>.svg
  items/items.json      id, name, family, shape, silhouette, color group, file
  items/_contact_sheet.png
  tools/                hammer, paper bag (hidden item)
  basket/               basket back / front / combined, "done" napkin
  boosters/             undo, pick (Quick Pick), tidy (Tidy Up), shuffle
  shelf/                key tag, tie seal, twine
  ui/                   coin, star on/off, gear, lock, tutorial hand, bunting
png@4x/                 the same sprites as PNG at 4× (items 160×256)
screens/                screenshots of every screen and main modals, 390×844 @2x
screens/elements/       crops: HUD, baskets, counter, cabinet, booster bar, shelf states
layout/LAYOUT.md        frame, sizes, formulas, color tokens, fonts
layout/styles.css       full stylesheet from the prototype
layout/layout_skeleton.html  static DOM skeleton
data/                   level tables (20-level and original 10-level curves)
source/index.html       original prototype file, as published
```

Shelf states (ice, boards, twine, second-row ghost) and wood/awning/wallpaper surfaces are CSS, not images. See styles.css and screens/elements.

## In the Unity project

| Source (elements.rar) | Project |
|---|---|
| `png@4x/items/**`, `items/items.json` | `Content/Visual/Items/` and `Content/Data/Items/items.json`, referenced by `Content/Data/Items/ItemCatalog.asset` (addressable `config/item-catalog`) through `AssetReference`s; the sprites live in per-level groups `Items-Level-NN` / `Items-Rest` (see below) |
| `png@4x/tools`, `basket`, `boosters`, `shelf` | `Content/Visual/Gameplay/` |
| `png@4x/ui` | `Content/Visual/UI/Icons/` |
| CSS surfaces (wood, awning, wallpaper, chips, buttons, ribbons, doilies) | 9-slice sprites in `Content/Visual/UI/Kit/`, made by `Tools/Art/generate_ui_kit.py`; slicing is set in USS (`Kit.uss`, `-unity-slice-*`) |
| `layout/styles.css`, `layout/LAYOUT.md` | USS next to each layout (`Content/Screens/Shared/*.uss`, `Content/Screens/<Feature>/*.uss`); colors as variables in `Game.uss` |
| Fonts (Fredoka, Yeseva One) | `Content/Visual/Fonts/`, set with `-unity-font` in `Game.uss` (no TextMesh Pro) |
| `screens/*.png` | The reference for every layout: compare Play mode at 390×844 with the matching screenshot |

The UI is UI Toolkit with one `PanelSettings` at the 390×844 reference resolution, so the CSS pixel values from
`styles.css`, `LAYOUT.md` and the screenshots (which are @2x: divide by 2) go into USS unchanged. Scenes, layouts,
styles and configs are authored assets; there is no editor code that generates them.

Sprite atlases (Sprite Atlas V2, `Content/Visual/Atlases/`): `UI` packs the shared kit and icons
(`Content/Visual/UI`), `Gameplay` packs the board art (`Content/Visual/Gameplay`). A sprite added to one of those
folders joins its atlas automatically. Item sprites are not atlased: they load per level from their own Addressables
groups, and an items atlas would pull every item into one bundle.

Item bundles: `Tools/Addressables/Rebuild Level Item Groups` plays the default curve as a fresh profile and puts each
item sprite into `Items-Level-NN`, NN being the first level that uses it; items no level uses go to `Items-Rest`. It
also rewrites the catalog's sprite references. Run it after changing the default curve, the catalog or the generator,
and commit the group and catalog assets. At runtime a level loads the sprites of the items it generated before it is
drawn (`IContentService.LoadItemSpritesAsync`); home and the album load the whole catalog.
