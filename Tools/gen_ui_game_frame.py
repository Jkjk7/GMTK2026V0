"""Generate transparent UI game frame — Phase-1 layout fix.

1920x1080 target from Newchange.txt:
  Battle:  nearly full width at top (~34%)
  Board:   left+center bottom, includes emitter (left ~16% + board ~58%)
  Sidebar: right column of BOTTOM only (~22% x ~66%)
"""
from PIL import Image, ImageDraw
from pathlib import Path

OUT = Path(r"D:\unity\GMTK2026塔防V0\Assets\Resources\UI\ui_game_frame.png")
W, H = 1920, 1080

FRAME = (20, 16, 36, 255)
FRAME_EDGE = (12, 10, 24, 255)
ACCENT = (80, 170, 230, 235)
ACCENT_DIM = (45, 95, 145, 170)
SIDEBAR = (16, 14, 28, 250)
SIDEBAR_LINE = (55, 110, 160, 180)

MARGIN = 30
# Sidebar only on lower console (not over battle)
SIDEBAR_X0 = int(W * 0.78)          # 1498
BATTLE_BOTTOM = int(H * 0.34)       # 367
DIVIDER = 14
BOARD_TOP = BATTLE_BOTTOM + DIVIDER  # 381

# Battle = almost full width (over sidebar column too)
BATTLE = (MARGIN, MARGIN, W - MARGIN, BATTLE_BOTTOM)

# Board = left+center of lower area (emitter + grid)
BOARD = (MARGIN, BOARD_TOP, SIDEBAR_X0 - 10, H - MARGIN)

# Sidebar plate = lower-right only
SIDEBAR_RECT = (SIDEBAR_X0, BOARD_TOP, W - MARGIN, H - MARGIN)


def stroke_rect(draw, box, color, width=2):
    x0, y0, x1, y1 = box
    for i in range(width):
        draw.rectangle([x0 - i, y0 - i, x1 + i, y1 + i], outline=color)


def main():
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Fill opaque frame, then punch battle + board
    draw.rectangle([0, 0, W, H], fill=FRAME)
    draw.rectangle([0, 0, W, MARGIN], fill=FRAME_EDGE)
    draw.rectangle([0, H - MARGIN, W, H], fill=FRAME_EDGE)
    draw.rectangle([0, 0, MARGIN, H], fill=FRAME_EDGE)
    draw.rectangle([W - MARGIN, 0, W, H], fill=FRAME_EDGE)

    for x0, y0, x1, y1 in (BATTLE, BOARD):
        clear = Image.new("RGBA", (x1 - x0, y1 - y0), (0, 0, 0, 0))
        img.paste(clear, (x0, y0))

    draw = ImageDraw.Draw(img)

    # Sidebar plate
    sx0, sy0, sx1, sy1 = SIDEBAR_RECT
    draw.rectangle([sx0, sy0, sx1, sy1], fill=SIDEBAR)
    draw.rectangle([sx0, sy0, sx0 + 3, sy1], fill=SIDEBAR_LINE)
    mid_y = sy0 + int((sy1 - sy0) * 0.48)
    draw.rectangle([sx0 + 14, mid_y, sx1 - 14, mid_y + 2], fill=SIDEBAR_LINE)

    # Divider bar between battle and board (left of sidebar)
    draw.rectangle(
        [MARGIN, BATTLE_BOTTOM, SIDEBAR_X0 - 10, BOARD_TOP],
        fill=FRAME_EDGE,
    )
    draw.rectangle(
        [MARGIN + 8, BATTLE_BOTTOM + 5, SIDEBAR_X0 - 18, BATTLE_BOTTOM + 7],
        fill=ACCENT_DIM,
    )

    stroke_rect(draw, BATTLE, ACCENT, 2)
    stroke_rect(draw, BOARD, ACCENT, 2)
    stroke_rect(draw, SIDEBAR_RECT, ACCENT_DIM, 2)
    stroke_rect(draw, (3, 3, W - 4, H - 4), ACCENT, 2)

    # Corner ticks on board window
    tick = 26
    bx0, by0, bx1, by1 = BOARD
    for cx, cy, dx, dy in [
        (bx0, by0, 1, 1),
        (bx1, by0, -1, 1),
        (bx0, by1, 1, -1),
        (bx1, by1, -1, -1),
    ]:
        x0, x1 = sorted([cx, cx + dx * tick])
        y0a, y1a = sorted([cy, cy + dy * 3])
        draw.rectangle([x0, y0a, x1, y1a], fill=ACCENT)
        x0b, x1b = sorted([cx, cx + dx * 3])
        y0b, y1b = sorted([cy, cy + dy * tick])
        draw.rectangle([x0b, y0b, x1b, y1b], fill=ACCENT)

    OUT.parent.mkdir(parents=True, exist_ok=True)
    img.save(OUT, "PNG")

    def a(x, y):
        return img.getpixel((x, y))[3]

    tests = [
        ("battle center", 960, 180, True),
        ("battle over sidebar column", 1700, 180, True),
        ("board center", 800, 700, True),
        ("board left / emitter", 120, 700, True),
        ("sidebar plate", 1700, 700, False),
        ("outer margin", 10, 540, False),
    ]
    ok = True
    for name, x, y, clear in tests:
        alpha = a(x, y)
        passed = (alpha == 0) if clear else (alpha > 200)
        ok &= passed
        print(f"{name}: alpha={alpha} PASS={passed}")

    print(f"BATTLE={BATTLE}")
    print(f"BOARD={BOARD}")
    print(f"SIDEBAR={SIDEBAR_RECT}")
    print(f"Saved {OUT} size={OUT.stat().st_size}")
    print(f"ALL_PASS={ok}")


if __name__ == "__main__":
    main()
