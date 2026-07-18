# UI 改造说明（ui-design-finish）

基于 `Newchange.txt` 第一阶段目标：统一游戏面板外观，不破坏棋盘/战斗逻辑。

## 分支

- 工作分支：`ui-design-finish`（从 `ui-redesign` 分出）
- 仓库中无 `ui-design-wip`，以 `ui-redesign` 作为 WIP 起点
- **不要直接改 `main`**

## 改了什么

### `Assets/Scripts/Game/GameBootstrap.cs`
- 主 Camera **全屏**（不再用 `Camera.rect` 裁掉底部/右侧，避免战斗区变窄）
- 加载 `Resources/UI/ui_game_frame` 作为整屏外框；找不到则程序化边框兜底
- 外框 `raycastTarget = false`，不挡点击
- 新建 **Sidebar**：上手牌、下商店（3 列网格）
- HUD / 胜负遮罩位置适配新布局
- 胜负面板全屏遮罩可拦截点击

### `Assets/Resources/UI/ui_game_frame.png`
- 1920×1080，**战斗窗与棋盘窗真正透明**（alpha=0）
- 右侧栏不透明，供手牌/商店叠放
- 导入设置：Sprite (2D and UI)、Point、无压缩、Alpha Is Transparency

### `Tools/gen_ui_game_frame.py`
- 生成上述透明外框的脚本（可重新跑）

## 未改（按文档第一阶段）

- `GridBoard` / `BattleLane` / `Enemy` / `WaveManager` 玩法逻辑
- 仍未做完整 `GameLayout.prefab`、多摄像机、TMP、GameSkin

## Unity 里你需要做的

1. 打开工程，等脚本编译完成
2. Game 窗口选 **1920×1080**，Scale 拉到 **1x / Fit**（不要 2x）
3. 选中 `Assets/Resources/UI/ui_game_frame.png`，确认 Inspector：
   - Texture Type = Sprite (2D and UI)
   - Filter Mode = Point
   - Alpha Is Transparency = 开
   - 预览里棋盘/战斗窗口应是**灰白格子**（透明）
4. Play

## 如何验证

- [ ] 能透过外框看到棋盘、敌人、能量球
- [ ] 手牌在右侧上方，商店在右侧下方
- [ ] 点商店/手牌不会在棋盘上放置/拆除
- [ ] F 刷新、买模块、放置、旋转、拆除仍正常
- [ ] 波次 / 机会 / 胜负仍正常
- [ ] Console 无红错；应有「UI 外框加载成功」或程序化边框警告

## 旧资源注意

- `Assets/Art/UI/ui_game_frame.png.jpg` 是 **JPG**，无透明通道，**不要**再当外框用
- 正式外框必须是 `Assets/Resources/UI/ui_game_frame.png`
