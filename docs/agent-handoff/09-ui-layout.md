# 09 — UI 与前端结构

## 1. 原则

- UI **主要由 `GameBootstrap` 代码创建**，不是靠场景拖好的完整 Canvas Prefab。
- 布局用视口比例 / 世界坐标对齐（沙漏列 x≈0.08，熔炉与其对齐）。
- `GameLayoutView` 只是引用 hub，方便其它系统拿锚点。

## 2. 区域职责

| 区域 | 内容 |
|------|------|
| 上战斗道 | 敌人移动；沙漏面板覆盖 Mage；漏怪闪屏 |
| 左棋盘窗 | GridBoard、熔炉、球、球数 HUD、分解区、金币栏、增幅折叠 |
| 右侧栏 | 上手牌 8、下商店 6+刷新 |
| 覆盖层 | 准备面板、大倒计时、草稿三选一、确认框、胜负、Tooltip |

## 3. Bootstrap 创建的主要 UI 对象

| 对象 | 作用 |
|------|------|
| Canvas + Scaler | 全屏 UI |
| SandClockPanel | mm:ss.mmm + 沙漏视觉 + 罚沙字 |
| CombatHUD | 波次状态、breach、结果联动 |
| PrepPhasePanel | 准备倒计时、进度、Ready |
| DraftChoiceView | 三选一（模块/熔炉/祝福自定义文案） |
| Hand 8 / Shop 6 | 核心经营 UI |
| ScrapZone | 世界空间分解 |
| GoldPanel | 金币 |
| ModuleTooltipView | 长悬停模块/附魔 |
| ConfirmPromptView | 拆/分解确认 |
| WaveCountdownHud | 剩余波数 |
| RunStatsHud | 「查看已有增幅」默认折叠 |
| BallCountHud | 世界空间 `cur/max` |
| ResultOverlayView | 胜负 |
| Hint / ExpandHint | 操作与扩展费用提示 |

## 4. 输入与 PlacementController

`PlacementController` 是交互中枢：

- 手牌/商店拖放放置
- 棋盘拖移、合成、分解
- R 旋转
- 幽灵预览染色（可放绿/不可红）
- 传送门数量限制预览
- Bent 幽灵：对 `_ghostOther.ApplyCardData`

战斗中贵操作走 `ConfirmPromptView`。

## 5. 改 UI 时注意

- 改位置优先改 Bootstrap 里的锚点常量/viewport，而不是只改某一个 Panel 局部。
- 沙漏必须盖住 Mage 蓝块（视觉重复）。
- 分解区与金币区对称（右上/右下）是刻意布局。
- 旧「frame」图可能被禁用，边框程序化绘制。

更细的 UI 改造笔记见：`docs/UI改造说明-ui-design-finish.md`（若与代码冲突，以 Bootstrap 为准）。
