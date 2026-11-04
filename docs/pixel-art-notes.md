# Pixel art refresh

This branch changes presentation and cosmetic rendering only. The existing rules, prices, damage, enemy statistics, targeting, wave schedule, movement and rewards are preserved.

## Visuals

- 27 individually drawn 32×32 module icons share a cached point-filtered atlas. Shop, hand, draft, tooltip and placed modules use the same family; directional routing pieces retain their port indicators.
- Seven 48×48 enemy silhouettes distinguish swarm, grunt, tank, elite, boss, disassembler and shield caster. Burn, chill/freeze and sand markers follow the existing states. Their held poses distinguish scuttling, walking, heavy steps, hovering and boss sway, with visual hit flinch and caster charge accents. Only child artwork animates; roots, colliders and combat state stay unchanged. Freeze and reduced motion restore the rest pose while retaining status indicators.
- Dark blue panels, brass edges, quieter locked-cell padlocks, larger card icons and actual enemy preview portraits replace prototype blocks and circles. The original hourglass frame and bilingual font remain.
- Hit, melt, death and shield effects use shared crisp pixel textures. Scene-owned pools retain at most 96 accents, 12 shield effects and 24 particle bursts; overflow reuses the oldest cosmetic effect. Each particle burst uses at most 12 moving fragments, eight sampled fragment trails, two rings and a small impact cross (23 sprite renderers). Lifetimes are at most 0.78 seconds, and particle layers stay below enemy health bars. Fire is orange, frost cyan and arcane violet; deaths scatter falling chips and black holes draw particles inward. The new particles use private deterministic randomness and cached sprites, with no per-spawn materials. The original shield effect's global random-number calls remain in the same order.
- The furnace adds child-only nozzle recoil, an amber core and a brass rim. The core turns and pulses; its fill-dependent scale and color observe the existing emitter buffer. These accents do not trigger emission or change stored energy, timing or the gameplay transform.
- Ball-count labels and the 60 countdown tick colors skip updates when their displayed values are unchanged. No frame-rate improvement percentage is claimed.

Sand-grain and coin animations still own their original resource-arrival callbacks and timing. They were deliberately left unchanged.

## Optional Field Guide and preferences

The bilingual **Field Guide / 新手指南** opens on request from the main menu or in-game **Settings / 设置**. Six directly selectable lessons cover **Your first circuit / 第一条电路**, **Route & controls / 路径与操作**, **Modules & energy / 模块与能量**, **Ready for battle / 准备迎战**, **Elemental effects / 元素效果**, and **Build & upgrade / 搭建与升级**. **Basics / 基础**, **Controls / 操作** and **Build Tips / 搭建技巧** tabs give quick access to diagrams, controls and advice. Previous/next buttons and the left/right arrow keys browse the lessons; **Close [Esc] / 关闭 [Esc]** closes the guide.

The guide pauses the simulation, keeps keyboard focus inside the modal, and blocks board/shop input. Closing or destroying it restores the exact previous time scale, including an already-paused game. It consumes the closing frame's input so the same Escape/click cannot trigger an underlying action. There is no forced onboarding, practice simulation or resource grant.

Main-menu **Settings → Presentation / 设置 → 画面表现** and in-game settings expose saved **Effects: Reduced / 特效：精简**, **Effects: Balanced / 特效：均衡**, and **Effects: Cinematic / 特效：华丽** options. These select particle intensities of 0.25, 0.65 and 1 respectively; Cinematic is the default. **Motion: Full / 动态：完整** and **Motion: Reduced / 动态：精简** separately control the extra enemy/furnace motion and guide opening transition. Reduced motion preserves status information. These preferences do not change simulation speed or rules.

## Art source and reproduction

Module icons, panels and effects are code-native integer-pixel artwork in `PixelArtSprites`, `PixelUiSprites` and `PixelVfxSprites`; no external runtime package is required.

The enemy source sheet `Tools/Art/Source/pixel-enemies.png` was generated with the built-in imagegen tool. The prompt specified a transparent sheet with seven left-facing brass clockwork enemies in this order: beetle, red soldier, blue tank, violet elite knight, clocktower boss, purple spanner mechanist, gold/teal shield caster. It requested distinct silhouettes, dark outlines, limited pixel-art colors, no labels or scenery. The selected sheet is cropped and resampled to the game's 48px grid by the deterministic import script.

```sh
node Tools/Art/process-pixel-enemies.cjs
node --test Tools/Art/*.test.*
```

The script requires the same optional `sharp` dependency as the existing artwork tools. It writes the reviewable `Assets/Art/Pixel/enemy-atlas.png` and matching generated `Assets/Scripts/Core/PixelEnemyData.cs`. The embedded PNG is decoded once to a shared texture, avoiding per-enemy loading and platform-dependent file I/O. Regenerate both together; do not edit the base64 string manually.

## Phase 1 validation performed

- Nine Node asset tests passed: existing processing behavior plus enemy atlas dimensions, distinct silhouettes, transparent margins, binary alpha and exact embedded PNG correspondence.
- All 121 runtime C# files compiled with Roslyn against the Unity assemblies shipped in `Play/TimeSands_Data/Managed` with zero errors. Existing deprecation/unreachable-code warnings remain.
- A disposable copy of the shipped **Unity 6000.4.9f1 Windows player** ran the new assembly and a temporary smoke harness. It passed `PixelIconRegressionChecks.Verify`, `PixelVfxRegressionChecks` and `CountdownArtRegressionChecks` with only the Editor exit/reporting API adapted to the player. Sprite/component operations used the real Unity engine.
- Runtime checks covered all module icons, disabled icons, placement tint, directional variants, bounded effect bursts, reuse reset, shield RNG sequence, all seven enemy types, status clearing and scene reload. English and Simplified Chinese game scenes rendered successfully and were inspected.

This does **not** establish a clean Unity Editor import, an Editor-built release or a Mac build: no Unity Editor was installed in the working environment. The repository's `Play/` and V0.3 archives remain unchanged. A separately supplied Windows review copy, if present, uses the original player with the updated runtime assembly and is for reviewing this branch.

## Phase 2 regression entry points

The following checks cover the new guide, presentation preferences, particles and animations. Run from the repository root with Unity 6000.4.9f1, after closing any editor that uses this checkout:

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe'
$projectPath = (Get-Location).Path
New-Item -ItemType Directory -Force -Path Logs | Out-Null

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod PresentationSettingsRegressionChecks.Run -logFile Logs/presentation-settings-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod ParticleBurstRegressionChecks.Run -logFile Logs/particle-burst-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod AnimationRegressionChecks.Run -logFile Logs/animation-regression.log
$LASTEXITCODE

& $unityEditor -batchmode -nographics -projectPath $projectPath -executeMethod TutorialRegressionChecks.Run -logFile Logs/tutorial-regression.log
$LASTEXITCODE
```

| Entry point | Coverage |
| --- | --- |
| `PresentationSettingsRegressionChecks.Run` | Quality budgets and fallback, motion selection, unchanged saved preferences during previews, simulation speed and global RNG. |
| `ParticleBurstRegressionChecks.Run` | Burst cap/reuse/reset, fragment motion, trails, expiry with inactive renderers, reduced intensity, health-bar layering and global RNG. |
| `AnimationRegressionChecks.Run` | Root/collider/RNG isolation, bounded child reuse, archetype poses, hit flinch, freeze, reduced-motion statuses, caster charge observation and furnace recoil. |
| `TutorialRegressionChecks.Run` | Six lessons, navigation/focus, Chinese copy, modal reuse/input lock, pause restoration and cleanup. |

Inspect each command's exit code and log; zero plus its `PASS` marker indicates that group passed. These commands do not produce a release build or establish a frame-rate improvement. The checked-in `Play/` V0.3 player and archives remain unchanged.

## Phase 2 validation performed

- All 125 runtime C# files compiled against the shipped Unity assemblies with zero errors; 57 existing/API deprecation warnings remain. Nine Node asset checks passed.
- A disposable Windows player copy passed all four new regression groups and the existing icon, pixel VFX and countdown-art groups. Actual Unity components and rendering were used; only the Editor menu/exit reporting APIs were adapted for this temporary harness.
- Scene checks confirmed that closing a guide opened from settings keeps the run paused, closing settings resumes it, and unloading a scene with both modals open leaves the next scene unpaused. A new combat pool can spawn effects after scene reload.
- Reviewed English and Chinese guide pages, both main-menu settings layouts and in-game settings. Horizontal arrows page the guide without moving button focus; the final navigation change passed a separate runtime rerun.
- Captured a scripted visual demonstration of seven enemy silhouettes and fire, frost, arcane and implosion effects. The demo places enemies/effects for comparison; it is not a recording of a naturally occurring wave. The review player does not include the demonstration harness.
- Independent source review found no remaining blocking issue in gameplay isolation, RNG use, animation roots, pooling or modal lifecycle.

The Windows review copy reuses the repository's player with the updated runtime assembly. A clean Unity Editor import/export, a Mac build, full playthrough and late-wave performance profiling remain unverified; no specific FPS gain is claimed.

## Editor verification before a release

Open with Unity 6000.4.9f1, let asset/package import finish, then run the documented regression entry points in the README. Check a busy late wave in the Profiler and rebuild the desired platform through Unity. Verify high enemy density, shop/hand interactions, status overlays and scene return with the final exported player.
