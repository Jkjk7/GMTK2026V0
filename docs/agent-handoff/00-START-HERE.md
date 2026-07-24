# 00 — START HERE（Agent 上手）

你正在接手 **GMTK 2026 游戏 Jam 向塔防原型**：代号 **GMTK2026塔防V0 / Roguelike25**。

## 0. 你必须先记住的硬事实

1. **没有传统「命数」**：玩家血量是 **沙漏剩余毫秒**（`SandClock`）。沙 = 0 → 败。
2. **输出靠能量球**：战斗时熔炉（`Emitter`）从沙漏抽沙 → 攒满容量射球 → 球飞入棋盘 → 模块吸能开火。
3. **棋盘是电路**：收束器等路径模块决定球怎么走；**组环/拐弯**比堆塔更重要。
4. **整局代码自举**：`GameBootstrap` 用 `[RuntimeInitializeOnLoadMethod]` 在运行时拼世界与 UI；改玩法优先改脚本，不要指望场景里已有 Prefab 层级。
5. **25 波通关**：打完波 25（含 Boss）且沙 > 0 → 胜。

## 1. 接到任务时怎么路由

| 用户说… | 你该读… | 主要改… |
|---------|---------|---------|
| 太难/太简单/前几波 | `07-balance-tables.md` + skill | `WaveSpawnBudget.cs`，同步 `docs/generate_balance_workbook.py` |
| 新模块 / 改模块效果 | `04-systems-modules.md` | `Modules/*` + `ModuleType` + `ModuleCatalog` + `ModulePricing` + `PlacementController` + Unlock |
| 合成 / 拐弯 | `04` 合成节 | `ModuleCardData.cs` + `PlacementController.TryApplyBoardFuse` |
| 沙漏/漏怪/爆沙 | `03` + `05` | `SandClock.cs` |
| 商店价/金币 | `06` + `07` | `ModulePricing` / `WaveGoldBudget` / `Economy` |
| UI 布局 | `09-ui-layout.md` | `GameBootstrap.cs` |
| 祝福/束缚/解锁 | `06` | `*Director.cs` / `RunModifiers` |

## 2. 改代码检查清单（防漏接线）

新增一种 `ModuleType` 时，至少检查：

- [ ] `ModuleType.cs` 枚举
- [ ] 实现类 `XxxModule.cs`
- [ ] `ModuleCatalog`：稀有度、名称、描述、颜色、`IsAttack/Utility/Path*`、公式
- [ ] `ModulePricing`：基础价 + `GetShopPrice` 分支（若可升级）
- [ ] `PlacementController.CreateModule` + `EnsureGhost`
- [ ] `ModuleUnlockDirector` 候选（若应进草稿）
- [ ] `ModuleTooltipView.BuildStats`（若需详情）
- [ ] `WaveManager.ClearEnergyOnBoard`（若有储能）
- [ ] 若可与收束器拐弯：`ModuleCatalog.CanBendWithRedirector`

改波次数量/间隔时：

- [ ] `WaveSpawnBudget.cs` 数组
- [ ] `docs/generate_balance_workbook.py` 同名数组
- [ ] 可选：`docs/金币经济数值表.md` 与本包 `07`

## 3. 绝对不要做的事

- 不要把「塔 DPS 相加」当作平衡模型。
- 不要假设场景里已有完整 UI Prefab——UI 多半在 Bootstrap 里 `new GameObject`。
- 不要编辑用户的 `.cursor/plans/*.plan.md`，除非用户明确要求。
- 不要把 `docs/机制总结与路线图.md` 里的「目标态」当成已上线功能。
- 不要在未要求时 `git commit` / `push`。

## 4. 本地验证最短路径

1. Unity 打开工程 → Play。
2. 应看到：左侧沙漏、中间棋盘+熔炉、右侧手牌/商店、上方战斗道。
3. 准备阶段买塔/收束器铺路 → Space/Ready → 战斗看球与开火。
4. 编译错误：可用 Unity MCP `refresh_unity` + `read_console`（若环境可用）。

## 5. 关键单例 / 全局入口

| 类型 | 访问 |
|------|------|
| `SandClock.Instance` | 沙 |
| `Economy.Instance` | 金 |
| `EmitterRunUpgrades.Instance` | 熔炉四维 |
| `RunModulePool.Instance` | 本局商店池 |
| `RunModifiers.Instance` | 祝福束缚累计 |
| `WaveGoldBudget.Instance` | 波金币 |
| `WaveManager.FindDisplayWave()` | 当前显示波号 |

## 6. 版本标签

- **Roguelike25**：25 波、四档稀有度、每 3 波模块草稿、每 5 波熔炉+祝福束缚。
- 开局池：查理激光 + 收束器 + 雪花 + 火花。
- 前 5 波已特意压难度（无环教学区）：红更少、间隔更慢、波 5 仅 4 黄、无沙 buff。
