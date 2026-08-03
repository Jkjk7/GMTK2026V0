# -*- coding: utf-8 -*-
"""生成公开数值分析表。运行: python docs/generate_balance_workbook.py"""
from __future__ import annotations

import math
import subprocess
import sys
from pathlib import Path

try:
    from openpyxl import Workbook
    from openpyxl.styles import Alignment, Font, PatternFill, Border, Side
    from openpyxl.utils import get_column_letter
except ImportError:
    subprocess.check_call([sys.executable, "-m", "pip", "install", "openpyxl", "-q"])
    from openpyxl import Workbook
    from openpyxl.styles import Alignment, Font, PatternFill, Border, Side
    from openpyxl.utils import get_column_letter

OUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else (
    Path(__file__).resolve().parent / "数值分析_模块怪物波次.xlsx"
)

HEADER_FILL = PatternFill("solid", fgColor="1F4E79")
HEADER_FONT = Font(bold=True, color="FFFFFF")
NOTE_FILL = PatternFill("solid", fgColor="FFF2CC")
THIN = Border(
    left=Side(style="thin"),
    right=Side(style="thin"),
    top=Side(style="thin"),
    bottom=Side(style="thin"),
)

WAVE_NORMAL = [4, 5, 6, 8, 8, 14, 16, 18, 20, 24, 26, 28, 30, 32, 36, 40, 44, 48, 52, 58, 62, 66, 72, 78, 88]
WAVE_SWARM = [0, 0, 0, 0, 4, 12, 16, 20, 26, 36, 40, 44, 48, 52, 60, 68, 76, 84, 92, 104, 112, 120, 130, 140, 160]
WAVE_TANK = [0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 5, 6, 7, 8, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 30]
WAVE_SAND = [0, 0, 0, 0, 0, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4]
WAVE_HP = [10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 50, 50, 50, 50, 50, 80, 80, 80, 80, 80, 130, 130, 130, 130, 130]
WAVE_IV = [
    0.95, 0.88, 0.80, 0.72, 0.58, 0.50, 0.42, 0.34, 0.26, 0.16,
    0.38, 0.30, 0.24, 0.18, 0.12, 0.32, 0.26, 0.20, 0.15, 0.10,
    0.28, 0.22, 0.16, 0.12, 0.08,
]

BASE_PRICE = {
    "激光": (10, "普通", 2.25, 1.0),
    "炸弹": (25, "普通", 2.25, 1.0),
    "雪花": (15, "普通", 2.25, 1.0),
    "火花": (15, "普通", 2.25, 1.0),
    "收束器": (15, "稀有", 2.35, 1.5),
    "矿机": (18, "稀有", 2.35, 1.5),
    "火焰增幅": (20, "稀有", 2.35, 1.5),
    "传送门": (22, "稀有", 2.35, 1.5),
    "中续器": (22, "稀有", 2.35, 1.5),
    "加速器": (20, "稀有", 2.35, 1.5),
    "火附魔": (20, "稀有", 2.35, 1.5),
    "惊喜": (20, "稀有", 2.35, 1.5),
    "热浪": (25, "稀有", 2.35, 1.5),
    "黑洞": (45, "史诗", 2.60, 2.5),
    "聚变": (40, "史诗", 2.60, 2.5),
    "裂变": (40, "史诗", 2.60, 2.5),
    "分裂器": (55, "史诗", 2.60, 2.5),
}

ATTACK = {"激光", "炸弹", "雪花", "火花", "黑洞", "热浪"}
LEVEL_SCALE_FLAT = {"火焰增幅", "火附魔", "惊喜"}  # 1+0.4*(lv-1)


def stage(w: int) -> int:
    return (max(1, w) - 1) // 5


def round5(v: float) -> int:
    if v <= 0:
        return 0
    return max(5, int(round(v / 5.0) * 5))


def gold_budget(w: int) -> int:
    w = max(1, min(25, w))
    return max(20, round5(round(18 * (1.205 ** (w - 1)))))


def shop_price(name: str, lv: int, wave: int) -> int:
    base, _, lv_exp, rarity = BASE_PRICE[name]
    sm = 1.80 ** stage(wave)
    if name in ATTACK:
        price = base * (lv_exp ** (lv - 1)) * sm * rarity
    elif name == "矿机":
        price = base * (1 + 0.35 * (lv - 1)) * sm * rarity
    elif name in LEVEL_SCALE_FLAT:
        price = base * (1 + 0.4 * (lv - 1)) * sm * rarity
    else:
        price = base * sm * rarity
    return round5(round(price))


def style_header(ws, row: int, cols: int) -> None:
    for c in range(1, cols + 1):
        cell = ws.cell(row, c)
        cell.fill = HEADER_FILL
        cell.font = HEADER_FONT
        cell.alignment = Alignment(wrap_text=True, vertical="center")


def autosize(ws, min_w=8, max_w=42) -> None:
    for col in ws.columns:
        letter = get_column_letter(col[0].column)
        length = min_w
        for cell in col:
            if cell.value is None:
                continue
            length = max(length, min(max_w, len(str(cell.value)) + 2))
        ws.column_dimensions[letter].width = length


def write_table(ws, start_row: int, headers: list, rows: list) -> int:
    for i, h in enumerate(headers, 1):
        ws.cell(start_row, i, h)
    style_header(ws, start_row, len(headers))
    r = start_row + 1
    for row in rows:
        for i, v in enumerate(row, 1):
            cell = ws.cell(r, i, v)
            cell.border = THIN
            cell.alignment = Alignment(wrap_text=True, vertical="center")
        r += 1
    return r


def laser_dmg(lv: int) -> int:
    return round(5 * (1.8 ** (lv - 1)))


def bomb_dmg(lv: int) -> int:
    return round(15 * (1.5 ** (lv - 1)))


def spark_dmg(lv: int) -> int:
    return {1: 1, 2: 1, 3: 2, 4: 2, 5: 3}[lv]


def main() -> None:
    wb = Workbook()

    ws = wb.active
    ws.title = "00_说明"
    notes = [
        ["GMTK2026 塔防 — 数值分析表（由 docs/generate_balance_workbook.py 生成）"],
        ["配套说明", "docs/数值平衡表.md"],
        ["真相优先级", "Assets/Scripts 代码 > 本表 > 旧文档"],
        [""],
        ["约定"],
        ["能量", "有效输出 ≈ 熔炉吞吐 × 环路投能次数 × 伤/能"],
        ["生存", "沙漏毫秒；普通击杀不补沙"],
        ["刷怪", "固定红/黄/蓝配额，仅打乱顺序"],
        ["HP", "黄=⌈红×0.5⌉；蓝=红×4；红每5波换档"],
        ["商店", "stage=(wave-1)//5；货架暂只出 Lv1"],
        [""],
        ["工作表"],
        ["01_全局", "开局与常量"],
        ["02_模块", "等级要点（激光/炸弹/雪花/火花/矿机）"],
        ["03_熔炉", "四维档位"],
        ["04_波次", "25 波配额/HP/间隔/金币预算"],
        ["05_商店价", "各模块在关键波的 Lv1 标价"],
    ]
    for r, row in enumerate(notes, 1):
        for c, v in enumerate(row, 1):
            cell = ws.cell(r, c, v)
            if r == 1 or (c == 1 and v):
                cell.font = Font(bold=True)
    ws.column_dimensions["A"].width = 16
    ws.column_dimensions["B"].width = 70

    ws = wb.create_sheet("01_全局")
    write_table(
        ws,
        1,
        ["项", "值", "备注"],
        [
            ["起始金币", 50, ""],
            ["总波数", 25, ""],
            ["开局沙 ms", 100_000, "沙尽即败"],
            ["抽沙", "1000 ms/秒", "仅战斗"],
            ["手牌/商店", "8 / 6", ""],
            ["商店池上限", 12, "开局：激光+收束+雪花+火花"],
            ["球上限", 40, ""],
            ["分解返还", 0.30, "invested×30%"],
            ["扩展费用", "100 / 300", "3→5 / 5→7"],
            ["刷新费", "5+stage×(7+stage)", "同阶段恒定"],
            ["货架最高等级", 1, "ShopMaxOfferLevel"],
        ],
    )
    autosize(ws)

    ws = wb.create_sheet("02_模块")
    rows = []
    for lv in range(1, 6):
        rows.append([
            lv,
            laser_dmg(lv),
            5 + (lv - 1) * 2,
            round(max(0.05, 0.2 / (1 + 0.1 * (lv - 1))), 3),
            bomb_dmg(lv),
            round(1.5 * (1 + 0.3 * (lv - 1)), 2),
            5,
            2 + (lv - 1),
            spark_dmg(lv),
            round(2 + 0.5 * (lv - 1), 1),
            {1: 1, 2: 3, 3: 8}.get(min(lv, 3), 8) if lv <= 3 else "-",
        ])
    write_table(
        ws,
        1,
        [
            "Lv",
            "激光伤",
            "激光储能",
            "激光间隔s",
            "炸弹伤",
            "炸弹半径",
            "雪花伤",
            "雪花寒时长s",
            "火花伤",
            "火花烧时长s",
            "矿机金/次",
        ],
        rows,
    )
    ws.cell(8, 1, "雪花能耗1、寒30%；火花能耗1；炸弹能耗5；矿机能耗10、CD3s、最高Lv3。")
    ws.cell(8, 1).fill = NOTE_FILL
    autosize(ws)

    ws = wb.create_sheet("03_熔炉")
    caps = [2000, 1400, 1050, 800]
    speeds = [4.0, 5.5, 7.0, 8.5]
    mass = [1, 2, 3, 4]
    life = [12, 20, 32, 50]
    rows = []
    for i in range(4):
        rate = 1000 / caps[i]
        rows.append([i, caps[i], round(rate, 2), speeds[i], mass[i], life[i], round(rate * mass[i], 2)])
    write_table(
        ws,
        1,
        ["档", "容量ms", "出球/秒", "球速", "质量", "寿命s", "能量/秒"],
        rows,
    )
    autosize(ws)

    ws = wb.create_sheet("04_波次")
    rows = []
    prev_hp = None
    for w in range(1, 26):
        i = w - 1
        n, s, t = WAVE_NORMAL[i], WAVE_SWARM[i], WAVE_TANK[i]
        hp = WAVE_HP[i]
        total = n * hp + s * math.ceil(hp * 0.5) + t * hp * 4
        growth = "" if prev_hp is None else round(total / prev_hp - 1, 3)
        prev_hp = total
        gb = gold_budget(w)
        unlock = []
        if w % 3 == 0 and w < 25:
            unlock.append("模块")
        if w % 5 == 0 and w < 25:
            unlock.append("熔炉")
            unlock.append("祝福束缚")
        rows.append([
            w,
            stage(w),
            n,
            s,
            t,
            WAVE_SAND[i],
            WAVE_IV[i],
            hp,
            total,
            growth,
            gb,
            "+".join(unlock),
        ])
    write_table(
        ws,
        1,
        ["波", "stage", "红", "黄", "蓝", "沙buff基", "间隔s", "红HP", "总HP", "总HP环比", "波金币", "草稿"],
        rows,
    )
    autosize(ws)

    ws = wb.create_sheet("05_商店价")
    waves = (1, 5, 6, 10, 11, 15, 16, 20, 21, 25)
    headers = ["模块", "稀有度", "基础价"] + [f"波{w} Lv1" for w in waves]
    rows = []
    for name, (base, rarity, _, _) in BASE_PRICE.items():
        rows.append([name, rarity, base] + [shop_price(name, 1, w) for w in waves])
    write_table(ws, 1, headers, rows)
    autosize(ws)

    wb.save(OUT)
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
