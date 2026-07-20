# UI / 文档进度（ui-design-finish）

对照 `Newchange.txt` 实施顺序：

| 步骤 | 内容 | 状态 |
|------|------|------|
| 1 | 静态外框 + 右侧栏布局 | ✅ |
| 2 | GridBoard 相对 Transform 坐标 | ✅ |
| 3 | BattleLane 独立锚点 | ✅ |
| 4 | 手牌/商店槽位表现（ModuleSlotView Hover/Selected） | ✅ 运行时创建（未做独立 Prefab 资源） |
| 5 | HUD：机会 ◆◆◆、漏怪警告、CanvasGroup 胜负淡入 | ✅ |
| 6 | GameSkin + PrototypeSprites fallback | ✅（可放 `Resources/Game/GameSkin.asset`） |
| 7 | 反馈：槽位 Hover、放置绿/红幽灵、格高亮、发射器呼吸、漏怪红闪 | ✅ 基础版 |

## 仍可选（未阻塞游玩）

- 把运行时 UI 做成真正的 `GameLayout.prefab` / `HandSlot.prefab`（需在 Unity 编辑器里拖拽保存）
- TextMeshPro 替换 uGUI Text
- 正式像素图替换色块
- Sorting Layer 项目设置
- 音效 clip 挂到 `UIAudioFeedback`

## 手动验证

1. Play，确认棋盘顶不穿战斗窗
2. 鼠标悬停商店/手牌槽有高亮
3. 选中手牌后，可放置格幽灵为绿色，不可放置为红色
4. 漏怪时屏幕红闪 +「防线突破」
5. 胜利/失败遮罩淡入且挡住点击
6. 无 `GameSkin.asset` 时仍能启动
