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

import random

from PIL import Image, ImageChops, ImageDraw, ImageFilter

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
        self.vgrad_stops_rr(box, r, [(0, top), (1, bottom)])

    def _rr_mask(self, box, r):
        x0, y0, x1, y1 = box
        mask = Image.new("L", self.img.size, 0)
        ImageDraw.Draw(mask).rounded_rectangle((x0 * AA, y0 * AA, x1 * AA - 1, y1 * AA - 1), radius=r * AA, fill=255)
        return mask

    def _poly_mask(self, pts):
        mask = Image.new("L", self.img.size, 0)
        ImageDraw.Draw(mask).polygon([(x * AA, y * AA) for x, y in pts], fill=255)
        return mask

    def _paste(self, fill_img, mask):
        layer = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        layer.paste(fill_img, (0, 0), mask)
        self._commit(layer)

    def _vgrad_img(self, y0, y1, stops):
        """Full-canvas image with a vertical gradient through (t, color) stops between y0 and y1."""
        grad = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        gd = ImageDraw.Draw(grad)
        for y in range(self.img.size[1]):
            t = min(1, max(0, (y - y0 * AA) / max(1, (y1 - y0) * AA - 1)))
            for (ta, ca), (tb, cb) in zip(stops, stops[1:]):
                if t <= tb:
                    color = mix(ca, cb, 0 if tb == ta else (t - ta) / (tb - ta))
                    break
            else:
                color = stops[-1][1]
            gd.line([(0, y), (self.img.size[0], y)], fill=color)
        return grad

    def vgrad_stops_rr(self, box, r, stops):
        """Rounded rect with a vertical gradient through (t, color) stops, t in 0..1."""
        self._paste(self._vgrad_img(box[1], box[3], stops), self._rr_mask(box, r))

    def vgrad_poly(self, pts, y0, y1, stops):
        """Polygon with a vertical gradient through (t, color) stops between y0 and y1."""
        self._paste(self._vgrad_img(y0, y1, stops), self._poly_mask(pts))

    def stripes_rr(self, box, r, period, bands, vertical=False):
        """Rounded rect filled with repeating stripes: bands = [(length, color), ...] per period (CSS px * S)."""
        img = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        size = self.img.size[0] if vertical else self.img.size[1]
        for p in range(size):
            pos = (p / AA) % period
            acc = 0
            for length, color in bands:
                acc += length
                if pos < acc:
                    break
            if vertical:
                d.line([(p, 0), (p, self.img.size[1])], fill=color)
            else:
                d.line([(0, p), (self.img.size[0], p)], fill=color)
        self._paste(img, self._rr_mask(box, r))

    def shadow_rr(self, box, r, color, blur):
        """Soft shadow: a rounded rect blurred by `blur` (canvas px), drawn under what follows."""
        layer, d = self._layer()
        x0, y0, x1, y1 = [v * AA for v in box]
        d.rounded_rectangle((x0, y0, x1 - 1, y1 - 1), radius=r * AA, fill=color)
        self._commit(layer.filter(ImageFilter.GaussianBlur(blur * AA / 2)))

    def inset_top(self, box, r, color, width):
        """Light inner top edge (CSS inset 0 <width> 0 <color>): the rect minus itself moved down by `width`."""
        x0, y0, x1, y1 = box
        mask = ImageChops.subtract(self._rr_mask(box, r), self._rr_mask((x0, y0 + width, x1, y1 + width), r))
        self._paste(Image.new("RGBA", self.img.size, color), mask)

    def radial_ellipse(self, box, focus, stops):
        """Ellipse filled with a CSS radial-gradient(circle at focus, stops): focus is a fraction of the box, the
        gradient ends at the farthest corner (CSS default)."""
        x0, y0, x1, y1 = [v * AA for v in box]
        fx, fy = x0 + (x1 - x0) * focus[0], y0 + (y1 - y0) * focus[1]
        radius = max(math.hypot(cx - fx, cy - fy) for cx in (x0, x1) for cy in (y0, y1))
        grad = Image.new("RGBA", self.img.size, (0, 0, 0, 0))
        px = grad.load()
        for y in range(int(y0), int(y1)):
            for x in range(int(x0), int(x1)):
                t = min(1, math.hypot(x - fx, y - fy) / radius)
                for (ta, ca), (tb, cb) in zip(stops, stops[1:]):
                    if t <= tb:
                        px[x, y] = mix(ca, cb, 0 if tb == ta else (t - ta) / (tb - ta))
                        break
                else:
                    px[x, y] = stops[-1][1]
        mask = Image.new("L", self.img.size, 0)
        ImageDraw.Draw(mask).ellipse((x0, y0, x1 - 1, y1 - 1), fill=255)
        self._paste(grad, mask)

    def ring_rr(self, box, r, color, width):
        """Inset outline of a rounded rect (CSS inset box-shadow 0 0 0 width)."""
        self.rr(box, r, None, color, width)

    def save(self, path):
        self.img.resize((self.w, self.h), Image.LANCZOS).save(path)


SLICES = {}


def out(name, canvas, border=None, folder=""):
    path = os.path.join(OUT, folder, name + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    canvas.save(path)
    if border is not None:
        SLICES[(folder + "/" if folder else "") + name + ".png"] = border  # left, bottom, right, top


BTN_LIP = 5  # CSS .btn: box-shadow 0 5px 0 <lip>
BTN_SHADOW = 6  # soft shadow baked below the lip (CSS 0 10px 18px), inside the sprite
BTN_SINK = 4  # CSS .btn:active translateY(4px); the pressed sprite has a 1 px lip so the lip keeps its ground


def pressable(name, stops, lip, shadow, r=18, lip_px=BTN_LIP, shadow_px=BTN_SHADOW, sink=BTN_SINK):
    """3D button as in the prototype's .btn: 3-stop face gradient, 2 px inner top highlight, solid lip and a soft
    shadow below. Writes `<name>` and `<name>_pressed` with the same size and slices: USS moves the pressed button
    down by `sink`, so its shorter lip ends where the normal lip ends."""
    w, h = 60 * S, (60 + lip_px + shadow_px) * S
    face_b = h - (lip_px + shadow_px) * S
    for pressed in (False, True):
        lip_h = (lip_px - sink if pressed else lip_px) * S
        c = Canvas(w, h)
        c.shadow_rr((2 * S, face_b - 2 * r * S, w - 2 * S, face_b + lip_h + 2 * S), r * S,
                    shadow if not pressed else shadow[:3] + (shadow[3] * 2 // 3,),
                    max(1, shadow_px - (2 if pressed else 1)) * S)
        c.rr((0, lip_h, w, face_b + lip_h), r * S, lip)
        c.vgrad_stops_rr((0, 0, w, face_b), r * S, stops)
        c.inset_top((0, 0, w, face_b), r * S, (255, 255, 255, 89), 2 * S)
        b = r * S + 2
        out(name + ("_pressed" if pressed else ""), c, [b, b + (lip_px + shadow_px) * S, b, b])


def build():
    # --- buttons
    # gradients and lips from the prototype's .btn / .btn.green / .btn.wood / .btn.gold
    pressable("btn_berry", [(0, rgba("#E05A69")), (.45, rgba(T["berry"])), (1, rgba(T["berry_d"]))],
              rgba(T["berry_dd"]), (80, 20, 20, 72))
    pressable("btn_green", [(0, rgba("#86C265")), (.5, rgba(T["leaf"])), (1, rgba(T["leaf_d"]))],
              rgba("#2F5623"), (20, 60, 20, 64))
    pressable("btn_wood", [(0, rgba(T["oak_l"])), (.5, rgba(T["oak"])), (1, rgba(T["oak_d"]))],
              rgba(T["oak_dd"]), (60, 30, 10, 64))
    pressable("btn_gold", [(0, rgba("#FFE08A")), (.55, rgba(T["gold"])), (1, rgba(T["gold_d"]))],
              rgba("#8A5E10"), (90, 60, 10, 64))
    pressable("btn_grey", [(0, rgba("#CFC6BA")), (.5, rgba(T["grey"])), (1, rgba(T["grey_d"]))],
              shade(T["grey_d"], -.35), (60, 50, 40, 56))
    # small square wood button (settings gear, .gear / .homeGear): 12 px radius, 2 px lip, sinks by 1 px
    pressable("btn_gear", [(0, rgba(T["oak_l"])), (1, rgba(T["oak_d"]))], rgba(T["oak_dd"]), (70, 40, 15, 64),
              r=12, lip_px=2, shadow_px=4, sink=1)

    # --- card (modal body): cream, top glow, 10 px tan lip (CSS box-shadow 0 10px 0 #C9A774)
    c = Canvas(120 * S, 130 * S)
    face_b = c.h - 10 * S
    c.rr((0, 10 * S, c.w, c.h), 26 * S, rgba("#C9A774"))
    c.rr((0, 0, c.w, face_b), 26 * S, rgba(T["cream"]))
    c.vgrad_stops_rr((6 * S, 0, c.w - 6 * S, 50 * S), 26 * S, [(0, (255, 255, 255, 150)), (1, (255, 255, 255, 0))])
    out("card", c, [26 * S + 2, 36 * S + 2, 26 * S + 2, 26 * S + 2])

    c = Canvas(40 * S, 32 * S)
    c.rr((0, 2 * S, c.w, c.h), 16 * S, (120, 80, 40, 50))
    c.rr((0, 0, c.w, c.h - 2 * S), 16 * S, rgba(T["cream"]), rgba("#E6D2AE"), S)
    out("chip", c, [16 * S] * 4)

    # panels and rows (settings rows, stat rows, tiles): 14 px radius as the prototype's .toggle
    c = Canvas(32 * S, 32 * S)
    c.rr((0, 0, c.w, c.h), 14 * S, (255, 255, 255, 255))
    out("rounded_white", c, [14 * S + 1] * 4)

    c = Canvas(64, 64)
    c.ellipse((0, 0, 64, 64), (255, 255, 255, 255))
    out("circle", c)

    # soft drop shadow ellipse (under items, baskets)
    img = Image.new("RGBA", (64, 24), (0, 0, 0, 0))
    ImageDraw.Draw(img).ellipse((6, 6, 58, 18), fill=(60, 35, 15, 110))
    img = img.filter(ImageFilter.GaussianBlur(3))
    img.save(os.path.join(OUT, "shadow.png"))

    # --- ribbon title (card .ribbon): band with notched ends (clip-path 5% notches), 4 px dark lip, no tails.
    # Drawn at its USS height (40 px), sliced horizontally only.
    for name, top, bottom, dark in (("ribbon_berry", "#E05A69", T["berry_d"], T["berry_dd"]),
                                    ("ribbon_green", "#86C265", T["leaf_d"], "#2F5623")):
        w, h, lip, n = 120 * S, 40 * S, 4 * S, 7 * S
        hb = h - lip

        def band(dy):
            return [(0, dy), (w, dy), (w - n, dy + hb / 2), (w, dy + hb), (0, dy + hb), (n, dy + hb / 2)]

        c = Canvas(w, h)
        c.poly(band(lip), rgba(dark))
        c.vgrad_poly(band(0), 0, hb, [(0, rgba(top)), (1, rgba(bottom))])
        c.line([(n * .4, S), (w - n * .4, S)], (255, 255, 255, 70), S)
        out(name, c, [n + 4 * S, 0, n + 4 * S, 0])

    rng = random.Random(7)

    def grain(c, box, r, color, count, wave):
        """Thin wavy grain lines across a board, clipped to its rounded rect."""
        x0, y0, x1, y1 = box
        layer = Canvas(c.w // 1, c.h // 1)
        for _ in range(count):
            y = rng.uniform(y0 + r / 2, y1 - r / 2)
            amp, phase, k = rng.uniform(.3, 1) * wave, rng.uniform(0, 6.28), rng.uniform(1.5, 3.5)
            pts = [(x, y + amp * math.sin(phase + k * 6.28 * (x - x0) / (x1 - x0))) for x in range(int(x0), int(x1) + 1, S)]
            layer.line(pts, color, S * rng.choice((.5, 1)))
        c._paste(layer.img, c._rr_mask(box, r))

    # --- walnut sign (home, album): plank stripes (.sign), top sheen, dark walnut frame with a light inner line,
    # 12 px drop (0 12px 0 rgba(40,20,5,.25)) and a soft shadow. Drawn at the sign's width (340 px).
    w, face_h, drop = 340 * S, 120 * S, 14 * S
    c = Canvas(w, face_h + drop)
    c.shadow_rr((4 * S, 12 * S, w - 4 * S, face_h + 8 * S), 18 * S, (40, 20, 5, 70), 6 * S)
    c.rr((0, 12 * S, w, face_h + 12 * S), 18 * S, (40, 20, 5, 64))
    c.stripes_rr((0, 0, w, face_h), 18 * S, 15 * S,
                 [(7 * S, rgba("#8C5A2C")), (2 * S, rgba("#83532A")), (6 * S, rgba("#93602F"))])
    grain(c, (0, 0, w, face_h), 18 * S, (70, 40, 18, 55), 18, 3 * S)
    c.vgrad_stops_rr((0, 0, w, face_h), 18 * S, [(0, (255, 255, 255, 46)), (.5, (255, 255, 255, 0)), (1, (0, 0, 0, 0))])
    c.ring_rr((0, 0, w, face_h), 18 * S, rgba("#4A2C12"), 5 * S)
    c.ring_rr((5 * S, 5 * S, w - 5 * S, face_h - 5 * S), 13 * S, rgba("#A06C38"), 2 * S)
    out("board_walnut", c, [24 * S, 24 * S + drop, 24 * S, 24 * S])

    # --- oak counter (.counter): vertical oak boards, sheen, inner rim, 8 px oak lip and a soft shadow.
    # Drawn at the counter's width (358 px = 390 - 2 * 16).
    w, face_h, lip, sh = 358 * S, 104 * S, 8 * S, 6 * S
    c = Canvas(w, face_h + lip + sh)
    c.shadow_rr((4 * S, 20 * S, w - 4 * S, face_h + lip + 2 * S), 16 * S, (60, 30, 10, 72), 5 * S)
    c.rr((0, lip, w, face_h + lip), 16 * S, rgba(T["oak_d"]))
    c.stripes_rr((0, 0, w, face_h), 16 * S, 60 * S,
                 [(34 * S, rgba("#D7A263")), (2 * S, rgba("#CF9A5A")), (22 * S, rgba("#DCA96B")), (2 * S, rgba("#C9924F"))],
                 vertical=True)
    grain(c, (0, 0, w, face_h), 16 * S, (150, 95, 45, 40), 10, 2 * S)
    c.vgrad_stops_rr((0, 0, w, face_h), 16 * S, [(0, (255, 255, 255, 56)), (.4, (255, 255, 255, 0)), (1, (0, 0, 0, 0))])
    c.ring_rr((0, 0, w, face_h), 16 * S, (107, 67, 32, 110), 2 * S)
    c.ring_rr((2 * S, 2 * S, w - 2 * S, face_h - 2 * S), 14 * S, (255, 240, 210, 60), S)
    out("board_oak", c, [20 * S, 20 * S + lip + sh, 20 * S, 20 * S])

    # --- white pill with a soft shadow (tip toast above a button); 4 px of shadow room on the sides, 10 px below
    c = Canvas(48 * S, 46 * S)
    c.shadow_rr((4 * S, 6 * S, 44 * S, 40 * S), 16 * S, (60, 35, 15, 80), 4 * S)
    c.rr((4 * S, 2 * S, 44 * S, 36 * S), 16 * S, (255, 255, 255, 255))
    out("pill_shadow", c, [21 * S, 27 * S, 21 * S, 19 * S])

    # --- start booster tiles (.preSlot): 16 px radius, 2 px dashed tan border when off, solid green + glow when on
    def rr_path(x0, y0, x1, y1, r, step):
        """Points along a rounded rect outline, `step` apart."""
        pts = []
        segs = [((x0 + r, y0), (x1 - r, y0)), None, ((x1, y0 + r), (x1, y1 - r)), None,
                ((x1 - r, y1), (x0 + r, y1)), None, ((x0, y1 - r), (x0, y0 + r)), None]
        arcs = [(x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)]
        for k in range(4):
            (ax, ay), (bx, by) = segs[2 * k]
            n = max(1, int(math.hypot(bx - ax, by - ay) / step))
            pts += [(ax + (bx - ax) * i / n, ay + (by - ay) * i / n) for i in range(n)]
            cx, cy, a0 = arcs[k]
            n = max(1, int(math.pi * r / 2 / step))
            pts += [(cx + r * math.cos(math.radians(a0 + 90 * i / n)), cy + r * math.sin(math.radians(a0 + 90 * i / n)))
                    for i in range(n)]
        return pts

    w = h = 140 * S  # drawn at the tile's size so the dashes are not stretched
    c = Canvas(w, h)
    c.rr((0, 0, w, h), 16 * S, (255, 250, 238, 230))
    pts = rr_path(S, S, w - S, h - S, 15 * S, S / 2)
    dash, gap = 6 * S, 4 * S
    run, acc = [], 0.0
    for a, b in zip(pts, pts[1:] + pts[:1]):
        acc += math.hypot(b[0] - a[0], b[1] - a[1])
        if acc % (dash + gap) < dash:
            run.append(a)
        elif run:
            c.line(run + [a], (150, 98, 47, 115), 2 * S)
            run = []
    if run:
        c.line(run, (150, 98, 47, 115), 2 * S)
    out("tile_dashed", c, [20 * S] * 4)

    c = Canvas(w, h)
    c.rr((0, 0, w, h), 16 * S, (79, 138, 58, 64))
    c.rr((3 * S, 3 * S, w - 3 * S, h - 3 * S), 13 * S, rgba("#4F8A3A"))
    c.rr((5 * S, 5 * S, w - 5 * S, h - 5 * S), 11 * S, rgba("#E5F4E4"))
    out("tile_on", c, [20 * S] * 4)

    # --- tricky-level gingham (#game.tricky): two crossed 45 deg stripe sets, 16 px berry 10% / 16 px clear.
    # The tile is 45 px (= 32 * sqrt 2, rounded) so it repeats seamlessly; drawn per pixel without supersampling.
    tile = 45 * S
    period = tile / math.sqrt(2)
    img = Image.new("RGBA", (tile, tile), (0, 0, 0, 0))
    px = img.load()
    for y in range(tile):
        for x in range(tile):
            a = ((x + y) / math.sqrt(2)) % period < period / 2
            b = ((x - y) / math.sqrt(2)) % period < period / 2
            alpha = 1 - (1 - (.10 if a else 0)) * (1 - (.10 if b else 0))
            px[x, y] = (200, 49, 62, round(alpha * 255))
    img.save(os.path.join(OUT, "gingham.png"))

    # --- snowflake for the ice badge (".ice b" shows "❄ 3"): 16 px, white, six arms with two twigs each
    n = 16 * S
    c = Canvas(n, n)
    mid = n / 2
    for k in range(6):
        a = math.radians(90 + k * 60)
        dx, dy = math.cos(a), -math.sin(a)
        tip = (mid + dx * mid * .92, mid + dy * mid * .92)
        c.line([(mid, mid), tip], rgba("#FFFFFF"), 1.8 * S)
        for t in (.45, .7):
            bx, by = mid + dx * mid * t, mid + dy * mid * t
            for side in (-1, 1):
                b = a + side * math.radians(45)
                ln = mid * (.3 if t < .5 else .22)
                c.line([(bx, by), (bx + math.cos(b) * ln, by - math.sin(b) * ln)], rgba("#FFFFFF"), 1.4 * S)
    out("snowflake", c)

    # --- sign rope (.sign::before): 4 px twisted rope, 4 px dark / 4 px light bands, tiled vertically
    c = Canvas(4 * S, 8 * S)
    c.rr((0, 0, c.w, 4 * S), 0, rgba("#8A6A3A"))
    c.rr((0, 4 * S, c.w, 8 * S), 0, rgba("#B8955C"))
    c.line([(0, 5 * S), (c.w, 3 * S)], (255, 240, 200, 70), S * .5)
    out("rope", c)

    # --- showcase plank (.showcase .plank): 16 px, light top edge, soft shadow below
    w, ph, sh = 80 * S, 16 * S, 7 * S
    c = Canvas(w, ph + sh)
    c.shadow_rr((3 * S, 6 * S, w - 3 * S, ph + 4 * S), 4 * S, (40, 20, 5, 72), 4 * S)
    c.vgrad_stops_rr((0, 0, w, ph), 4 * S, [(0, rgba("#F6D6A2")), (.31, rgba("#F6D6A2")), (.32, rgba("#C98E52")),
                                            (1, rgba("#9C6532"))])
    out("plank", c, [6 * S, 6 * S + sh, 6 * S, 6 * S])

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
    # level badge (.lvlBadge / .lvlBadge.hard): 52 px disc, radial gradient lit from the top left, 3 px white inner
    # ring, 3 px lip and a soft shadow (0 6px 10px). The disc sits at (6, 2) in a 64 x 68 sprite.
    for name, stops, lip in (
            ("badge_gold", [(0, rgba("#FFE7A8")), (.55, rgba(T["gold"])), (1, rgba(T["gold_d"]))], rgba("#8A5E10")),
            ("badge_red", [(0, rgba("#FFB0A0")), (.55, rgba("#D84A3A")), (1, rgba("#8E2418"))], rgba("#5E170F"))):
        c = Canvas(64 * S, 68 * S)
        disc = (6 * S, 2 * S, 58 * S, 54 * S)
        layer, d = c._layer()
        d.ellipse((6 * S * AA, 8 * S * AA, 58 * S * AA, 60 * S * AA), fill=(0, 0, 0, 51))
        c._commit(layer.filter(ImageFilter.GaussianBlur(5 * S * AA / 2)))
        c.ellipse((disc[0], disc[1] + 3 * S, disc[2], disc[3] + 3 * S), lip)
        c.radial_ellipse(disc, (.35, .30), stops)
        c.ellipse(disc, None, (255, 255, 255, 89), 3 * S)
        out(name, c)

    # booster tile (.bst .ic): 58 px, 18 px radius, cream gradient, 3 px tan inner ring, 4 px lip (0 4px 0 #A77A48)
    # and a soft shadow (0 8px 12px). Pressed (.bst:active .ic): moved down 3 px by USS, 1 px lip, smaller shadow.
    # The face sits at (6, 2) in a 70 x 78 sprite.
    for pressed in (False, True):
        c = Canvas(70 * S, 78 * S)
        face = (6 * S, 2 * S, 64 * S, 60 * S)
        lip_px = 1 if pressed else 4
        c.shadow_rr((8 * S, (5 if pressed else 10) * S, 62 * S, (63 if pressed else 68) * S), 18 * S,
                    (40, 20, 5, 51 if pressed else 64), (3 if pressed else 6) * S)
        c.rr((face[0], face[1] + lip_px * S, face[2], face[3] + lip_px * S), 18 * S, rgba("#A77A48"))
        c.vgrad_rr(face, 18 * S, rgba("#FFFDF5"), rgba("#F1E1C0"))
        c.ring_rr(face, 18 * S, (150, 98, 47, 64), 3 * S)
        out("booster_tile" + ("_pressed" if pressed else ""), c)

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

    with open(os.path.join(OUT, "slices.json"), "w", newline="\n") as f:
        json.dump(SLICES, f, indent=1, sort_keys=True)


if __name__ == "__main__":
    OUT = sys.argv[1] if len(sys.argv) > 1 else "out"
    os.makedirs(OUT, exist_ok=True)
    build()
    print(f"wrote {len(os.listdir(OUT))} files to {OUT}")
