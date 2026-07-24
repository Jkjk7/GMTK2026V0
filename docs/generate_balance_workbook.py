# -*- coding: utf-8 -*-
"""Generate GMTK2026 balance analysis workbook (.xlsx). Run: python docs/generate_balance_workbook.py"""
from __future__ import annotations

import math
import sys
from pathlib import Path

try:
    from openpyxl import Workbook, load_workbook
    from openpyxl.styles import Alignment, Font, PatternFill, Border, Side
    from openpyxl.utils import get_column_letter
except ImportError:
    import subprocess
    import sys

    subprocess.check_call([sys.executable, "-m", "pip", "install", "openpyxl", "-q"])
    from openpyxl import Workbook, load_workbook
    from openpyxl.styles import Alignment, Font, PatternFill, Border, Side
    from openpyxl.utils import get_column_letter


DEFAULT_OUT = Path(__file__).resolve().parent / "数值分析_模块怪物波次.xlsx"
OUT = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else DEFAULT_OUT

# --- formulas mirrored from game code ---

def laser_dmg(lv: int) -> int:
    return round(5 * (1.8 ** (lv - 1)))


def laser_cap(lv: int) -> int:
    return 5 + (lv - 1) * 2  # 削弱：基础 10 → 5


def laser_interval(lv: int) -> float:
    return max(0.05, 0.2 / (1 + 0.10 * (lv - 1)))  # 削弱：基础射速 10/s → 5/s


def bomb_dmg(lv: int) -> int:
    return round(15 * (1.5 ** (lv - 1)))


def bomb_radius(lv: int) -> float:
    return 1.5 * (1 + 0.30 * (lv - 1))


def bomb_cap(lv: int) -> int:
    return 20 + (lv - 1) * 2


def ice_cap(lv: int) -> int:
    return laser_cap(lv)  # 与激光同储能


def ice_interval(lv: int) -> float:
    return laser_interval(lv)  # 与激光同射速


def ice_slow(lv: int) -> float:
    return 0.30  # 固定 30%


def ice_slow_dur(lv: int) -> float:
    return 2.0 + (lv - 1)  # 升级只延长时长


def miner_cost(lv: int) -> int:
    return 10  # 固定能耗


def miner_gold(lv: int) -> int:
    return {1: 1, 2: 3, 3: 8}[min(max(lv, 1), 3)]


def stage(w: int) -> int:
    return (max(1, w) - 1) // 5


def round5(v: int) -> int:
    if v <= 0:
        return 0
    return max(5, round(v / 5) * 5)


def shop_price(kind: str, lv: int, wave: int) -> int:
    st = stage(wave)
    stage_mult = 1.80 ** st
    base = {
        "laser": 10, "redirector": 15, "bomb": 25, "ice": 22, "miner": 18, "blackhole": 45,
    }[kind]
    rarity_mult = {
        "laser": 1.0, "bomb": 1.0, "ice": 1.0,
        "redirector": 1.5, "miner": 1.5, "blackhole": 2.5,
    }[kind]
    lv_exp = {
        "laser": 2.25, "bomb": 2.25, "ice": 2.25,
        "redirector": 2.35, "miner": 2.35, "blackhole": 2.6,
    }[kind]
    if kind in ("laser", "bomb", "ice", "blackhole"):
        price = base * (lv_exp ** (lv - 1)) * stage_mult * rarity_mult
        return round5(round(price))
    if kind == "miner":
        price = base * (1 + 0.35 * (lv - 1)) * stage_mult * rarity_mult
        return round5(round(price))
    return round5(round(base * stage_mult * rarity_mult))


def refresh_cost(wave: int) -> int:
    st = stage(wave)
    return 5 + st * (7 + st)


WAVE_NORMAL_COUNTS = [
    4, 5, 6, 8, 8, 14, 16, 18, 20, 24, 26, 28, 30, 32, 36,
    40, 44, 48, 52, 58, 62, 66, 72, 78, 88,
]
WAVE_SWARM_COUNTS = [
    0, 0, 0, 0, 4, 12, 16, 20, 26, 36, 40, 44, 48, 52, 60,
    68, 76, 84, 92, 104, 112, 120, 130, 140, 160,
]
WAVE_TANK_COUNTS = [
    0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 5, 6, 7, 8, 10,
    11, 12, 14, 16, 18, 20, 22, 24, 26, 30,
]
WAVE_NORMAL_HP = [
    10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 50, 50, 50, 50, 50,
    80, 80, 80, 80, 80, 130, 130, 130, 130, 130,
]
WAVE_SPAWN_INTERVALS = [
    0.95, 0.88, 0.80, 0.72, 0.58,
    0.50, 0.42, 0.34, 0.26, 0.16,
    0.38, 0.30, 0.24, 0.18, 0.12,
    0.32, 0.26, 0.20, 0.15, 0.10,
    0.28, 0.22, 0.16, 0.12, 0.08,
]
TARGET_COMBAT_SECONDS = [
    (6, 10), (8, 12), (9, 14), (11, 16), (14, 20),
    (20, 30), (22, 32), (25, 35), (28, 40), (30, 45),
    (35, 50), (40, 55), (45, 60), (50, 65), (55, 75),
    (60, 85), (65, 95), (70, 105), (75, 115), (80, 130),
    (90, 145), (100, 160), (110, 175), (120, 190), (130, 210),
]


def wave_index(w: int) -> int:
    return max(1, min(25, w)) - 1


def wave_counts(w: int) -> tuple[int, int, int]:
    i = wave_index(w)
    return WAVE_NORMAL_COUNTS[i], WAVE_SWARM_COUNTS[i], WAVE_TANK_COUNTS[i]


def enemy_hp(w: int) -> tuple[int, int, int]:
    normal = WAVE_NORMAL_HP[wave_index(w)]
    return normal, math.ceil(normal * 0.5), normal * 4


def wave_budget(w: int) -> int:
    """Legacy point-equivalent display; runtime queue uses fixed quotas."""
    normal, swarm, tank = wave_counts(w)
    return normal * 5 + swarm + tank * 20


def spawn_interval(w: int) -> float:
    return WAVE_SPAWN_INTERVALS[wave_index(w)]


def guaranteed_tanks(w: int) -> int:
    return wave_counts(w)[2]


def wave_total_hp(w: int) -> int:
    counts = wave_counts(w)
    hp = enemy_hp(w)
    return sum(count * per_enemy for count, per_enemy in zip(counts, hp))


def estimated_spawn_seconds(w: int) -> float:
    """First enemy is immediate; a Swarm shortens the following gap to 55%."""
    normal, swarm, tank = wave_counts(w)
    count = normal + swarm + tank
    if count <= 1:
        return 0.0
    shortened_gap_ratio = swarm / count
    average_gap_scale = 1.0 - 0.45 * shortened_gap_ratio
    return (count - 1) * spawn_interval(w) * average_gap_scale


def prep_seconds(w: int) -> float:
    w = max(1, w)
    if w <= 5:
        base = 20
    elif w <= 10:
        base = 25
    elif w <= 20:
        base = 35
    elif w <= 30:
        base = 45
    elif w <= 40:
        base = 60
    else:
        base = 75
    if w % 5 == 0:
        base += 15
    return float(base)


def gold_budget(w: int) -> int:
    w = max(1, min(25, w))
    return max(20, round5(round(18 * (1.205 ** (w - 1)))))


def dismantle_cost(kind: str, lv: int, wave: int, combat: bool) -> int:
    if not combat:
        return 0
    ref = shop_price(kind, lv, wave)
    rate = 0.12 if kind in ("laser", "bomb", "ice", "blackhole") else 0.06
    return max(1, round(ref * rate))


HEADER_FILL = PatternFill("solid", fgColor="1F4E79")
HEADER_FONT = Font(color="FFFFFF", bold=True)
SECTION_FILL = PatternFill("solid", fgColor="D6EAF8")
NOTE_FILL = PatternFill("solid", fgColor="FFF2CC")
THIN = Border(
    left=Side(style="thin", color="B0B0B0"),
    right=Side(style="thin", color="B0B0B0"),
    top=Side(style="thin", color="B0B0B0"),
    bottom=Side(style="thin", color="B0B0B0"),
)


def style_header(ws, row: int, cols: int):
    for c in range(1, cols + 1):
        cell = ws.cell(row, c)
        cell.fill = HEADER_FILL
        cell.font = HEADER_FONT
        cell.alignment = Alignment(wrap_text=True, vertical="center")
        cell.border = THIN


def autosize(ws, min_w=10, max_w=28):
    for col in ws.columns:
        letter = get_column_letter(col[0].column)
        length = 0
        for cell in col:
            if cell.value is None:
                continue
            length = max(length, min(max_w, len(str(cell.value)) + 2))
        ws.column_dimensions[letter].width = max(min_w, length)


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


def main():
    wb = Workbook()

    # --- 00 说明 ---
    ws = wb.active
    ws.title = "00_说明"
    notes = [
        ["GMTK2026 塔防 — 数值分析表（由代码公式生成）"],
        ["生成脚本", "docs/generate_balance_workbook.py"],
        ["用途", "观察现有模块/怪物/波次量化关系；环路乘数需实机校准，表中给理论上下限"],
        [""],
        ["关键约定"],
        ["能量瓶颈", "有效输出 ≈ 能量到达攻击模块速率 × 伤害/能；塔射速通常不是瓶颈"],
        ["索敌", "全体打最左敌人"],
        ["刷怪模型", "每波固定红/黄/蓝配额，仅随机打乱顺序；点数只保留作旧版等价显示"],
        ["HP模型", "黄=向上取整(红×50%)；蓝=红×4；红HP在波6/11/16/21换档：10→25→50→80→130"],
        ["商店阶段", "stage = (wave-1)//5 → 波1-5=0 … 波21-25=4；价含稀有度倍率"],
        ["发射器升级时机", "波5/10/15/20 结束后三选一"],
        ["模块解锁时机", "每3波（至24）；波≥9 可抽黑洞"],
        ["祝福束缚", "波5/10/15/20：祝福+束缚组合三选一"],
        [""],
        ["工作表"],
        ["01_全局常量", "开局资源、棋盘、返还率等"],
        ["02_怪物", "三类怪 HP关系、速度及关键波HP"],
        ["03_模块_激光", "Lv1-5 伤害/储能/射速/伤每能"],
        ["04_模块_炸弹", "Lv1-5 伤害/AOE/能耗"],
        ["05_模块_寒冰", "Lv1-5 控制参数"],
        ["06_模块_采矿", "Lv1-3 能耗与金产出"],
        ["07_收束器", "路径模块"],
        ["08_发射器", "四维升级档位与能量/秒"],
        ["09_波次曲线", "25波固定配额/HP/总HP/间隔/刷怪段/验收时长"],
        ["10_商店价格", "各模块（含黑洞）在关键波标价"],
        ["11_能量DPS对照", "无环理论DPS vs 波次总HP粗估"],
        ["12_拆除费", "战斗中移动/拆除费用示例"],
    ]
    for r, row in enumerate(notes, 1):
        for c, v in enumerate(row, 1):
            cell = ws.cell(r, c, v)
            if r == 1:
                cell.font = Font(bold=True, size=14)
            if c == 1 and v and not str(v).startswith("docs"):
                cell.font = Font(bold=True)
    ws.column_dimensions["A"].width = 18
    ws.column_dimensions["B"].width = 72

    # --- 01 全局 ---
    ws = wb.create_sheet("01_全局常量")
    rows = [
        ["起始金币", 50, "Economy.StartingGold"],
        ["法师生命", 3, "Mage.MaxLives"],
        ["手牌格", 8, "HandController.SlotCount"],
        ["商店槽", 6, "ShopController.SlotCount"],
        ["攻击最高等级", 5, "ModulePricing.MaxAttackLevel"],
        ["采矿最高等级", 3, "Miner"],
        ["本局商店池上限", 12, "RunModulePool.MaxSize"],
        ["开局解锁", "激光+收束器+寒冰+火花", ""],
        ["分解返还率", 0.30, "invested×30%，至少1"],
        ["棋盘逻辑尺寸", "7×7", ""],
        ["可建造起步", "3×3", "BoardExpandService"],
        ["扩展到5×5费用", 100, ""],
        ["扩展到7×7费用", 300, ""],
        ["全场球上限", 40, "EnergyBallManager.maxBalls"],
        ["黄潮刷怪间隔系数", 0.55, "相对本波基础间隔"],
        ["刷新费公式", "3+2×stage", "同阶段恒定，不递增"],
        ["波末商店", "进入准备免费自动刷新(波≥2)", ""],
    ]
    write_table(ws, 1, ["项", "值", "备注"], rows)
    autosize(ws)

    # --- 02 怪物 ---
    ws = wb.create_sheet("02_怪物")
    rows = [
        ["红(Normal)", "红HP表", 1.5, 5, 10, 10, 25, 50, "波1起；每5波换档"],
        ["黄(Swarm)", "ceil(红×0.5)", 3.0, 1, 5, 5, 13, 25, "波5起；最快"],
        ["蓝(Tank)", "红×4", 0.75, 20, 40, 40, 100, 200, "波8起；高血低速"],
    ]
    end = write_table(
        ws,
        1,
        ["类型", "HP关系", "移速", "旧版点数", "波1 HP", "波5 HP", "波10 HP", "波15 HP", "出现/备注"],
        rows,
    )
    ws.cell(end + 1, 1, "说明：运行时以固定配额和逐波HP为准；旧版点数不再决定刷怪数量。")
    ws.cell(end + 1, 1).fill = NOTE_FILL
    autosize(ws)

    # --- 03 激光 ---
    ws = wb.create_sheet("03_模块_激光")
    rows = []
    for lv in range(1, 6):
        dmg = laser_dmg(lv)
        cap = laser_cap(lv)
        iv = laser_interval(lv)
        # 1 energy per shot
        dpe = dmg
        max_fire_dps = dmg / iv  # if infinite energy
        rows.append([lv, dmg, 1, cap, round(iv, 4), round(1 / iv, 2), dpe, round(max_fire_dps, 1), "伤=5×1.8^(lv-1)；通常能量受限"])
    write_table(
        ws,
        1,
        ["等级", "单发伤害", "能耗/发", "储能上限", "开火间隔s", "射速上限/s", "伤/能", "无限能量时DPS", "备注"],
        rows,
    )
    autosize(ws)

    # --- 04 炸弹 ---
    ws = wb.create_sheet("04_模块_炸弹")
    rows = []
    for lv in range(1, 6):
        dmg = bomb_dmg(lv)
        rad = bomb_radius(lv)
        cap = bomb_cap(lv)
        iv = 1 / 1.5
        ep = 5
        dpe = dmg / ep
        rows.append([lv, dmg, ep, cap, round(iv, 4), 1.5, round(rad, 2), round(dpe, 2), "投向开火时最左怪位置再AOE；无目标不耗能"])
    write_table(
        ws,
        1,
        ["等级", "爆炸伤害", "能耗/发", "储能上限", "开火间隔s", "射速/s", "AOE半径", "伤/能(单目标)", "备注"],
        rows,
    )
    autosize(ws)

    # --- 05 寒冰 ---
    ws = wb.create_sheet("05_模块_寒冰")
    rows = []
    for lv in range(1, 6):
        dmg = 5
        ep = 2
        cap = ice_cap(lv)
        iv = ice_interval(lv)
        slow = ice_slow(lv)
        dur = ice_slow_dur(lv)
        dpe = dmg / ep
        rows.append([lv, dmg, ep, cap, round(iv, 4), round(slow, 2), dur, round(dpe, 2), "减速取最大刷新时长；价值在拉长到达时间"])
    write_table(
        ws,
        1,
        ["等级", "单发伤害", "能耗/发", "储能上限", "开火间隔s", "减速%", "减速时长s", "伤/能", "备注"],
        rows,
    )
    autosize(ws)

    # --- 06 采矿 ---
    ws = wb.create_sheet("06_模块_采矿")
    rows = []
    for lv in range(1, 4):
        cost = miner_cost(lv)
        gold_per = miner_gold(lv)
        cd = 3.0
        # theoretical gold/s if energy infinite
        gps = gold_per / cd
        rows.append([lv, cost, gold_per, cd, round(gps, 3), 3, 10, "全图最多3台同时产出；与攻击抢能量"])
    write_table(
        ws,
        1,
        ["等级", "能耗/次", "产金/次", "冷却s", "无限能量金/s", "全图活跃上限", "储能下限≈能耗", "备注"],
        rows,
    )
    autosize(ws)

    # --- 07 收束 ---
    ws = wb.create_sheet("07_收束器")
    rows = [
        ["收束器", "路径", 15, "直角连通两口；R旋转；环路核心", "不造成伤害；提高球投能次数"],
    ]
    write_table(ws, 1, ["名称", "类型", "基础价", "机制", "平衡角色"], rows)
    autosize(ws)

    # --- 08 发射器 ---
    ws = wb.create_sheet("08_发射器")
    fire = [0.50, 0.70, 0.95, 1.25]
    speed = [4.0, 5.5, 7.0, 8.5]
    mass = [1, 2, 3, 4]  # 削弱：原 1/2/5/10
    life = [12, 20, 32, 50]
    rows = []
    for i in range(4):
        eps = fire[i] * mass[i]
        rows.append([i + 1, fire[i], speed[i], mass[i], life[i], round(eps, 2), "能量/秒=射速×质量（进板前）"])
    end = write_table(
        ws,
        1,
        ["档位", "射速(/s)", "球速(格/s)", "质量(能量)", "寿命(s)", "能量/秒", "备注"],
        rows,
    )
    ws.cell(end + 1, 1, "开局=档1。波5/10升级各+1档于所选维。寿命越长环路投能次数越高。")
    ws.cell(end + 1, 1).fill = NOTE_FILL
    # combo extremes
    end2 = end + 3
    ws.cell(end2, 1, "组合极端（能量/秒）")
    ws.cell(end2, 1).font = Font(bold=True)
    combos = [
        ["全开局", 0.5 * 1, "0.50"],
        ["满射速+开局质量", 1.25 * 1, "1.25"],
        ["开局射速+满质量", 0.5 * 4, "2.00"],
        ["满射速+满质量", 1.25 * 4, "5.00"],
    ]
    write_table(ws, end2 + 1, ["情景", "能量/秒", "验算"], combos)
    autosize(ws)

    # --- 09 波次 ---
    ws = wb.create_sheet("09_波次曲线")
    rows = []
    previous_total_hp = None
    for w in range(1, 26):
        normal_count, swarm_count, tank_count = wave_counts(w)
        normal_hp, swarm_hp, tank_hp = enemy_hp(w)
        enemy_count = normal_count + swarm_count + tank_count
        total_hp = wave_total_hp(w)
        hp_growth = "" if previous_total_hp is None else total_hp / previous_total_hp - 1
        pts = wave_budget(w)
        iv = spawn_interval(w)
        spawn_seconds = estimated_spawn_seconds(w)
        target_min, target_max = TARGET_COMBAT_SECONDS[wave_index(w)]
        gb = gold_budget(w)
        kill = round(gb * 0.70)
        clear = round(gb * 0.20)
        perfect = max(0, gb - kill - clear)
        unlock = ""
        if w % 3 == 0 and w < 25:
            unlock = "模块Draft"
        if w % 5 == 0 and w < 25:
            unlock = (unlock + "+" if unlock else "") + "发射器"
            unlock += "+祝福束缚"
        rows.append(
            [
                w,
                stage(w),
                normal_count,
                swarm_count,
                tank_count,
                enemy_count,
                normal_hp,
                swarm_hp,
                tank_hp,
                total_hp,
                hp_growth,
                pts,
                round(iv, 3),
                round(spawn_seconds, 1),
                f"{target_min}–{target_max}",
                prep_seconds(w),
                gb,
                kill,
                clear,
                perfect,
                refresh_cost(w),
                unlock,
            ]
        )
        previous_total_hp = total_hp
    write_table(
        ws,
        1,
        [
            "波",
            "商店阶段",
            "红数量",
            "黄数量",
            "蓝数量",
            "总数量",
            "红HP",
            "黄HP",
            "蓝HP",
            "总HP",
            "总HP环比",
            "旧版点数等价",
            "基础间隔s",
            "预计刷怪段s",
            "目标战斗时长s",
            "准备s",
            "本波金币预算",
            "击杀池70%",
            "清波20%",
            "完美剩余",
            "刷新费",
            "事件",
        ],
        rows,
    )
    for row in range(2, 27):
        ws.cell(row, 11).number_format = "0%"
    autosize(ws, max_w=22)

    # --- 10 商店价格 ---
    ws = wb.create_sheet("10_商店价格")
    price_waves = (1, 3, 5, 6, 8, 10, 11, 13, 15, 16, 20, 21, 25)
    headers = ["模块", "等级"] + [f"波{w}价" for w in price_waves]
    rows = []
    for kind, name, max_lv in [
        ("laser", "查理激光塔", 5),
        ("bomb", "大卫炸弹塔", 5),
        ("ice", "查理寒冰塔", 5),
        ("blackhole", "黑洞发射器", 5),
        ("miner", "比特币采矿机", 3),
        ("redirector", "收束器", 1),
    ]:
        for lv in range(1, max_lv + 1):
            row = [name, lv]
            for w in price_waves:
                row.append(shop_price(kind, lv, w))
            rows.append(row)
    write_table(ws, 1, headers, rows)
    autosize(ws)

    # --- 11 能量DPS对照 ---
    ws = wb.create_sheet("11_能量DPS对照")
    ws.cell(1, 1, "理论：无环、能量全进激光、无浪费 → DPS = 能量/秒 × 伤/能")
    ws.cell(1, 1).fill = NOTE_FILL
    ws.merge_cells("A1:H1")

    # emitter scenarios × laser levels vs wave HP
    headers = [
        "发射器情景",
        "能量/秒",
        "激光Lv",
        "伤/能",
        "理论DPS(无环)",
        "波15混合HP",
        "波15清场秒(无环)",
        "波25混合HP",
        "波25清场秒(无环)",
        "备注",
    ]
    scenarios = [
        ("开局 0.5×1", 0.5),
        ("射速满×质量1", 1.25),
        ("射速0.5×质量满", 2.0),
        ("双满 1.25×4", 5.0),
    ]
    hp15 = wave_total_hp(15)
    hp25 = wave_total_hp(25)
    rows = []
    for name, eps in scenarios:
        for lv in (1, 3, 5):
            dpe = laser_dmg(lv)  # 1 energy per shot
            dps = eps * dpe
            t15 = hp15 / dps if dps > 0 else None
            t25 = hp25 / dps if dps > 0 else None
            rows.append(
                [
                    name,
                    eps,
                    lv,
                    dpe,
                    round(dps, 1),
                    round(hp15),
                    round(t15, 1) if t15 else "-",
                    round(hp25),
                    round(t25, 1) if t25 else "-",
                    "环路会把有效投能倍率抬高；实机校准",
                ]
            )
    write_table(ws, 3, headers, rows)

    # loop multiplier note table
    r0 = 3 + len(rows) + 3
    ws.cell(r0, 1, "环路粗算（需实机填）")
    ws.cell(r0, 1).font = Font(bold=True)
    loop_headers = ["情景", "假设投能倍率", "相对无环DPS", "你的实机笔记"]
    loop_rows = [
        ["直路无环", 1.0, "×1", ""],
        ["小环约2攻击格", 2.0, "×2（示意）", ""],
        ["四收束紧环", "3~8?", "待测", "填实测清场时间反推"],
    ]
    write_table(ws, r0 + 1, loop_headers, loop_rows)

    # red TTK
    r1 = r0 + 6
    ws.cell(r1, 1, "单体斩杀：关键波怪物HP / 激光伤")
    ws.cell(r1, 1).font = Font(bold=True)
    ttk_rows = []
    for lv in range(1, 6):
        d = laser_dmg(lv)
        wave5 = enemy_hp(5)
        wave10 = enemy_hp(10)
        wave15 = enemy_hp(15)
        ttk_rows.append(
            [
                lv,
                d,
                math.ceil(wave5[0] / d),
                math.ceil(wave10[0] / d),
                math.ceil(wave15[0] / d),
                math.ceil(wave15[1] / d),
                math.ceil(wave15[2] / d),
            ]
        )
    write_table(
        ws,
        r1 + 1,
        ["激光Lv", "伤害", "波5红发数", "波10红发数", "波15红发数", "波15黄发数", "波15蓝发数"],
        ttk_rows,
    )
    autosize(ws, max_w=24)

    # --- 12 拆除 ---
    ws = wb.create_sheet("12_拆除费")
    headers = ["模块", "等级", "波", "准备拆除", "战斗拆除/移动"]
    rows = []
    for kind, name in [
        ("laser", "激光"),
        ("redirector", "收束器"),
        ("bomb", "炸弹"),
        ("ice", "寒冰"),
        ("miner", "采矿"),
        ("blackhole", "黑洞"),
    ]:
        max_lv = 3 if kind == "miner" else (1 if kind == "redirector" else 5)
        for lv in range(1, min(3, max_lv) + 1):
            for w in (5, 10, 15, 20, 25):
                rows.append([name, lv, w, 0, dismantle_cost(kind, lv, w, True)])
    write_table(ws, 1, headers, rows)
    autosize(ws)

    wb.save(OUT)
    check = load_workbook(OUT, read_only=True, data_only=True)
    wave_sheet = check["09_波次曲线"]
    for wave, expected_count, expected_hp in (
        (1, 5, 50),
        (5, 19, 145),
        (6, 26, 506),
        (10, 65, 1568),
        (11, 71, 3300),
        (15, 106, 5300),
        (20, 180, wave_total_hp(20)),
        (25, 278, wave_total_hp(25)),
    ):
        row = wave + 1
        actual_count = wave_sheet.cell(row, 6).value
        actual_hp = wave_sheet.cell(row, 10).value
        assert actual_count == expected_count, (wave, actual_count, expected_count)
        assert actual_hp == expected_hp, (wave, actual_hp, expected_hp)
    check.close()
    print(f"Wrote and verified {OUT}")


if __name__ == "__main__":
    main()
