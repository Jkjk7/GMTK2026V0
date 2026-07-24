# 如何把本包喂给另一个 Agent

把下面整段复制到新对话开头即可：

---

你在接手 Unity 项目 **GMTK2026塔防V0**（路径以仓库根为准）。  
请先完整阅读文件夹 `docs/agent-handoff/`，顺序：

1. `README.md`
2. `00-START-HERE.md`
3. `01-game-overview.md`
4. 按任务再读 `02`–`11`

硬规则：
- 真相优先级：代码 ≫ 本交接包 ≫ 旧 docs
- 这是「沙漏生命 + 能量球电路塔防」，不是传统塔 DPS 相加
- 改波次同步 `WaveSpawnBudget.cs` 与 `docs/generate_balance_workbook.py`
- 新模块必须接完 Catalog/Pricing/Placement/Unlock/Tooltip/ClearEnergy
- 不要编辑 `.cursor/plans/`；不要擅自 commit

用户接下来的需求：
（在此粘贴具体任务）

---

## 本包文件完整性检查

生成时应包含：

- [x] README.md
- [x] 00-START-HERE.md
- [x] 01-game-overview.md
- [x] 02-file-map.md
- [x] 03-systems-core-loop.md
- [x] 04-systems-modules.md
- [x] 05-systems-combat.md
- [x] 06-systems-economy-progression.md
- [x] 07-balance-tables.md
- [x] 08-design-philosophy.md
- [x] 09-ui-layout.md
- [x] 10-glossary.md
- [x] 11-quick-reference.md
- [x] HOW-TO-HANDOFF.md（本文件）
