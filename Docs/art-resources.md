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
| `png@4x/items/**`, `items/items.json` | `Services/Content/Items/` and `items.json`, referenced by `Services/Content/ItemCatalog.asset` (addressable `config/item-catalog`) |
| `png@4x/tools`, `basket`, `boosters`, `shelf` | `Features/Gameplay/Art/` |
| `png@4x/ui` | `Services/UI/Art/Icons/` |
| CSS surfaces (wood, awning, wallpaper, chips, buttons, ribbons, doilies) | 9-slice sprites in `Services/UI/Art/Kit/`, made by `Tools/Art/generate_ui_kit.py`; slicing is set in USS (`Kit.uss`, `-unity-slice-*`) |
| `layout/styles.css`, `layout/LAYOUT.md` | USS next to each layout (`Services/UI/Layouts/*.uss`, `Features/*/Layouts/*.uss`); colors as variables in `Game.uss` |
| Fonts (Fredoka, Yeseva One) | `Services/UI/Fonts/`, set with `-unity-font` in `Game.uss` (no TextMesh Pro) |
| `screens/*.png` | The reference for every layout: compare Play mode at 390×844 with the matching screenshot |

The UI is UI Toolkit with one `PanelSettings` at the 390×844 reference resolution, so the CSS pixel values from
`styles.css`, `LAYOUT.md` and the screenshots (which are @2x: divide by 2) go into USS unchanged. Scenes, layouts,
styles and configs are authored assets; there is no editor code that generates them.
