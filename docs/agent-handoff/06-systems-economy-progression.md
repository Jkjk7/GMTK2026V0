# 06 — 经济与成长进度

## 1. 金币（`Economy`）

- 起始 **50**。
- 来源：击杀（受波击杀池封顶）、清波奖、完美防守奖、矿机、祝福 GoldBurst、分解返还。
- 支出：商店、刷新、扩展、战斗中移动/拆除费。

## 2. 波金币预算（`WaveGoldBudget`）

```text
budget = max(20, RoundToFive(round(18 × 1.205^(w-1))))  // w 钳制 1..25
```

约：波1=20，波5=40，波10=100，波15=250，波20≈640，波25≈1635。

拆分：

| 份额 | 用途 |
|------|------|
| 70% | 击杀池（按类型权重分摊，单怪随机区间再封顶） |
| 20% | 清波固定奖 |
| 剩余 | 完美防守（本波零漏） |

漏怪 → 完美奖作废。清屏消失的怪不给击杀金。

## 3. 商店（`ShopController` + `RunModulePool`）

- **6 槽**；池内按稀有度权重抽取（`ModuleCatalog.GetShopWeight`）。
- 本局池上限 **12**；开局 4 种。
- **货架等级**：`ShopMaxOfferLevel = 1`（暂时只卖 Lv1）。
- 定价见 `07`；刷新费 `5 + stage×(7+stage)`（同 stage 恒定）。

## 4. 手牌

- **8 槽**；可手牌内合成；可拖到棋盘/分解区。

## 5. 拆除与分解

| 行为 | 费用/返还 |
|------|-----------|
| 准备阶段移动 | 免费 |
| 战斗中移动/拆除 | 攻击约参考价 12%，功能 6%，至少 1 |
| 分解区 scrap | 返还 invested×30%（投入>0 至少 1） |

## 6. 模块解锁草稿（每 3 波，wave&lt;25）

`ModuleUnlockDirector` 候选（已在池的去掉）：

- 常驻候选：Bomb, Miner, Portal, Relay, Accelerator, FireEnchant, Surprise, Heatwave
- 波≥6：FlameAmp
- 波≥9：BlackHole, Fusion, Fission
- 波≥12：Splitter  

池满时选项带「替换已有」。  
注意：寒冰/火花已在开局池，**不再**进解锁候选。

## 7. 熔炉升级草稿（每 5 波，wave&lt;25）

从未满级的四维中抽 3 个选项（不足则更少）。升一档立即生效。

## 8. 祝福 + 束缚（每 5 波捆绑三选一）

一张卡 = **一个祝福 + 一个束缚**。  
Tier：每次提供时 `BlessingTier` +1，钳制 1–4，影响祝福/束缚强度。

### 祝福（摘要）

| ID | 效果随 tier |
|----|-------------|
| GoldBurst | +100/200/300/400 金 |
| TimeGift | +20/30/40/50 s 沙 |
| RareWeapon | 随机高稀有武器入手牌 |
| BombRadius | AOE×1.2（总上限 2.5×） |
| BoardExpandDiscount | 下次扩展半价（已满则补金） |
| BurnDamageUp | 灼烧 +5/跳 |
| EnchantRandomCells | 随机附魔 1–4 格 |

### 束缚（摘要）

| ID | 效果 |
|----|------|
| CurseCells | 诅咒 1–4 可建造格（有塔则卸走） |
| LockModules | 永久锁 1–4 个已放模块 |
| TimeTax | −25 / 37.5 / 50 / 62.5 s 沙 |
| PoolPurge | 从商店池移除若干（保核心） |
| EnemyHaste | 敌人移速 +8%（上限 1.5×） |

## 9. 棋盘扩展

`BoardExpandService`：3×3 → 5×5（100）→ 7×7（300）。  
扩展半价祝福可消费一次。
