# GMTK2026 塔防 V0 — Agent 交接文档包

> **用途**：把本文件夹整包交给「对项目一无所知」的 AI / 新成员，即可无缝理解并继续开发。  
> **生成日期**：2026-07-24  
> **项目根**：`D:\unity\GMTK2026塔防V0`  
> **引擎**：Unity 6（编辑器显示 6000.x）2D；运行时由 `GameBootstrap` 自举，几乎不依赖场景摆件。

---

## 先读这个（5 分钟）

1. 读 [`00-START-HERE.md`](00-START-HERE.md) — 项目定位、硬规则、改代码入口  
2. 读 [`01-game-overview.md`](01-game-overview.md) — 游戏是什么、怎么跑完一局  
3. 按任务跳转下面分册  

**改数值 / 难度** → [`07-balance-tables.md`](07-balance-tables.md) + Skill `.cursor/skills/gmtk-v01-balance/SKILL.md`  
**改模块 / 合成 / 路径** → [`04-systems-modules.md`](04-systems-modules.md)  
**改战斗 / 怪潮 / 沙漏** → [`05-systems-combat.md`](05-systems-combat.md)  
**改商店 / 金币 / 草稿** → [`06-systems-economy-progression.md`](06-systems-economy-progression.md)  
**找文件** → [`02-file-map.md`](02-file-map.md)  
**设计思路 / 坑** → [`08-design-philosophy.md`](08-design-philosophy.md)  
**UI 布局** → [`09-ui-layout.md`](09-ui-layout.md)  
**术语表** → [`10-glossary.md`](10-glossary.md)  
**一页速查** → [`11-quick-reference.md`](11-quick-reference.md)  
**复制给新 Agent 的提示词** → [`HOW-TO-HANDOFF.md`](HOW-TO-HANDOFF.md)

---

## 文档清单

| 文件 | 内容 |
|------|------|
| `00-START-HERE.md` | Agent 上手清单、禁止事项、任务路由 |
| `01-game-overview.md` | 简介、风格、运行形式、流程图、前端结构 |
| `02-file-map.md` | 全文件地图（文件夹 + 每个 `.cs` 用途） |
| `03-systems-core-loop.md` | 沙漏、熔炉、能量球、棋盘、扩展 |
| `04-systems-modules.md` | 全部 17 模块 + 合成/拐弯/附魔 |
| `05-systems-combat.md` | 敌人、波次、伤害、融化、灼烧 |
| `06-systems-economy-progression.md` | 金币、商店、手牌、解锁/熔炉/祝福草稿 |
| `07-balance-tables.md` | 干净数值表（波次/模块/经济/沙漏） |
| `08-design-philosophy.md` | 设计锚点、常见误区、如何安全改参 |
| `09-ui-layout.md` | UI 区域、Bootstrap 创建清单、操作 |
| `10-glossary.md` | 中英术语对照 |
| `11-quick-reference.md` | 一页速查 |

---

## 与旧文档的关系

| 旧路径 | 说明 |
|--------|------|
| `docs/金币经济数值表.md` | 经济主文档；本包 `07` 更干净，冲突时以**代码**为准 |
| `docs/generate_balance_workbook.py` | Excel 生成脚本；改波次表时请同步 |
| `docs/机制总结与路线图.md` | 含愿景/未落地内容；**不要当成已实现真相** |
| `.cursor/skills/gmtk-v01-balance/SKILL.md` | 数值 Agent 工作流 |

**真相优先级**：`Assets/Scripts/**/*.cs` ≫ 本交接包 ≫ 旧 docs / skill 快照。

---

## 一句话本质

这不是「多塔 DPS 相加」的传统塔防，而是：

> **沙漏时间 = 生命**；**熔炉抽沙射能量球**；球在 **7×7 棋盘模块网络**里投能；塔用能量打怪。  
> 有效输出 ≈ **熔炉吞吐 × 路径环路倍率 × 伤/能**。无环时理论 DPS 远低于四收束环。
