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
