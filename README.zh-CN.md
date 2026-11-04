# TimeSands

[English](README.md)

> 在最后一粒沙落下前，把时间导向武器。

**TimeSands** 是 **强者🥛 团队** 为 GMTK Game Jam 2026 主题 **Count Down** 制作的线路构筑塔防游戏。时间既是生命值，也是驱动防御设施的资源。

仓库包含 Unity 工程及预先构建的 V0.3 试玩版本。试玩包是固定快照；若要体验代码或美术的最新修改，请重新构建工程。

## 游戏玩法

1. 从商店购买模块，放到棋盘上，将发射器产生的能量球导向武器。鼠标悬停可查看模块说明。
2. 利用准备阶段布置线路、合成兼容模块、扩展棋盘。准备阶段没有时间限制；完成后按 **空格** 或 **准备** 按钮。
3. 战斗中，发射器从沙漏抽沙产生能量球。敌人从右侧来袭，漏怪会额外扣沙；击杀沙 buff 敌人和清波奖励可补充沙量。
4. 随进度解锁模块，并选择发射器强化、祝福和束缚。守住 **25 个常规波次与第 26 波最终 Boss**，且仍有余沙，即可获胜。沙量耗尽或 Boss 突破防线会结束本局。

当前源码目录包含 **27 种类型，其中 3 种为一次性道具**，涵盖攻击、路径、经济、附魔与辅助；每局通过解锁使用其中一部分。系统包括购买、放置、路径模块旋转、移动、合成、升级、分解，以及灼烧、寒冷、融化等互动。

当前源码采用 **27 种独立像素模块图标与 7 种敌人轮廓**，共享清晰的点采样精灵，并为命中、融化、死亡及护盾破碎特效提供有容量上限的对象池。火焰、冰霜与奥术命中会迸出移动的像素碎片、拖尾、分层冲击环和局部闪光；死亡会碎解成像素片，黑洞展开时粒子向内收拢。敌人姿态与熔炉仅对美术子物体添加动态，玩法对象的变换保持不变。这些视觉改动不改变玩法规则，也不保证特定帧率。仓库附带的 `Play/` V0.3 试玩尚未包含这些改动。

界面新增悬停、按压与交易反馈。买不起的商品会红闪、轻晃并显示 **金币不足**；手牌没有空位时则显示 **手牌已满**。卡牌进入手牌时，包括从棋盘拆回的模块，会亮起金色星光并发出轻柔的“叮”声；商店刷新、成功合成与分解出售也有反馈。奖励选择以错峰弹出的卡牌、彩色边框与像素魔法阵呈现，设置、确认与结算窗口有短暂的打开过渡。操作仍立即生效，价格、奖励与资源到账时机保持不变。交易音效复用缓存的轻声音调，已配置的自定义音频仍优先使用。

左上角沙漏采用清晰的像素铜框、青色玻璃和随实际余量变化的金色沙堆。细小流沙在暂停或简化动态时停止，精确计时与低余量警告始终保留。

首次启动默认英文。在主菜单 **Settings → Language → 中文 / English** 切换语言，选择会保存。

## 操作

| 输入 | 功能 |
| --- | --- |
| 鼠标 | 购买、选择卡牌；从商店或手牌拖放；拖动棋盘模块；合成兼容卡牌/模块；在分解区出售换金币。出现确认提示时按提示操作。 |
| 悬停 | 查看模块或地块详情。 |
| **R** | 旋转选中或携带的路径模块，也可旋转正从棋盘拖起的路径模块。 |
| **空格** / **准备** | 结束准备，开始波次倒计时。 |
| **F** | 刷新商店；没有免费刷新次数时消耗金币。 |
| 对准棋盘模块 **右键** / **X** | 拆回手牌。准备阶段免费立即执行；战斗中可能收费并要求确认。手牌必须有空位。 |
| **Esc** | 新手指南打开时优先关闭指南；否则打开/关闭局内设置，暂停、切换显示模式、继续游戏，或确认后退出。 |
| 新手指南中的 **← / →** | 切换六节课程。 |

波次倒计时及选择草稿期间，放置与商店操作会暂时禁用。锁定、地块和合成规则也会限制操作。拆除会回收模块到手牌，分解则出售换金币。

## 新手指南与画面表现

可从主菜单 **新手指南**，或局内 **设置 → 新手指南** 主动打开指南。指南跟随当前中英文选择，包含六课：**第一条电路**、**路径与操作**、**模块与能量**、**准备迎战**、**元素效果**、**搭建与升级**。**基础**、**操作**、**搭建技巧** 页签可快速定位线路示意、按键说明与搭建建议。既可直接点击课程，也可使用 **上一页** / **下一课  >** 或左右方向键翻阅。

指南打开时暂停本局，并阻止商店与棋盘操作。点击 **关闭 [Esc]**、最后一课的 **关闭指南** 或按 Esc，会恢复打开前的暂停状态；从已暂停的设置进入指南，关闭后仍保持暂停。指南无需强制完成，也不会启动练习局或发放资源。

主菜单 **设置 → 画面表现** 与局内 **设置** 提供会保存的画面偏好。点击 **特效：华丽**、**特效：精简** 或 **特效：均衡**，可循环切换三档粒子数量与强度，包括奖励装饰；默认 **华丽**。**动态：完整** / **动态：精简** 控制额外的敌人、熔炉动态、界面晃动与缩放、奖励卡牌运动及弹窗和指南的打开过渡。精简动态仍保留状态标记、失败提示文字、颜色闪光与收卡星光。粒子强度和动态是独立设置，均不改变游戏速度、伤害、奖励或索敌。

## 运行试玩包

运行预构建试玩包无需安装 Unity。

- **Windows：**运行 `Play/TimeSands.exe`；也可解压 `TimeSands V0.3 试玩版.zip` 后运行其中的可执行文件。请保留可执行文件、`TimeSands_Data`、`UnityPlayer.dll` 及其余配套文件的相对位置。
- **macOS：**解压 `Play_mac.app.tar` 或 `TimeSandV0.3 Mac.7z`，打开解压后的 `.app`。`.7z` 需使用支持该格式的解压工具。如解压时丢失可执行权限，可针对实际 app 路径执行：

  ```sh
  chmod +x "/path/to/TimeSands.app/Contents/MacOS/"*
  ```

  请替换成解压后的实际名称。若 macOS 拦截应用，在 **系统设置 → 隐私与安全性** 查看提示，仅在信任下载来源时允许打开。

## 打开 Unity 工程

环境要求：

- **Unity Editor 6000.4.9f1**，版本记录在 `ProjectSettings/ProjectVersion.txt`。通过 Unity Hub 安装，并按目标平台安装对应构建支持模块。
- 源码克隆需要 **Git 与 Git LFS**。图片、字体、压缩包、可执行文件和动态库等使用 LFS。
- 首次恢复依赖需要联网。`Packages/manifest.json` 包含基于 Git 的 Unity MCP 依赖，因此 Unity 也需要能调用 Git。运行已打包试玩无需该编辑器集成。

```sh
git lfs install
git clone https://github.com/Jkjk7/GMTK2026V0.git
cd GMTK2026V0
git lfs pull
```

已有克隆可在仓库根目录执行 `git lfs install` 和 `git lfs pull`。GitHub 源码 ZIP 不一定包含 LFS 实际内容；遇到缺失素材时，请使用完整的 LFS 克隆。

在 Unity Hub 选择 **从磁盘添加项目**，选中包含 `Assets`、`Packages` 和 `ProjectSettings` 的工程根目录。使用指定版本打开，等待依赖恢复与素材导入完成，再打开 **`Assets/Scenes/MainMenu.unity`** 并点击 Play。在设置中选择语言后开始游戏。

`Game_EN.unity`、`Game_ZH.unity` 是中英文游戏场景。工程保留了 `SampleScene.unity`，但未将它加入启用的构建场景。游戏对象与大量 UI 由代码在运行时创建，因此进入 Play 前场景看起来空，并不一定是导入失败。

## 从源码构建

1. 打开 **File → Build Profiles**，选择 Windows 或 macOS；如有提示，安装对应 Unity 平台模块。
2. 确认启用场景依次为 `MainMenu`、`Game_EN`、`Game_ZH`，顺序记录在 `ProjectSettings/EditorBuildSettings.asset`。
3. 输出到独立目录，例如 `Builds/Windows` 或 `Builds/macOS`，以保留仓库附带试玩。
4. 启动新构建，检查主菜单、两种语言、准备到战斗的切换及设置菜单。分发时携带完整输出文件夹或 app。

仓库未提供自动构建玩家程序的入口。下列回归命令只检查部分行为与资源，不会构建程序或完整通关。

## 现有回归检查

执行批处理检查前，请关闭正在打开同一工程的编辑器。在**仓库根目录的 PowerShell** 执行，按实际安装位置调整编辑器路径：

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
$projectPath = (Get-Location).Path
New-Item -ItemType Directory -Force -Path Logs | Out-Null

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod GmtkBugfixRegressionChecks.Run -logFile Logs/bugfix-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod LocalizationRegressionChecks.Run -logFile Logs/localization-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod CountdownArtRegressionChecks.Run -logFile Logs/countdown-art-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod PixelIconRegressionChecks.Run -logFile Logs/pixel-icon-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod PixelVfxRegressionChecks.Run -logFile Logs/pixel-vfx-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod PresentationSettingsRegressionChecks.Run -logFile Logs/presentation-settings-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod ParticleBurstRegressionChecks.Run -logFile Logs/particle-burst-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod AnimationRegressionChecks.Run -logFile Logs/animation-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod TutorialRegressionChecks.Run -logFile Logs/tutorial-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod UiFeedbackRegressionChecks.Run -logFile Logs/ui-feedback-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod TransactionFeedbackRegressionChecks.Run -logFile Logs/transaction-feedback-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod RewardFeedbackRegressionChecks.Run -logFile Logs/reward-feedback-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod HourglassVisualRegressionChecks.Run -logFile Logs/hourglass-visual-regression.log
$LASTEXITCODE
```

各入口会自行退出 Unity：**0** 表示该组检查通过，**1** 表示失败。还应查看日志中的 `PASS` 标记及编译/导入错误。macOS 可使用已安装 Unity Editor app 内的可执行文件，参数相同。

| 入口 | 检查范围 |
| --- | --- |
| `GmtkBugfixRegressionChecks.Run` | 投射物重新索敌、保留灼烧/寒冷效果、无目标处理、带种子的附魔布局与合成。 |
| `LocalizationRegressionChecks.Run` | 默认英文、中文选择、无效语言回退；该检查会重置保存的语言偏好。 |
| `CountdownArtRegressionChecks.Run` | 倒计时刻度与警告、美术或回退资源、模块图标与外观、敌人轮廓与状态、特效寿命上限及部分沙量常量。 |
| `PixelIconRegressionChecks.Run` | 27 种独立共享像素图标、采样与轴心、棋盘外观、路径朝向、放置预览颜色及原型回退。 |
| `PixelVfxRegressionChecks.Run` | 特效复用时重置状态、特效与护盾池容量上限、保留全局随机数序列及像素特效边缘。 |
| `PresentationSettingsRegressionChecks.Run` | 三档粒子预算递增、无效值回退、动态选择、不落盘的偏好预览，以及游戏速度与全局随机数不变。 |
| `ParticleBurstRegressionChecks.Run` | 最多 24 组粒子爆发、复用与重置、碎片与拖尾运动、到期停用全部渲染器、降低强度、血条层级及全局随机数不变。 |
| `AnimationRegressionChecks.Run` | 敌人/熔炉仅美术子物体运动，根变换/碰撞体/随机数不变，冻结与静止姿态、精简动态下的状态可见性、受击反馈、不同敌人姿态及观察施法/熔炉状态的表现。 |
| `TutorialRegressionChecks.Run` | 恢复原暂停状态（含原本已暂停）、模态复用与输入阻挡、六课可选、键盘导航、中文内容与关闭清理。 |
| `UiFeedbackRegressionChecks.Run` | 暂停时的反馈、随机数不变、装饰数量有界且不拦截输入、到期/停用/重开后的精确姿态恢复，以及精简动态下的静止提示。 |
| `TransactionFeedbackRegressionChecks.Run` | 购买费用/回调/随机数、金币不足与满手牌拒绝、扣费回调后无法入手时的静默退款、即时收卡且悬停/选中不重播、直接购入棋盘及刷新/对局状态限制。 |
| `RewardFeedbackRegressionChecks.Run` | 选择/确认/结算回调即时且只执行一次、暂停时的展示、装饰数量上限、精简动态姿态、资源/随机数不变，以及弹窗输入与变换清理。 |
| `HourglassVisualRegressionChecks.Run` | 点采样边框与清晰沙粒、精确计时文字、沙量填充/耗尽/溢出、精简动态警告、复用上限、计时区域留空及随机数不变。 |

可选 PNG 工具需要 Node.js 与 `sharp`，不影响 Unity 游戏本身：

```sh
npm install --prefix Tools/Art --no-save --package-lock=false sharp
node --test Tools/Art/*.test.*
```

不要将本地安装的 `node_modules` 提交到仓库。如需按现有清单重新生成美术输出：

```sh
node Tools/Art/process-countdown-assets.mjs --manifest Tools/Art/countdown-assets.json
```

该清单会写入 `Assets/Art/Generated/Countdown`；运行时副本位于 `Assets/Resources/Countdown`。修改这些美术后应检查并同步需要的运行时资源。

如需从 `Tools/Art/Source/pixel-enemies.png` 重新生成像素敌人图集：

```sh
node Tools/Art/process-pixel-enemies.cjs
```

这会写入 `Assets/Art/Pixel/enemy-atlas.png` 和内嵌运行时数据 `Assets/Scripts/Core/PixelEnemyData.cs`，两个输出应同步保留。模块图标由 `PixelArtSprites` 代码绘制。以上说明的是可用检查方法，不代表它们已在你的机器上通过。

## 仓库结构

```text
Assets/Scenes/                   主菜单、中英文游戏场景、旧示例场景
Assets/Scripts/                  棋盘、能量球、模块、战斗、经济、UI 与核心服务
Assets/Editor/                   批处理回归入口
Assets/Art/Generated/Countdown/  美术源图与处理输出
Assets/Art/Pixel/                处理后的像素敌人图集
Assets/Resources/Countdown/     运行时加载的倒计时美术
Assets/Resources/UI/            运行时 UI 图片
Packages/                       包清单与依赖锁文件
ProjectSettings/                编辑器版本、玩家设置与构建场景
Play/                           预构建 Windows V0.3 试玩
Tools/Art/                      PNG 源图、处理脚本与 Node 测试
docs/                           中文策划、数值说明与工作簿生成脚本
```

参见[游戏策划案](docs/游戏策划案.md)、[数值平衡表](docs/数值平衡表.md)和[数值分析工作簿](docs/数值分析_模块怪物波次.xlsx)。部分说明保留了旧版本内容，当前数值以实际代码为准：模块可用性见 `ModuleCatalog` / `RunModulePool`，波次见 `WaveSpawnBudget`，经济与沙量见对应运行时服务。

## 故障排查

- **素材缺失、图片无效或程序无法启动：**执行 `git lfs pull`。内容以 `version https://git-lfs.github.com/spec/v1` 开头的小文本文件只是 LFS 指针，并非实际素材。Windows 试玩应恢复完整解压目录。
- **Unity 无法恢复依赖：**检查 Git、网络和 Package Manager 错误，特别是 `Packages/manifest.json` 中的 Git 地址。依赖恢复及编译完成后再进入 Play。
- **Game 视图空白或入口不对：**打开 `MainMenu.unity`，进入 Play 并开始游戏；构建时检查三个启用场景及顺序。
- **按键或拖放没有反应：**编辑器中先点击 Game 视图；完成草稿选择、倒计时或确认，检查金币、手牌容量和放置规则，并关闭设置遮罩。
- **UI 显示不全：**在设置中切换桌面分辨率全屏与固定 **1440 × 900** 窗口模式。
- **批处理提前结束或没有 `PASS`：**确认编辑器路径与版本、关闭同一工程的交互式编辑器，并查看指定日志。Unity 成功启动不等于检查通过。
- **提示 `Cannot find module 'sharp'`：**按前文安装可选依赖；打开和运行 Unity 工程无需它。

## 生成式 AI 披露

项目部分视觉开发、代码、文档与投稿文案使用了生成式 AI 工具辅助。最终内容由团队选择和集成。

## 许可与使用

仓库未包含独立的项目许可证文件。本项目用于 Game Jam 展示与学习交流；团队原有使用限制为：未经许可，不得将项目素材或代码用于商业用途。源码公开不等于授予无限制复用许可。第三方依赖仍适用各自的许可条款。
