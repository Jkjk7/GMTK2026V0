# 03 — 核心循环：沙漏 · 熔炉 · 能量球 · 棋盘

## 1. 沙漏 = 生命（`SandClock`）

- **单位**：1 粒沙 = **1 毫秒**。
- **开局**：`InitialSandMs = 100_000`（显示约 01:40.000）。
- **归零**：`CheckDefeat()` → `GameSession.SetDefeat()`。

### 沙的增减来源

| 方向 | 来源 | 规则 |
|------|------|------|
| 减 | 熔炉抽沙 | 仅战斗中；`Emitter` 每帧 `TryDrain`，约 **1000 ms/秒** |
| 减 | 漏怪 | `ApplyBreachPenalty`：按类型基础 × stage 倍率 ×（沙buff则×1.5） |
| 减 | 束缚 TimeTax | 固定扣一段沙（仍留 floor） |
| 加 | 击杀沙 buff 怪 | **仅**带沙 buff 的怪；普通击杀不补沙 |
| 加 | 清波 | `GrantWaveClearReward`，按 stage 固定 |
| 加 | 祝福 TimeGift | 一次性加沙 |

### 漏怪基础罚沙（再乘 stage 倍率）

| 类型 | 基础 ms |
|------|--------:|
| 黄 Swarm | 3_000 |
| 红 Normal | 10_000 |
| 蓝 Tank | 30_000 |
| Boss | 90_000（蓝×3） |

Stage 倍率（波 1–5 / 6–10 / …）：**1.0 / 1.3 / 1.65 / 2.1 / 2.5**。

清波补沙 stage：**6 / 9 / 12 / 16 / 20** 秒。  
沙 buff 击杀爆沙 stage：**6 / 8 / 11 / 14 / 17.5** 秒。

---

## 2. 熔炉（`Emitter` + `EmitterRunUpgrades`）

- 位置：棋盘左侧，入口格 **(0, 3)**，球向右飞入。
- 仅 `GameSession` 战斗态抽沙。
- 逻辑：抽到的沙累加到「炉内」；达到 **FurnaceCapMs** → 射 1 球 → 炉内清零重累。

### 四维本局升级（每维 0–3 档，每 5 波三选一升一档）

| 维（枚举） | 显示名 | 档位值 |
|------------|--------|--------|
| FireRate | 熔炉容量 | 2000 → 1400 → 1050 → 800 **ms**（越小出球越快） |
| BallSpeed | 球速 | 4.0 → 5.5 → 7.0 → 8.5 **格/秒** |
| Mass | 质量 | 球能量 **1 → 2 → 3 → 4** |
| Lifetime | 存活 | 球寿命 **12 → 20 → 32 → 50** 秒 |

近似出球率（只看容量、抽沙 1000ms/s）：0.5 / ≈0.71 / ≈0.95 / 1.25 球/秒。

---

## 3. 能量球（`EnergyBall` / `EnergyBallManager`）

| 属性 | 说明 |
|------|------|
| Energy | 进入模块时加到储能（质量升级决定） |
| Direction | 四向飞行 |
| Lifetime | `_age` 到 `lifetimeSeconds` 销毁 |
| Speed | 格/秒 × `CellSize` |
| HasAccelerated | 加速模块只生效一次 |
| 上限 | 场上最多 **40** |

### 触发规则（重要）

- 球必须**越过格子中心**才 `OnBallEnter`（防斜穿误触）。
- 触发前 Snap 到格心。
- 同一格用 `_lastTriggeredCell` 防重复；分裂/传送后会 `MarkCellTriggered` + `NudgeAlongDirection` 避免同帧再进。

### 常用 API

- `RefreshLifetime()`：`_age=0`，保留 lifetimeSeconds（中续器）
- `SetRemainingLifetime` / `HalveRemainingLifetime`
- `SetSpeedCellsPerSecond` / `MarkAccelerated`
- `Despawn()` → Manager 移除计数

`TrySpawnBall(pos, dir, speed, life, energy)`：自定义参数生成（分裂/聚变/裂变用）。

---

## 4. 棋盘（`GridBoard`）

- 逻辑尺寸恒 **7×7**；**可建造窗口**由 `BoardExpandService` 控制：开局约中心 **3×3**，可扩到 5×5、7×7。
- 扩展费用：3→5 **100** 金；5→7 **300** 金（可被祝福半价一次）。
- 每格可有：模块、诅咒（不可建造/不可附魔）、附魔一种。
- 坐标约定：**(0,0)=左下**，Col 向右，Row 向上。

### 准备阶段清场（`WaveManager.PrepareBoardForPrepPhase`）

- 清除全部能量球。
- `ClearEnergyOnBoard`：清空各攻击塔/矿机/路径储能（Relay/Fusion/Fission/Heatwave 等）。

---

## 5. 能量如何变成伤害（一句话链路）

```
沙漏 --抽沙--> 熔炉攒满 --射球--> 路径模块导流
  --球进入攻击塔--> 储能 += Energy
  --储能够且 CD 好--> 开火 --> CombatDamage.Apply --> Enemy HP
```

无球/球走不到塔 = 塔是摆设。这就是「必须组路径/环」的原因。
