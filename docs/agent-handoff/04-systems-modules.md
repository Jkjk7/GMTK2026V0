# 04 — 模块系统（17 种）与合成

## 1. 卡牌数据（`ModuleCardData`）

```csharp
Type, Level, InvestedGold, Bent, InstanceSeed
```

- **Level**：攻击类/火焰增幅/附魔/热浪 1–5；矿机 1–3；多数路径功能固定 1。
- **InvestedGold**：购买价累加；分解返还 `max(1, floor(invested×0.30))`（投入>0 时）。
- **Bent**：路径功能模块是否为 L 拐弯版。
- **InstanceSeed**：火附魔/惊喜卡的稳定实例种子；其他模块为 0。

### 合成规则

**A. 同型升级**  
同 `Type`、同 `Level`、同 `Bent`，且未满级 → `Level+1`，金币相加。

**B. 收束器 × 可拐弯功能**（`CanBendFuseWith`）  
一方 `Redirector`，另一方为 `{Portal, Relay, Accelerator, Fusion, Fission}` 且 **尚未 Bent**  
→ 结果为**功能模块类型** + `Bent=true`（保留功能模块等级，金币相加）。  
若棋盘上占格类型变了（收束器被换成传送门等），`PlacementController.TryApplyBoardFuse` 会 **拆旧建新**。

**不可拐弯合成**：分裂器、火附魔、惊喜、热浪、收束器彼此、已 Bent 再合收束器。

合成可发生在：手牌槽、手牌→棋盘、棋盘→棋盘、商店拖到已有模块上。

---

## 2. 分类速查

| 类别 | 类型 | 旋转 | 升级 | 稀有度 |
|------|------|------|------|--------|
| 路径拐弯 | Redirector | ✓ | ✗ | Rare |
| 路径功能 | Portal Relay Accelerator Fusion Fission | ✓ | ✗（Bent 变体） | Rare/Epic |
| 路径分裂 | Splitter | ✓ Tee | ✗ | Epic |
| 攻击 | Projectile Bomb IceLaser Spark BlackHole Heatwave | ✗ | ✓ | C/C/C/C/E/R |
| 经济 | Miner | ✗ | ✓≤3 | Rare |
| 被动增幅 | FlameAmp | ✗ | ✓ | Rare |
| 被动附魔 | FireEnchant Surprise | ✗ | ✓ 格数 | Rare |

`ModuleCatalog.IsAttackModule` 含 Heatwave。  
开局池：Projectile, Redirector, IceLaser, Spark。

---

## 3. 攻击 / 经济 / 被动（行为摘要）

### 查理激光 `ProjectileModule`
- 吸能；对**最近**敌人激光。
- 伤 `Round(5×1.8^(Lv-1))`；储能 `5+2×(Lv-1)`；基础间隔 0.2s（约 5 发/秒），等级略加快。

### 大卫炸弹 `BombModule`
- 对**最左**敌人投弹，落地 AOE。
- 伤 `Round(15×1.5^(Lv-1))`；半径受等级与 `RunModifiers.AoeRadiusMult`。
- 容约 20+，耗 5，射速约 1.5/秒。

### 雪花 `IceLaserModule`
- 弧线淡蓝弹（`ArcSparkProjectile`）；**伤害恒定 5**；寒冷 30%，时长 `2+(Lv-1)`s。
- 能耗 1。可与火花触发**融化**。

### 火花 `SparkModule`
- 弧线橙红弹；挂灼烧；伤 Lv1–2:1 / 3–4:2 / 5:3；烧时长 `2+0.5×(Lv-1)`。
- 能耗 1；储能容量随等级升。

### 黑洞 `BlackHoleModule`
- Epic；耗 5 / 容 5 / **3 秒一发**；吸引场聚怪。
- 半径/时长/吸力随等级；半径×AOE 倍率。

### 热浪 `HeatwaveModule`
- 容 20 / 耗 20 / CD 5s（Cooldown 附魔可减半）。
- 全屏 `ApplyBurn`：时长 Lv1–4 → 2/3/4/5s（Lv5 钳 5）+ 红闪。

### 矿机 `MinerModule`
- 固定 10 能 → 产金 1/3/8（Lv1/2/3）；CD 3s；全图同时工作上限相关逻辑见模块内。

### 火焰增幅 `FlameAmpModule`
- 被动：提高全局灼烧每跳伤害（`RunModifiers.RecalcFlameAmp` 汇总场上）。
- 球进入不吸能。加成表：+1/3/5/7/10。

### 火附魔 / 惊喜
- 放置时按种子给 1–4 格写附魔（Lv=格数目标）。
- 每个火附魔/惊喜卡在 `ModuleCardData.InstanceSeed` 保存独立种子。
- 目标格只由模块类型 + 实例种子 + 等级步进决定，与模块放置格无关。
- 移动、回手牌再放置与升级均保留种子；升级确定性增加 1 格。
- 诅咒/不可建造格**跳过且不补抽**；目标坐标始终位于 7×7 棋盘内。
- 拆除：只清「仍是自己写入种类」的格。
- 火附魔固定 Flame；惊喜每格随机种类。

---

## 4. 路径模块细节

几何基类：`PathEffectModule` + `PathGeometry`。

| Shape | 含义 |
|-------|------|
| Straight | 对穿；orientation 偶=左右，奇=上下 |
| Bent | 同收束器 L 口表 orientation 0–3 |
| Tee | 柄=入口，两臂=出口（分裂器用） |

### 收束器 Redirector
- 仅 L；R 旋转；球从合法口进改向飞出。

### 传送门 Portal
- 场上 **≤2**（放置时 `CanPlaceTypeAt` 检查）。
- 成对：进 A → 瞬移到 B 格心，**保持世界飞行方向**；同帧防环。
- 仅 1 座：当直通路径。

### 中续器 Relay
- 球飞入：**吸收销毁**，能量累加至 cap **20**。
- 当储能 **>0** 时，下一球**不吸收**，`RefreshLifetime()`，清空储能，球继续（并改向出口）。

### 加速 Accelerator
- 若 `!HasAccelerated`：速度 ×**1.5** 并打标；然后改向飞出。

### 核聚变 Fusion
- 吸收恰好 **5** 颗球（记录各球 speed/life/energy）。
- 射出 1 颗：energy=Σ，life/speed=平均；方向=出口。

### 核裂变 Fission
- 吸收能量累加；≥**5** 后 0.5s 内依次射 **5** 颗「默认球」：能1、寿12、速**8**（4×2），再重置。
- 迸发中来球改为放行改向。

### 分裂器 Splitter
- Tee；原球销毁；左右口各生成同能量、**剩余寿命×0.5** 的球。
- 受 40 球上限约束，空间不够则尽量生成。
- **不可**与收束器 Bent 合成。

---

## 5. 格子附魔对模块的影响（`ModuleBase` / `CombatDamage`）

| 附魔 | 效果 |
|------|------|
| Flame | 命中至少挂 3s 灼烧 |
| Frost | 命中至少挂 3s 寒 |
| DamageUp | 伤害 ×1.2 |
| Shrink | 伤害 ×0.5；射速翻倍（间隔×0.5） |
| Cooldown | 有 CD 的模块 CD×0.5 |

诅咒格：不可建造；卸模块时可能回手/分解（祝福束缚流程）。

永久锁定：`IsPermanentlyLocked` — 不可移/拆/合成，仍可开火。

---

## 6. 新增模块接线清单

见 `00-START-HERE.md` 第 2 节；漏接线会导致商店能买但放不出来，或幽灵预览为空。
