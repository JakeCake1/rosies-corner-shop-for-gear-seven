#!/usr/bin/env python3
"""Renders the UI kit sprites that the HTML prototype drew with CSS (wood, awning, wallpaper, buttons...).

Colors come from the prototype's :root tokens (see _inputs/elements/layout/LAYOUT.md). Sprites are drawn at 2x
(one prototype CSS pixel = 2 texture pixels) and written with a sidecar `slices.json` that lists 9-slice borders;
the Unity builder applies those borders on import.

Usage: python3 generate_ui_kit.py <output folder>
"""
import json
import math
import os
import sys

from PIL import Image, ImageDraw, ImageFilter

S = 2  # supersampling relative to CSS px
AA = 4  # extra anti-aliasing factor while drawing

T = {
    "wall": "#F4E8D0", "wall2": "#EBDBBC", "wall3": "#E2CFAA",
    "sage": "#A9BC96", "sage_d": "#7D9469", "sage_dd": "#5F7650",
    "oak_ll": "#F3D2A0", "oak_l": "#E7B77C", "oak": "#CD955A", "oak_d": "#96622F", "oak_dd": "#6B4320",
    "walnut": "#5E3C20", "walnut_d": "#3F2714",
    "berry": "#C63D4E", "berry_d": "#962B3A", "berry_dd": "#6E1D29",
    "leaf": "#5F9A46", "leaf_d": "#437232",
    "gold": "#EDB53F", "gold_d": "#B8831E",
    "cream": "#FFF8EA", "paper": "#F8ECD2",
    "ink": "#3B2A1B", "ink2": "#7A6049",
    "awning": "#D9534F", "awning_w": "#FFF6E8",
    "ice": "#BFE3FF", "grey": "#B9AFA2", "grey_d": "#8C8276",
}


def rgba(hex_, a=255):
    h = hex_.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def mix(c1, c2, t):
    return tuple(round(a + (b - a) * t) for a, b in zip(c1, c2))


def shade(hex_, amt):
    c = rgba(hex_)
    if amt < 0:
        return tuple(round(v * (1 + amt)) for v in c[:3]) + (255,)
    return tuple(round(v + (255 - v) * amt) for v in c[:3]) + (255,)


class Canvas:
    """Draws at AA x resolution and downsamples on save for smooth edges."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.img = Image.new("RGBA", (w * AA, h * AA), (0, 0, 0, 0))

    def _layer(self):
        layer = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        return layer, ImageDraw.Draw(layer)

    def _commit(self, layer):
        self.img = Image.alpha_composite(self.img, layer)

    def rr(self, box, r, fill, outline=None, width=0):
        layer, d = self._layer()
        x0, y0, x1, y1 = [v * AA for v in box]
        d.rounded_rectangle((x0, y0, x1 - 1, y1 - 1), radius=r * AA, fill=fill, outline=outline, width=width * AA)
        self._commit(layer)

    def ellipse(self, box, fill, outline=None, width=0):
        layer, d = self._layer()
        x0, y0, x1, y1 = [v * AA for v in box]
        d.ellipse((x0, y0, x1 - 1, y1 - 1), fill=fill, outline=outline, width=width * AA)
        self._commit(layer)

    def poly(self, pts, fill):
        layer, d = self._layer()
        d.polygon([(x * AA, y * AA) for x, y in pts], fill=fill)
        self._commit(layer)

    def line(self, pts, fill, width):
        layer, d = self._layer()
        d.line([(x * AA, y * AA) for x, y in pts], fill=fill, width=max(1, int(width * AA)))
        self._commit(layer)

    def vgrad_rr(self, box, r, top, bottom):
        """Rounded rect with a vertical gradient fill."""
        x0, y0, x1, y1 = box
        grad = Image.new("RGBA", (self.w * AA, self.h * AA), (0, 0, 0, 0))
        gd = ImageDraw.Draw(grad)
        for y in range(int(y0 * AA), int(y1 * AA)):
            t = (y - y0 * AA) / max(1, (y1 - y0) * AA - 1)
            gd.line([(x0 * AA, y), (x1 * AA, y)], fill=mix(top, bottom, t))
        mask = Image.new("L", grad.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle((x0 * AA, y0 * AA, x1 * AA - 1, y1 * AA - 1), radius=r * AA, fill=255)
        layer = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        layer.paste(grad, (0, 0), mask)
        self._commit(layer)

    def save(self, path):
        self.img.resize((self.w, self.h), Image.LANCZOS).save(path)


SLICES = {}


def out(name, canvas, border=None, folder=""):
    path = os.path.join(OUT, folder, name + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    canvas.save(path)
    if border is not None:
        SLICES[(folder + "/" if folder else "") + name + ".png"] = border  # left, bottom, right, top


def pressable(name, face, face_d, lip, r=18):
    """3D pill button: face gradient, darker lip below (pressed state shifts down in code)."""
    w, h, lipH = 60 * S, 60 * S, 5 * S
    c = Canvas(w, h)
    c.rr((0, lipH, w, h), r * S, lip)
    c.vgrad_rr((0, 0, w, h - lipH), r * S, shade(face, .12), rgba(face_d))
    c.rr((3 * S, 2 * S, w - 3 * S, 10 * S), 6 * S, (255, 255, 255, 46))
    b = r * S + 2
    out(name, c, [b, b + lipH, b, b])


def build():
    # --- buttons
    pressable("btn_berry", T["berry"], T["berry_d"], rgba(T["berry_dd"]))
    pressable("btn_green", T["leaf"], T["leaf_d"], shade(T["leaf_d"], -.3))
    pressable("btn_wood", T["oak_l"], T["oak"], rgba(T["oak_d"]))
    pressable("btn_gold", T["gold"], T["gold_d"], shade(T["gold_d"], -.3))
    pressable("btn_grey", T["grey"], T["grey_d"], shade(T["grey_d"], -.35))

    # --- card (modal body) and chip
    c = Canvas(120 * S // 2 * 2, 120 * S // 2 * 2)
    c.rr((0, 2 * S, c.w, c.h), 26 * S, (90, 60, 30, 60))
    c.rr((0, 0, c.w, c.h - 2 * S), 26 * S, rgba(T["cream"]), rgba("#EAD7B4"), S)
    out("card", c, [26 * S + 2] * 4)

    c = Canvas(40 * S, 32 * S)
    c.rr((0, 2 * S, c.w, c.h), 16 * S, (120, 80, 40, 50))
    c.rr((0, 0, c.w, c.h - 2 * S), 16 * S, rgba(T["cream"]), rgba("#E6D2AE"), S)
    out("chip", c, [16 * S] * 4)

    c = Canvas(24 * S, 24 * S)
    c.rr((0, 0, c.w, c.h), 8 * S, (255, 255, 255, 255))
    out("rounded_white", c, [8 * S] * 4)

    c = Canvas(64, 64)
    c.ellipse((0, 0, 64, 64), (255, 255, 255, 255))
    out("circle", c)

    # soft drop shadow ellipse (under items, baskets)
    img = Image.new("RGBA", (64, 24), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse((6, 6, 58, 18), fill=(60, 35, 15, 110))
    img = img.filter(ImageFilter.GaussianBlur(3))
    img.save(os.path.join(OUT, "shadow.png"))

    # --- ribbon title (berry / green), notched ends, sliced horizontally
    for name, face, dark in (("ribbon_berry", T["berry"], T["berry_dd"]), ("ribbon_green", T["leaf"], shade(T["leaf_d"], -.3)[:3])):
        dark_c = rgba(dark) if isinstance(dark, str) else dark + (255,)
        w, h = 120 * S, 40 * S
        c = Canvas(w, h)
        n = 12 * S
        c.poly([(0, 6 * S), (n + 8 * S, 6 * S), (n + 8 * S, h), (0, h), (n * .6, (h + 6 * S) / 2)], dark_c)
        c.poly([(w, 6 * S), (w - n - 8 * S, 6 * S), (w - n - 8 * S, h), (w, h), (w - n * .6, (h + 6 * S) / 2)], dark_c)
        c.vgrad_rr((n, 0, w - n, h - 6 * S), 6 * S, shade(face, .1), rgba(face) if isinstance(face, str) else face)
        out(name, c, [n + 10 * S, 8 * S, n + 10 * S, 8 * S])

    # --- wood board (counter, sign, showcase plank)
    def wood_board(name, top, bottom, edge, r, w=80 * S, h=80 * S, grain=True):
        c = Canvas(w, h)
        c.rr((0, 4 * S, w, h), r * S, rgba(edge))
        c.vgrad_rr((0, 0, w, h - 4 * S), r * S, rgba(top), rgba(bottom))
        if grain:
            for i in range(1, 6):
                y = (h - 4 * S) * i / 6
                c.line([(r * S, y), (w - r * S, y + S)], (120, 70, 30, 26), S)
        b = r * S + 4
        out(name, c, [b, b + 4 * S, b, b])

    wood_board("board_oak", T["oak_ll"], T["oak_l"], T["oak"], 16)
    wood_board("board_walnut", "#7A5230", T["walnut"], T["walnut_d"], 14)
    wood_board("plank", T["oak_l"], T["oak"], T["oak_d"], 4, 80 * S, 22 * S, grain=False)

    # shelf parts: floor plank, side post, top cap
    c = Canvas(40 * S, 8 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 2 * S, rgba("#F6D6A2"), rgba("#9C6532"))
    c.line([(0, 2 * S), (c.w, 2 * S)], rgba("#C98E52"), S)
    out("shelf_floor", c, [4 * S, 3 * S, 4 * S, 3 * S])
    c = Canvas(5 * S, 40 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 1 * S, rgba(T["oak_l"]), rgba(T["oak"]))
    out("shelf_post", c, [2, 4 * S, 2, 4 * S])
    c = Canvas(40 * S, 40 * S)
    c.rr((0, 0, c.w, c.h), 3 * S, rgba("#F7E6C8"))
    for i in range(1, 5):
        c.line([(c.w * i / 5, 0), (c.w * i / 5, c.h)], (200, 160, 110, 40), S)
    out("shelf_back", c, [4 * S] * 4)

    # cabinet frame (back panel + oak frame)
    c = Canvas(80 * S, 80 * S)
    c.rr((0, 4 * S, c.w, c.h), 14 * S, rgba(T["oak_d"]))
    c.rr((0, 0, c.w, c.h - 4 * S), 14 * S, rgba(T["oak"]))
    c.rr((7 * S, 7 * S, c.w - 7 * S, c.h - 11 * S), 9 * S, rgba("#EBD3AC"))
    out("cabinet", c, [18 * S, 20 * S, 18 * S, 18 * S])

    # awning: red/cream stripes with scalloped bottom; one stripe pair repeats (tiled)
    stripe = 26 * S
    w, h = stripe * 2, 38 * S
    c = Canvas(w, h)
    for i, col in enumerate((T["awning"], T["awning_w"])):
        x0 = i * stripe
        c.vgrad_rr((x0, 0, x0 + stripe, 26 * S), 0, shade(col, .08), rgba(col) if col != T["awning"] else shade(col, -.08))
        c.ellipse((x0, 26 * S - stripe / 2, x0 + stripe, 26 * S + stripe / 2), rgba(col) if col != T["awning"] else shade(col, -.08))
    out("awning", c)

    # wallpaper: cream with faint vertical stripes and dots (tiled)
    w, h = 36 * S, 36 * S
    c = Canvas(w, h)
    c.rr((0, 0, w, h), 0, rgba(T["wall"]))
    c.rr((0, 0, 6 * S, h), 0, (235, 219, 188, 90))
    for (x, y) in ((18, 9), (0, 27), (36, 27)):
        c.ellipse(((x - 3) * S, (y - 3) * S, (x + 3) * S, (y + 3) * S), (226, 207, 170, 200))
    out("wallpaper", c)

    # wainscot: sage panels with grooves (tiled horizontally), top rail
    w, h = 42 * S, 60 * S
    c = Canvas(w, h)
    c.rr((0, 0, w, h), 0, rgba(T["sage"]))
    c.line([(w - S, 0), (w - S, h)], rgba(T["sage_d"]), 2 * S)
    out("wainscot", c)
    c = Canvas(40 * S, 10 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 0, rgba(T["oak_l"]), rgba(T["oak_d"]))
    out("wainscot_rail", c, [2, 2, 2, 2])

    # progress bar track (walnut) and fill (berry), level badges
    c = Canvas(40 * S, 30 * S)
    c.rr((0, 0, c.w, c.h), 15 * S, rgba(T["walnut"]))
    c.rr((0, 0, c.w, 6 * S), 3 * S, (0, 0, 0, 40))
    out("bar_track", c, [15 * S] * 4)
    c = Canvas(30 * S, 24 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 12 * S, shade(T["berry"], .2), rgba(T["berry_d"]))
    out("bar_fill", c, [12 * S] * 4)
    for name, face, rim in (("badge_gold", T["gold"], T["gold_d"]), ("badge_red", T["berry"], T["berry_dd"])):
        c = Canvas(52 * S, 54 * S)
        c.ellipse((0, 2 * S, 52 * S, 54 * S), rgba(rim))
        c.ellipse((0, 0, 52 * S, 52 * S), shade(face, -.05))
        c.ellipse((4 * S, 4 * S, 48 * S, 48 * S), shade(face, .15))
        out(name, c)

    # counter slot doily (lace ring) and red "hot" variant
    for name, ring in (("doily", (255, 255, 255, 235)), ("doily_hot", rgba("#F3B7B7"))):
        c = Canvas(56 * S, 20 * S)
        c.ellipse((0, 0, c.w, c.h), ring)
        c.ellipse((4 * S, 3 * S, c.w - 4 * S, c.h - 3 * S), rgba("#E9D3AE"))
        c.ellipse((7 * S, 5 * S, c.w - 7 * S, c.h - 5 * S), (255, 252, 244, 255))
        out(name, c)

    # basket ledge with lace edge (tiled horizontally)
    c = Canvas(20 * S, 22 * S)
    c.vgrad_rr((0, 0, c.w, 14 * S), 0, rgba(T["oak_l"]), rgba(T["oak"]))
    c.ellipse((0, 10 * S, 20 * S, 22 * S), (255, 255, 255, 255))
    out("ledge", c)

    # frozen overlay, boards, gift halo
    c = Canvas(40 * S, 40 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 6 * S, (225, 243, 255, 205), (150, 200, 240, 205))
    c.rr((0, 0, c.w, c.h), 6 * S, None, (255, 255, 255, 255), S)
    c.line([(6 * S, 30 * S), (16 * S, 10 * S)], (255, 255, 255, 140), 2 * S)
    out("ice", c, [7 * S] * 4)
    c = Canvas(60 * S, 14 * S)
    c.vgrad_rr((0, 0, c.w, c.h), 3 * S, rgba("#C8915A"), rgba("#8E5A2C"))
    for x in (5, 55):
        c.ellipse(((x - 2) * S, 5 * S, (x + 2) * S, 9 * S), rgba("#5B5B5B"))
    out("board", c, [9 * S, 4 * S, 9 * S, 4 * S])

    # toggle switch
    c = Canvas(52 * S, 30 * S)
    c.rr((0, 0, c.w, c.h), 15 * S, (255, 255, 255, 255))
    out("pill_white", c, [15 * S] * 4)

    # generic 1-px white for veils/fills
    Image.new("RGBA", (4, 4), (255, 255, 255, 255)).save(os.path.join(OUT, "white.png"))

    with open(os.path.join(OUT, "slices.json"), "w") as f:
        json.dump(SLICES, f, indent=1, sort_keys=True)


if __name__ == "__main__":
    OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
    os.makedirs(OUT, exist_ok=True)
    build()
    print(f"wrote {len(os.listdir(OUT))} files to {OUT}")
