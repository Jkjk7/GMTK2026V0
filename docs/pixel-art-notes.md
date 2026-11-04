# Pixel art refresh

This branch changes presentation and cosmetic rendering only. The existing rules, prices, damage, enemy statistics, targeting, wave schedule, movement and rewards are preserved.

## Visuals

- 27 individually drawn 32×32 module icons share a cached point-filtered atlas. Shop, hand, draft, tooltip and placed modules use the same family; directional routing pieces retain their port indicators.
- Seven 48×48 enemy silhouettes distinguish swarm, grunt, tank, elite, boss, disassembler and shield caster. Burn, chill/freeze and sand markers follow the existing states. Only child artwork receives the stepped bob animation.
- Dark blue panels, brass edges, quieter locked-cell padlocks, larger card icons and actual enemy preview portraits replace prototype blocks and circles. The original hourglass frame and bilingual font remain.
- Hit, melt, death and shield effects use shared crisp pixel textures. Scene-owned pools retain at most 96 accents and 12 shield effects; overflow reuses the oldest cosmetic effect. The original shield effect's global random-number calls remain in the same order.
- Ball-count labels and the 60 countdown tick colors skip updates when their displayed values are unchanged. No frame-rate improvement percentage is claimed.

Sand-grain and coin animations still own their original resource-arrival callbacks and timing. They were deliberately left unchanged.

## Art source and reproduction

Module icons, panels and effects are code-native integer-pixel artwork in `PixelArtSprites`, `PixelUiSprites` and `PixelVfxSprites`; no external runtime package is required.

The enemy source sheet `Tools/Art/Source/pixel-enemies.png` was generated with the built-in imagegen tool. The prompt specified a transparent sheet with seven left-facing brass clockwork enemies in this order: beetle, red soldier, blue tank, violet elite knight, clocktower boss, purple spanner mechanist, gold/teal shield caster. It requested distinct silhouettes, dark outlines, limited pixel-art colors, no labels or scenery. The selected sheet is cropped and resampled to the game's 48px grid by the deterministic import script.

```sh
node Tools/Art/process-pixel-enemies.cjs
node --test Tools/Art/*.test.*
```

The script requires the same optional `sharp` dependency as the existing artwork tools. It writes the reviewable `Assets/Art/Pixel/enemy-atlas.png` and matching generated `Assets/Scripts/Core/PixelEnemyData.cs`. The embedded PNG is decoded once to a shared texture, avoiding per-enemy loading and platform-dependent file I/O. Regenerate both together; do not edit the base64 string manually.

## Validation performed

- Nine Node asset tests passed: existing processing behavior plus enemy atlas dimensions, distinct silhouettes, transparent margins, binary alpha and exact embedded PNG correspondence.
- All 121 runtime C# files compiled with Roslyn against the Unity assemblies shipped in `Play/TimeSands_Data/Managed` with zero errors. Existing deprecation/unreachable-code warnings remain.
- A disposable copy of the shipped **Unity 6000.4.9f1 Windows player** ran the new assembly and a temporary smoke harness. It passed `PixelIconRegressionChecks.Verify`, `PixelVfxRegressionChecks` and `CountdownArtRegressionChecks` with only the Editor exit/reporting API adapted to the player. Sprite/component operations used the real Unity engine.
- Runtime checks covered all module icons, disabled icons, placement tint, directional variants, bounded effect bursts, reuse reset, shield RNG sequence, all seven enemy types, status clearing and scene reload. English and Simplified Chinese game scenes rendered successfully and were inspected.

This does **not** establish a clean Unity Editor import, an Editor-built release or a Mac build: no Unity Editor was installed in the working environment. The repository's `Play/` and V0.3 archives remain unchanged. A separately supplied Windows review copy, if present, uses the original player with the updated runtime assembly and is for reviewing this branch.

## Editor verification before a release

Open with Unity 6000.4.9f1, let asset/package import finish, then run the documented regression entry points in the README. Check a busy late wave in the Profiler and rebuild the desired platform through Unity. Verify high enemy density, shop/hand interactions, status overlays and scene return with the final exported player.
