"""Generate transparent pixel-style UI game frame for Unity."""
from PIL import Image, ImageDraw
from pathlib import Path

OUT = Path(r"D:\unity\GMTK2026塔防V0\Assets\Resources\UI\ui_game_frame.png")
W, H = 1920, 1080

FRAME_BASE = (28, 22, 48, 255)
FRAME_MID = (42, 36, 72, 255)
FRAME_DARK = (16, 12, 32, 255)
HIGHLIGHT = (110, 140, 200, 220)
HIGHLIGHT_DIM = (70, 90, 150, 180)
SIDEBAR = (24, 20, 44, 255)
SIDEBAR_EDGE = (55, 70, 120, 255)
CORNER = (90, 120, 180, 240)
BORDER_INNER = (60, 80, 140, 200)

BATTLE = (80, 40, 1400, 360)
BOARD = (280, 400, 1400, 1040)
SIDEBAR_X0, SIDEBAR_X1 = 1460, 1890
BORDER = 32


def draw_border_frame(draw, x0, y0, x1, y1, thickness, outer, inner, highlight):
    draw.rectangle([x0 - thickness, y0 - thickness, x1 + thickness, y0], fill=outer)
    draw.rectangle([x0 - thickness, y1, x1 + thickness, y1 + thickness], fill=outer)
    draw.rectangle([x0 - thickness, y0 - thickness, x0, y1 + thickness], fill=outer)
    draw.rectangle([x1, y0 - thickness, x1 + thickness, y1 + thickness], fill=outer)
    draw.rectangle([x0 - 2, y0 - 2, x1 + 1, y0], fill=highlight)
    draw.rectangle([x0 - 2, y1, x1 + 1, y1 + 1], fill=highlight)
    draw.rectangle([x0 - 2, y0 - 2, x0, y1 + 1], fill=highlight)
    draw.rectangle([x1, y0 - 2, x1 + 1, y1 + 1], fill=highlight)
    ox0, oy0 = x0 - thickness, y0 - thickness
    ox1, oy1 = x1 + thickness, y1 + thickness
    draw.rectangle([ox0, oy0, ox1, oy0 + 1], fill=inner)
    draw.rectangle([ox0, oy0, ox0 + 1, oy1], fill=inner)


def draw_l_corner(draw, cx, cy, size, thick, color, flip_x=False, flip_y=False):
    dx = -1 if flip_x else 1
    dy = -1 if flip_y else 1
    x1 = cx + dx * size
    y1 = cy + dy * thick
    draw.rectangle([min(cx, x1), min(cy, y1), max(cx, x1), max(cy, y1)], fill=color)
    x2 = cx + dx * thick
    y2 = cy + dy * size
    draw.rectangle([min(cx, x2), min(cy, y2), max(cx, x2), max(cy, y2)], fill=color)


def main():
    img = Image.new("RGBA", (W, H), FRAME_BASE)
    draw = ImageDraw.Draw(img)

    draw.rectangle([0, 0, W, 40], fill=FRAME_DARK)
    draw.rectangle([0, H - 40, W, H], fill=FRAME_DARK)
    draw.rectangle([0, 0, 60, H], fill=FRAME_DARK)
    draw.rectangle([60, 40, 1460, H - 40], fill=FRAME_MID)

    draw.rectangle([SIDEBAR_X0, 0, SIDEBAR_X1, H], fill=SIDEBAR)
    draw.rectangle([SIDEBAR_X0, 0, SIDEBAR_X0 + 3, H], fill=SIDEBAR_EDGE)
    draw.rectangle([SIDEBAR_X1, 0, W, H], fill=FRAME_DARK)
    for y in (120, 280, 520, 780):
        draw.rectangle([SIDEBAR_X0 + 20, y, SIDEBAR_X1 - 20, y + 1], fill=HIGHLIGHT_DIM)

    draw.rectangle([0, 0, W - 1, 2], fill=HIGHLIGHT)
    draw.rectangle([0, 0, 2, H - 1], fill=HIGHLIGHT)
    draw.rectangle([0, H - 3, W - 1, H - 1], fill=HIGHLIGHT_DIM)
    draw.rectangle([W - 3, 0, W - 1, H - 1], fill=HIGHLIGHT_DIM)

    bx0, by0, bx1, by1 = BATTLE
    ox0, oy0, ox1, oy1 = BOARD

    transparent = Image.new("RGBA", (bx1 - bx0, by1 - by0), (0, 0, 0, 0))
    transparent2 = Image.new("RGBA", (ox1 - ox0, oy1 - oy0), (0, 0, 0, 0))

    draw_border_frame(draw, bx0, by0, bx1, by1, BORDER, FRAME_BASE, HIGHLIGHT, BORDER_INNER)
    draw_border_frame(draw, ox0, oy0, ox1, oy1, BORDER, FRAME_BASE, HIGHLIGHT, BORDER_INNER)

    cs, ct = 28, 6
    draw_l_corner(draw, bx0 - 4, by0 - 4, cs, ct, CORNER, False, False)
    draw_l_corner(draw, bx1 + 4, by0 - 4, cs, ct, CORNER, True, False)
    draw_l_corner(draw, bx0 - 4, by1 + 4, cs, ct, CORNER, False, True)
    draw_l_corner(draw, bx1 + 4, by1 + 4, cs, ct, CORNER, True, True)
    draw_l_corner(draw, ox0 - 4, oy0 - 4, cs, ct, CORNER, False, False)
    draw_l_corner(draw, ox1 + 4, oy0 - 4, cs, ct, CORNER, True, False)
    draw_l_corner(draw, ox0 - 4, oy1 + 4, cs, ct, CORNER, False, True)
    draw_l_corner(draw, ox1 + 4, oy1 + 4, cs, ct, CORNER, True, True)
    draw_l_corner(draw, 8, 8, 40, 8, CORNER, False, False)
    draw_l_corner(draw, W - 9, 8, 40, 8, CORNER, True, False)
    draw_l_corner(draw, 8, H - 9, 40, 8, CORNER, False, True)
    draw_l_corner(draw, W - 9, H - 9, 40, 8, CORNER, True, True)

    # Punch transparent interiors last so they stay alpha=0
    img.paste(transparent, (bx0, by0))
    img.paste(transparent2, (ox0, oy0))

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT, "PNG")
    print(f"Saved: {OUT}")
    print(f"Size: {OUT.stat().st_size} bytes")
    print(f"Dimensions: {img.size} mode={img.mode}")

    battle_samples = [(200, 100), (700, 200), (1200, 300)]
    board_samples = [(400, 500), (800, 700), (1200, 900)]
    sidebar_samples = [(1600, 100), (1700, 540), (1800, 1000)]

    def check(label, pts, expect_transparent=True):
        results = []
        for x, y in pts:
            a = img.getpixel((x, y))[3]
            results.append((x, y, a))
        print(f"{label}: {results}")
        if expect_transparent:
            ok = all(a == 0 for _, _, a in results)
        else:
            ok = all(a > 200 for _, _, a in results)
        print(f"  PASS={ok}")
        return ok

    ok1 = check("Battle (expect alpha=0)", battle_samples, True)
    ok2 = check("Board (expect alpha=0)", board_samples, True)
    ok3 = check("Sidebar (expect alpha>200)", sidebar_samples, False)
    print(f"ALL_CHECKS_PASS={ok1 and ok2 and ok3}")


if __name__ == "__main__":
    main()
