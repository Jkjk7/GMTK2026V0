# Task 9 Report — Procedural Countdown MVP

## Implemented

- Added shared countdown rules with 60 ticks, clamped ratios, and a 20,000 ms warning boundary.
- Added a giant world-space procedural countdown ring using `PrototypeSprites.Square`, palette colors, sorting order `-20`, clamped lit ticks, and alternating orange/red warning colors.
- Loaded `GameSkin` before world construction and created the ring alongside the runtime `SandClock`.
- Added procedural clock faces, hands, hubs, and alternating gear details for every `ModuleType`, using only square/circle fallbacks and the countdown palette.
- Applied module details to newly created/placed modules, type-changing fused results, same-type fused modules (details persist), and all placement ghost variants.
- Kept the precise countdown/hourglass and made warning text pulse using the shared warning rule.
- Added an editor batch entry point covering exactly 60 ticks, ratio clamping, the 20,000 ms boundary, all enum module types, and unchanged sand-clock gameplay constants.

The follow-up review tranche adds procedural enemy/status and bounded combat VFX without generated-asset dependencies.

## TDD Evidence

- RED: Created `Assets/Editor/CountdownArtRegressionChecks.cs` first, referencing the not-yet-created `CountdownVisualRules` and `ModuleSkinApplicator`. Command:
  `/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath ... -executeMethod CountdownArtRegressionChecks.Run`
  Unity spent the timebox rebuilding the absent Library and was interrupted before script compilation, so there is no compiler-failure line to claim.
- GREEN: `/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath ... -executeMethod CountdownArtRegressionChecks.Run -logFile /tmp/gmtk-countdown-art/final-art.log`
  Exit 0; `[Countdown Art Regression] PASS`.

## Verification

- Existing Unity regression:
  `/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath ... -executeMethod GmtkBugfixRegressionChecks.Run`
  Exit 0; `[GMTK Regression] PASS`. The existing edit-mode `Destroy` warnings remain.
- `git diff --check`: clean.

## Files Changed

- `Assets/Editor/CountdownArtRegressionChecks.cs`
- `Assets/Scripts/UI/CountdownVisualRules.cs`
- `Assets/Scripts/UI/CountdownRingView.cs`
- `Assets/Scripts/Modules/ModuleSkinApplicator.cs`
- `Assets/Scripts/Game/GameBootstrap.cs`
- `Assets/Scripts/UI/SandClockPanel.cs`
- `Assets/Scripts/UI/PlacementController.cs`
- `Assets/Scripts/Combat/EnemyVisualController.cs`
- `Assets/Scripts/Combat/CombatVfxService.cs`
- `Assets/Scripts/Combat/TransientSpriteVfx.cs`
- `Assets/Scripts/Combat/Enemy.cs`
- `Assets/Scripts/Combat/CombatDamage.cs`
- Corresponding Unity `.meta` files.

## Self-review / Concerns

- The runtime slice is procedural-only; generated PNGs are deliberately not coupled to it.
- Unity reserialized package/project files during initial import. They, generated art, `ProjectSettings/PhysicsCoreProjectSettings2D.asset`, and the pre-existing `MinerModule.cs` compatibility edit are excluded from this commit.

## Review Fix Report

- Added an enemy clockwork core and rotating hand whose world size follows the existing variant scale.
- Added child burn ember/flame, chill rim, and sand orbit visuals. Their enabled state continuously derives from the enemy's existing status booleans, so they follow the enemy and clear without changing status timing.
- Added hit, death, and white-hot Melt accents with clamped lifetimes of at most one second; runtime instances self-destroy.
- RED: after adding the new assertions first, `CountdownArtRegressionChecks.Run` failed compilation with missing `EnemyVisualController`, `TransientSpriteVfx`, and `CombatVfxService`, as expected.
- GREEN: `CountdownArtRegressionChecks.Run` exited 0 with `[Countdown Art Regression] PASS`.
- Full regression: `GmtkBugfixRegressionChecks.Run` exited 0 with `[GMTK Regression] PASS`; only its pre-existing edit-mode `Destroy` warnings remain.
