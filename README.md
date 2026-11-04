# TimeSands

[简体中文](README.zh-CN.md)

> Route time into weapons before the final grain falls.

**TimeSands** is a circuit-building tower-defense game created for GMTK Game Jam 2026 and its theme, **Count Down**, by **Team 强者🥛**. Time is both your health and the resource that powers your defenses.

This repository contains the Unity project and prebuilt V0.3 demos. The demos are snapshots; rebuild the project to play changes made to the source or artwork.

## How to play

1. Buy modules from the shop, place them on the board, and route energy balls from the emitter into your weapons. Hover over modules for descriptions.
2. Arrange your circuit, merge compatible modules, and expand the board during preparation. Preparation has no time limit; press **Space** or **Ready** when finished.
3. During combat, the emitter draws sand from the hourglass to produce energy balls. Enemies approach from the right; breaches cost additional sand. Sand-buffed enemies and wave-clear rewards can replenish it.
4. Draft modules and choose emitter upgrades, blessings, and bindings as the run progresses. Survive **25 regular waves and the final Boss on wave 26** with sand remaining. Running out of sand, or letting the Boss breach, ends the run.

The current source includes **27 catalog types, including 3 consumable items**, across attack, routing, economy, enchantment, and support roles. Each run unlocks a subset. Systems include buying, placing, rotating path modules, moving, merging, upgrading, scrapping, and fire/chill/Melt interactions.

The source now uses **27 distinct pixel module icons and 7 enemy silhouettes**, shared sprites with crisp point filtering, and bounded pools for hit, Melt, death, and shield-shatter accents. These visual changes do not change gameplay rules or guarantee a particular frame rate. The included `Play/` V0.3 demo has not been rebuilt with them.

The interface defaults to English on a fresh launch. Choose **Settings → Language → English / 中文** from the main menu; the choice is saved.

## Controls

| Input | Action |
| --- | --- |
| Mouse | Buy/select cards; drag from the shop or hand to place; drag board modules to move; merge compatible cards/modules; use the scrap area to sell for gold. Follow confirmation prompts where shown. |
| Hover | Show module or cell details. |
| **R** | Rotate a selected/carried path module, including one being dragged from the board. |
| **Space** / **Ready** | End preparation and start the wave countdown. |
| **F** | Refresh the shop; costs gold unless a free refresh is available. |
| **Right click** / **X** over a board module | Dismantle it back into the hand. Free and immediate during preparation; combat may require payment and confirmation. A free hand slot is required. |
| **Esc** | Open/close in-game settings to pause, change display mode, resume, or quit with confirmation. |

Placement and shop actions are temporarily unavailable during the wave countdown and draft selection. Locks, placement, and merge rules can also restrict an action. Dismantling returns a module to your hand; scrapping sells it.

## Play a prebuilt demo

No Unity installation is needed for a prebuilt demo.

- **Windows:** launch `Play/TimeSands.exe`, or extract `TimeSands V0.3 试玩版.zip` and launch its executable. Keep the executable, `TimeSands_Data`, `UnityPlayer.dll`, and the other accompanying files together.
- **macOS:** extract `Play_mac.app.tar` or `TimeSandV0.3 Mac.7z`, then open the extracted `.app`. The `.7z` file requires a compatible archive tool. If executable permissions were lost during extraction, restore them for the actual app path:

  ```sh
  chmod +x "/path/to/TimeSands.app/Contents/MacOS/"*
  ```

  Use the extracted app's actual name. If macOS blocks the app, review the warning in **System Settings → Privacy & Security** and allow it only if you trust this download.

## Open the Unity project

Requirements:

- **Unity Editor 6000.4.9f1**, recorded in `ProjectSettings/ProjectVersion.txt`. Install through Unity Hub with build support for your target platform.
- **Git and Git LFS** for a source checkout. Art, fonts, archives, executables, and libraries use LFS.
- Internet access for initial package restoration. `Packages/manifest.json` includes a Git-based Unity MCP dependency, so Git must also be available to Unity. This editor integration is not required to play the packaged demo.

```sh
git lfs install
git clone https://github.com/Jkjk7/GMTK2026V0.git
cd GMTK2026V0
git lfs pull
```

For an existing clone, run `git lfs install` and `git lfs pull` from its root. A GitHub source ZIP may not contain actual LFS payloads; use a complete LFS checkout if assets are missing.

In Unity Hub, choose **Add project from disk** and select the folder containing `Assets`, `Packages`, and `ProjectSettings`. Open it with the recorded editor version, wait for package restoration/import to finish, then open **`Assets/Scenes/MainMenu.unity`** and press Play. Choose a language in Settings and start a run.

`Game_EN.unity` and `Game_ZH.unity` are the gameplay scenes. `SampleScene.unity` is retained but is not enabled in the build scene list. Gameplay objects and much of the UI are assembled at runtime, so an empty-looking scene before Play is not necessarily an import failure.

## Build from source

1. Open **File → Build Profiles**, select Windows or macOS, and install the matching Unity platform module if prompted.
2. Ensure the enabled scenes are ordered `MainMenu`, `Game_EN`, `Game_ZH`, as stored in `ProjectSettings/EditorBuildSettings.asset`.
3. Build into a separate folder such as `Builds/Windows` or `Builds/macOS` to preserve the checked-in demos.
4. Launch the generated player and check the main menu, both languages, a preparation/combat transition, and the settings menu. Distribute the entire generated player folder/app.

There is no checked-in automated player-build entry point. The regression commands below check selected behavior and resources; they do not build or play through a full run.

## Existing regression checks

Close any editor using the same project before starting a batch check. Run in **PowerShell from the repository root**; adjust the editor path for your installation:

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
```

Each entry point exits Unity itself: **0** means its checks passed; **1** means failure. Inspect the log for its `PASS` marker and any compile/import errors. On macOS, use the executable inside the installed Unity Editor app with the same arguments.

| Entry point | Coverage |
| --- | --- |
| `GmtkBugfixRegressionChecks.Run` | Projectile retargeting, retained burn/chill effects, safe handling of missing targets, and seeded enchantment layout/merging. |
| `LocalizationRegressionChecks.Run` | English fallback, Chinese selection, and invalid-language fallback. This check resets the saved language preference. |
| `CountdownArtRegressionChecks.Run` | Countdown ticks/warnings, art or fallback resources, module icons/skins, enemy silhouettes/status visuals, bounded effect lifetimes, and selected sand constants. |
| `PixelIconRegressionChecks.Run` | 27 distinct shared pixel icons, filtering/pivots, placed skins, path orientation, ghost validity tints, and prototype fallback. |
| `PixelVfxRegressionChecks.Run` | Fresh state on effect reuse, bounded accent/shield pools, preserved global random sequence, and crisp VFX texture edges. |

The optional PNG tools require Node.js with `sharp` and are independent of Unity gameplay:

```sh
npm install --prefix Tools/Art --no-save --package-lock=false sharp
node --test Tools/Art/*.test.*
```

Keep locally installed `node_modules` out of commits. To regenerate artwork outputs from the existing manifest:

```sh
node Tools/Art/process-countdown-assets.mjs --manifest Tools/Art/countdown-assets.json
```

This manifest writes under `Assets/Art/Generated/Countdown`; runtime copies live under `Assets/Resources/Countdown`. Review and synchronize the intended runtime assets when changing that artwork.

To regenerate the pixel enemy atlas from `Tools/Art/Source/pixel-enemies.png`:

```sh
node Tools/Art/process-pixel-enemies.cjs
```

This writes `Assets/Art/Pixel/enemy-atlas.png` and the embedded runtime data in `Assets/Scripts/Core/PixelEnemyData.cs`; keep both outputs together. Module icons are drawn in code by `PixelArtSprites`. These are instructions for available checks, not a claim that they have passed on your machine.

## Repository layout

```text
Assets/Scenes/                   Main menu, English/Chinese gameplay, legacy sample scene
Assets/Scripts/                  Board, balls, modules, combat, economy, UI, core services
Assets/Editor/                   Batch-mode regression entry points
Assets/Art/Generated/Countdown/  Artwork sources and processed outputs
Assets/Art/Pixel/                Processed pixel enemy atlas
Assets/Resources/Countdown/     Runtime-loadable countdown art
Assets/Resources/UI/            Runtime UI artwork
Packages/                       Package manifest and dependency lock
ProjectSettings/                Editor version, player settings, enabled scenes
Play/                           Prebuilt Windows V0.3 player
Tools/Art/                      PNG sources, processing pipelines, and Node tests
docs/                           Chinese design/balance notes and workbook generator
```

See [design notes](docs/游戏策划案.md), [balance notes](docs/数值平衡表.md), and [balance workbook](docs/数值分析_模块怪物波次.xlsx). Some notes describe earlier revisions; gameplay code is authoritative for current values. Module availability comes from `ModuleCatalog`/`RunModulePool`, waves from `WaveSpawnBudget`, and economy/sand values from their runtime services.

## Troubleshooting

- **Missing art, invalid images, or an executable that will not start:** run `git lfs pull`. A tiny text file beginning `version https://git-lfs.github.com/spec/v1` is an LFS pointer, not the actual asset. For the Windows demo, restore the entire extracted folder.
- **Unity cannot restore packages:** check Git availability, network access, and Package Manager errors, especially the Git URL in `Packages/manifest.json`. Let restoration and compilation finish before Play.
- **Blank Game view or wrong entry scene:** open `MainMenu.unity`, enter Play, and start a game. For a build, verify all three enabled scenes and their order.
- **An input does nothing:** click the Game view in the editor, finish any draft/countdown or confirmation, check gold/hand capacity and placement rules, and close the settings overlay.
- **UI does not fit:** use Settings to switch between desktop-resolution fullscreen and fixed **1440 × 900** windowed mode.
- **A batch check exits early or has no `PASS`:** confirm the editor path/version, close the interactive editor for this checkout, and read the log. A successful Unity launch is not evidence that a check passed.
- **`Cannot find module 'sharp'`:** install the optional dependency above; it is not needed to open or play the Unity project.

## Generative AI disclosure

Generative AI tools assisted with portions of the visual development, code, documentation, and submission text. Final selection and integration are handled by the team.

## License and use

No standalone project license file is included. This repository is published for game-jam demonstration and learning; the team's stated restriction is that its assets and code must not be reused commercially without permission. Public source availability does not grant an unrestricted reuse license. Third-party packages retain their own license terms.
