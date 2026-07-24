# 02 — 文件地图

根：`D:\unity\GMTK2026塔防V0`  
玩法代码几乎全在 `Assets/Scripts/`。

## 1. 仓库顶层（你需要关心的）

| 路径 | 用途 |
|------|------|
| `Assets/Scripts/` | **全部玩法与 UI 逻辑** |
| `Assets/Scenes/` | 可为空；Bootstrap 自举 |
| `docs/` | 数值文档、旧说明、本交接包 |
| `docs/agent-handoff/` | **本交接文档包（优先读）** |
| `.cursor/skills/gmtk-v01-balance/` | 数值平衡 Agent Skill |
| `Library/` `Temp/` `Logs/` | Unity 生成，**勿提交/勿当真相** |

## 2. `Assets/Scripts/` 子目录

| 目录 | 职责 |
|------|------|
| `Game/` | 启动与世界组装 |
| `Core/` | 枚举、目录、坐标、占位美术 |
| `Board/` | 棋盘、熔炉、扩展、熔炉升级数据 |
| `Ball/` | 能量球 |
| `Modules/` | 全部可放置模块 |
| `Combat/` | 波次、敌人、沙漏、伤害、草稿导演、投射物 |
| `Economy/` | 金币、卡牌数据、定价、商店池、波金预算 |
| `UI/` | 手牌商店放置 HUD 等 |

---

## 3. 逐文件用途

### Game/

| 文件 | 用途 |
|------|------|
| `GameBootstrap.cs` | 运行时创建世界+UI+接线；改布局/启动流程看这里 |

### Core/

| 文件 | 用途 |
|------|------|
| `ModuleType.cs` | 17 种模块枚举（0–16） |
| `ModuleRarity.cs` | Common/Rare/Epic/Legendary |
| `ModuleCatalog.cs` | 名称、描述、稀有度、商店权重、伤害/效果公式、显示色 |
| `CellEnchant.cs` | 格附魔：None/Flame/DamageUp/Frost/Shrink/Cooldown |
| `GridCoord.cs` | 格子坐标 |
| `GridDirection.cs` | 四向 + 工具 |
| `GameSkin.cs` | Sprite 配置，缺省回退占位 |
| `PrototypeSprites.cs` | 运行时白方块/圆 |

### Board/

| 文件 | 用途 |
|------|------|
| `GridBoard.cs` | 7×7 模块/诅咒/附魔、坐标、放置 |
| `BoardExpandService.cs` | 可建造窗 3→5→7、费用、锁定格视觉 |
| `Emitter.cs` | 战斗抽沙、攒容量射球到入口 |
| `EmitterRunUpgrades.cs` | 本局四维：容量/球速/质量/寿命 |

### Ball/

| 文件 | 用途 |
|------|------|
| `EnergyBall.cs` | 飞行、寿命、过格心触发、加速标记、续命 API |
| `EnergyBallManager.cs` | TrySpawn、上限 40、清场 |

### Modules/

| 文件 | 用途 |
|------|------|
| `ModuleBase.cs` | 卡牌、绑定格、附魔射速/CD 倍率、永久锁定 |
| `PathGeometry.cs` | PathShape Straight/Bent/Tee 端口 |
| `PathEffectModule.cs` | 路径功能基类：旋转、Bent、改向 |
| `RedirectorModule.cs` | 收束器 L 形 |
| `ProjectileModule.cs` | 查理激光 |
| `BombModule.cs` | 大卫炸弹 |
| `IceLaserModule.cs` | 雪花发射塔 |
| `SparkModule.cs` | 火花发射塔 |
| `MinerModule.cs` | 比特币采矿 |
| `BlackHoleModule.cs` | 黑洞 |
| `FlameAmpModule.cs` | 火焰增幅（全局灼烧） |
| `SplitterModule.cs` | 分裂器 Tee |
| `PortalModule.cs` | 传送门（≤2） |
| `RelayModule.cs` | 中续器 |
| `AcceleratorModule.cs` | 加速 |
| `FusionModule.cs` | 核聚变 |
| `FissionModule.cs` | 核裂变 |
| `FireEnchantModule.cs` | 火附魔 + `EnchantSeedUtil` |
| `SurpriseModule.cs` | 惊喜附魔 |
| `HeatwaveModule.cs` | 热浪 + 全屏红闪 |

### Combat/

| 文件 | 用途 |
|------|------|
| `GameSession.cs` | Preparing/Playing/Victory/Defeat |
| `WaveManager.cs` | 准备/倒计时/刷怪/清波/草稿链 |
| `WaveSpawnBudget.cs` | **25 波固定表**（数量/HP/间隔/沙buff） |
| `SandClock.cs` | 时间资源、抽沙、罚沙、补沙、败 |
| `Enemy.cs` | 移动、HP、烧/寒、沙buff、漏怪 |
| `BattleLane.cs` | 刷怪点与终点 |
| `Mage.cs` | 终点；漏怪回调罚沙 |
| `CombatDamage.cs` | 统一伤害：附魔、融化、挂状态 |
| `CombatHUD.cs` | 状态字、漏怪闪、结果 |
| `DamageTracker.cs` | 伤害统计 |
| `RunModifiers.cs` | 本局 AOE/移速/灼烧加成等 |
| `ModuleUnlockDirector.cs` | 每 3 波模块三选一 |
| `EmitterUpgradeDirector.cs` | 每 5 波熔炉三选一 |
| `BlessingCurseDirector.cs` | 每 5 波祝福+束缚三选一 |
| `BombProjectile.cs` | 炸弹弹道 AOE |
| `BlackHoleProjectile.cs` | 黑洞吸引场 |
| `ArcSparkProjectile.cs` | 弧线火花/雪花弹 |
| `SandVfxService.cs` / `SandGrainVfx.cs` | 沙粒子 |

### Economy/

| 文件 | 用途 |
|------|------|
| `Economy.cs` | 全局金币，起始 50 |
| `ModuleCardData.cs` | Type/Level/InvestedGold/Bent；合成规则 |
| `ModulePricing.cs` | 商店价、刷新、拆除、准备秒、扩展费 |
| `RunModulePool.cs` | 本局可售池，上限 12 |
| `WaveGoldBudget.cs` | 波预算与击杀/清波/完美拆分 |
| `GoldDropService.cs` | 飞金币入账 |

### UI/

| 文件 | 用途 |
|------|------|
| `PlacementController.cs` | 放置/移动/合成/分解/扩展/幽灵预览 |
| `HandController.cs` / `HandSlot.cs` | 8 槽手牌 |
| `ShopController.cs` / `ShopSlot.cs` | 6 槽商店 |
| `ModuleSlotView.cs` | 槽位视觉 |
| `ModuleTooltipView.cs` | 悬停详情 |
| `ScrapZone.cs` | 分解区 |
| `ConfirmPromptView.cs` | 确认框 |
| `DraftChoiceView.cs` | 三选一 UI |
| `PrepPhasePanel.cs` | 准备倒计时与 Ready |
| `SandClockPanel.cs` | 沙漏显示 |
| `GoldPanel.cs` | 金币栏 |
| `Combat` 相关 HUD：`BallCountHud` `WaveCountdownHud` `RunStatsHud` `ResultOverlayView` `GameLayoutView` `GridCellView` `CoinFlyVfx` `UIAudioFeedback` |

---

## 4. 改某功能时「常改文件组」

**新攻击塔**：`Modules/X` + `ModuleType` + `Catalog` + `Pricing` + `PlacementController` + `Unlock` + `Tooltip` +（储能则）`WaveManager.ClearEnergyOnBoard`

**难度曲线**：`WaveSpawnBudget` + `generate_balance_workbook.py` +（可选）`SandClock` 罚沙表

**合成规则**：`ModuleCardData` + `PlacementController.TryApplyBoardFuse` + 手牌 `HandController.TryFuseIntoSlot`

**UI 位置**：几乎只动 `GameBootstrap.cs`
