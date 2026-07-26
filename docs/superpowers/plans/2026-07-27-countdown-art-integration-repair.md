# Countdown Art Integration Repair Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the complete countdown ring, formal hourglass, all module icons, placed-module skins, board cells, and sidebar panels visibly coherent in the running Game view.

**Architecture:** `CountdownArtResources` becomes the single runtime resource resolver for environment, UI, and module sprites. UI cards and placed modules consume the same module lookup; `CountdownRingView` owns the full ornament plus sixty state ticks; `SandClockPanel` owns only time-dependent hourglass fills and feedback. Existing gameplay classes retain their rules and values.

**Tech Stack:** Unity 6.0 C#, Unity UI, `SpriteRenderer`, Resources API, Node.js test runner, Sharp image processing, built-in ImageGen, Unity batch-mode editor checks.

## Global Constraints

- Work directly on local `main`; do not push to GitHub.
- Preserve gameplay behavior, module footprints, collisions, targeting, damage, economy, enemy spawning, and localization.
- Keep exactly sixty countdown ticks.
- Do not add colliders or UI raycast targets to decorative art.
- All runtime art lookups must have safe prototype fallbacks.
- Do not stage the user's existing Package Manager, ProjectSettings, `.codex`, or unrelated documentation changes.
- Restart Play mode before final visual inspection; asset-file existence alone does not count as visual verification.

## File Map

- `tools/Art/process-countdown-assets.mjs`: chroma-key alpha and despill processing.
- `tools/Art/process-countdown-assets.test.mjs`: deterministic image-processor regressions.
- `Assets/Art/Generated/Countdown/Sources/*.png`: generated source sheets.
- `Assets/Art/Generated/Countdown/Modules/*.png`: processed module sprites.
- `Assets/Resources/Countdown/**`: runtime copies of formal art.
- `Assets/Scripts/Core/CountdownArtResources.cs`: stable resource paths and sprite lookup.
- `Assets/Scripts/Core/GameSkin.cs`: delegates all module icons to the resource lookup.
- `Assets/Scripts/UI/ModuleIconVisuals.cs`: shared UI image application for cards, tooltips, and drag previews.
- `Assets/Scripts/UI/ModuleSlotView.cs`: shop/hand icon and subtle slot state rendering.
- `Assets/Scripts/UI/HandSlot.cs`: hand drag icon.
- `Assets/Scripts/UI/ShopSlot.cs`: shop drag icon.
- `Assets/Scripts/UI/ModuleTooltipView.cs`: tooltip module icon.
- `Assets/Scripts/Modules/ModuleSkinApplicator.cs`: persistent formal placed-module skin.
- `Assets/Scripts/UI/CountdownRingView.cs`: ornament and sixty dynamic ticks.
- `Assets/Scripts/UI/SandClockPanel.cs`: formal frame plus dynamic sand chambers.
- `Assets/Scripts/Game/GameBootstrap.cs`: visual hierarchy, hourglass construction, and neutral UI panels.
- `Assets/Scripts/Board/GridBoard.cs`: subtle board state overlays.
- `Assets/Editor/CountdownArtRegressionChecks.cs`: runtime resource and renderer regressions.

---

### Task 1: Remove Chroma-Key Fringe From Generated Art

**Files:**
- Modify: `tools/Art/process-countdown-assets.mjs`
- Modify: `tools/Art/process-countdown-assets.test.mjs`
- Regenerate: `Assets/Art/Generated/Countdown/UI/*.png`
- Regenerate: `Assets/Art/Generated/Countdown/Environment/*.png`

**Interfaces:**
- Consumes: `keyedAlpha(r, g, b, originalAlpha): number`
- Produces: `keyedPixel(r, g, b, originalAlpha): { r, g, b, alpha }`

- [ ] **Step 1: Write the failing despill test**

Add a test that passes a near-green antialiased pixel and requires both partial alpha and a green
channel no brighter than the preserved red/blue edge luminance:

```js
import { keyedPixel } from "./process-countdown-assets.mjs";

test("near-key antialias pixels are despilled as alpha fades", () => {
  const pixel = keyedPixel(45, 220, 40, 255);
  assert.ok(pixel.alpha > 0 && pixel.alpha < 255);
  assert.ok(pixel.g <= Math.max(pixel.r, pixel.b) + 24);
});
```

- [ ] **Step 2: Run the Node test and verify RED**

Run:

```bash
NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules" \
/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node \
--test tools/Art/process-countdown-assets.test.mjs
```

Expected: FAIL because `keyedPixel` is not exported.

- [ ] **Step 3: Implement keyed alpha plus despill**

Return transparent RGB for fully keyed pixels. For partial alpha, clamp green toward the greater
of red and blue plus a maximum 24-point edge allowance. Update `processTransparentBuffer` to
write all four returned channels.

```js
export function keyedPixel(r, g, b, originalAlpha) {
  const alpha = keyedAlpha(r, g, b, originalAlpha);
  if (alpha === 0) return { r: 0, g: 0, b: 0, alpha: 0 };
  const edgeGreen = Math.min(g, Math.max(r, b) + 24);
  return { r, g: alpha < originalAlpha ? edgeGreen : g, b, alpha };
}
```

- [ ] **Step 4: Verify GREEN and regenerate existing assets**

Run the Node tests, then run:

```bash
NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules" \
/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node \
tools/Art/process-countdown-assets.mjs --manifest tools/Art/countdown-assets.json
```

Inspect `hourglass_frame.png` and `countdown_ring_ornament.png` at original detail. Their transparent
edges must contain no obvious neon-green fringe.

- [ ] **Step 5: Commit the processor fix**

Stage only the processor, its test, and regenerated environment/UI outputs.

```bash
git commit -m "fix: remove chroma fringe from countdown art"
```

---

### Task 2: Generate and Register All Module Sprites

**Files:**
- Create: `Assets/Art/Generated/Countdown/Sources/core_module_sheet.png`
- Create: `Assets/Art/Generated/Countdown/Sources/utility_module_sheet.png`
- Create: `Assets/Art/Generated/Countdown/Modules/*.png`
- Create: `Assets/Resources/Countdown/Modules/*.png`
- Modify: `Assets/Scripts/Core/CountdownArtResources.cs`
- Modify: `Assets/Scripts/Core/GameSkin.cs`
- Modify: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**
- Produces: `CountdownArtResources.LoadModuleSprite(ModuleType type): Sprite`
- Produces: `CountdownArtResources.IsFormalModuleSprite(Sprite sprite): bool`
- Produces: `GameSkin.GetModuleIcon(ModuleType type): Sprite`

- [ ] **Step 1: Write failing module-coverage checks**

Add this to the Unity regression runner:

```csharp
foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
{
    Sprite direct = CountdownArtResources.LoadModuleSprite(type);
    Sprite skinIcon = GameSkin.LoadOrCreateRuntime().GetModuleIcon(type);
    Require(direct != null, $"Missing runtime module sprite for {type}.");
    Require(direct != PrototypeSprites.Square, $"{type} still uses the square fallback.");
    Require(skinIcon == direct, $"{type} does not use the shared module sprite.");
}
```

- [ ] **Step 2: Run the Unity art regression and verify RED**

Run `CountdownArtRegressionChecks.Run` in a clean detached verification worktree.

Expected: FAIL on the first module whose formal resource is missing.

- [ ] **Step 3: Generate the core module sheet with built-in ImageGen**

Use one built-in ImageGen call with this prompt:

```text
Use case: stylized-concept
Asset type: production 2D top-down game sprite sheet
Primary request: exact 4 columns by 2 rows containing eight isolated clockwork modules
Subject order, row-major: sand-furnace emitter with glass reservoir and right-facing piston nozzle;
90-degree redirector pipe; precision laser turret with long focusing lens; squat bomb mortar with
pressure chamber; snowflake turret with crystalline fork muzzle; black-hole turret with suspended
concentric gravity rings; spark turret with twin electrical coils; heatwave turret with circular
radiator fins.
Style: dark clockwork alchemy, shared octagonal brass base, readable at 64 pixels, restrained detail
Palette: neutral silver-gray, aged brass, pale gold, dark purple-black
Composition: identical scale, centered in equal cells, generous safe margins, no grid dividers
Backdrop: perfectly flat solid #00ff00 chroma key
Constraints: no text, letters, numbers, watermark, cast shadow, floor, gradients in the background,
or #00ff00 on the subjects
```

Copy the generated result into the exact core source path.

- [ ] **Step 4: Generate the utility module sheet with built-in ImageGen**

Use one built-in ImageGen call with this prompt:

```text
Use case: stylized-concept
Asset type: production 2D top-down game sprite sheet
Primary request: exact 5 columns by 2 rows containing ten isolated clockwork modules
Subject order, row-major: mining gear-drill with coin stamp; double-flame crucible amplifier;
T-junction splitter pipe; paired concentric portal rings; hourglass-plus relay coil; double-arrow
accelerator turbine; five nodes converging into one fusion core; one core splitting into five rays;
flame branding-seal enchant module; faceted surprise prism with an abstract asymmetric spark.
Style: same dark clockwork alchemy set, shared octagonal brass base, readable at 64 pixels
Palette: neutral silver-gray, aged brass, pale gold, dark purple-black
Composition: identical scale, centered in equal cells, generous safe margins, no grid dividers
Backdrop: perfectly flat solid #00ff00 chroma key
Constraints: no question mark, text, letters, numbers, watermark, cast shadow, floor, gradients in
the background, or #00ff00 on the subjects
```

Copy the generated result into the exact utility source path.

- [ ] **Step 5: Process and inspect all seventeen module outputs**

Run the manifest processor. Inspect every output for cell merge, clipping, wrong row-major order,
baked text, and green fringe. Regenerate only the failed sheet once if needed.

Copy the seventeen processed outputs into `Assets/Resources/Countdown/Modules/` with matching
filenames.

- [ ] **Step 6: Implement complete enum-to-resource mapping**

Add constants for the hourglass, ring, panel, board frame, and all module paths. Implement an
exhaustive `switch` in `LoadModuleSprite`. Cache the centered runtime sprites exactly as the
existing `LoadSprite` method does.

Change `GameSkin.GetModuleIcon` to return `CountdownArtResources.LoadModuleSprite(type)`. It must
not contain a two-case module switch.

- [ ] **Step 7: Run Unity art regression and verify GREEN**

Expected: every enum value returns the same non-prototype sprite through both APIs.

- [ ] **Step 8: Commit module assets and resource mapping**

```bash
git commit -m "art: add complete countdown module sprite set"
```

---

### Task 3: Use One Formal Icon Across Shop, Hand, Drag, and Tooltip

**Files:**
- Create: `Assets/Scripts/UI/ModuleIconVisuals.cs`
- Modify: `Assets/Scripts/UI/ModuleSlotView.cs`
- Modify: `Assets/Scripts/UI/HandSlot.cs`
- Modify: `Assets/Scripts/UI/ShopSlot.cs`
- Modify: `Assets/Scripts/UI/ModuleTooltipView.cs`
- Modify: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**
- Consumes: `CountdownArtResources.LoadModuleSprite(ModuleType type): Sprite`
- Produces: `ModuleIconVisuals.Apply(Image image, ModuleType type, bool disabled = false): void`

- [ ] **Step 1: Write failing shared-icon behavior test**

Create a `GameObject`, add `Image`, call `ModuleIconVisuals.Apply`, and require the mapped sprite,
white neutral tint, and reduced alpha only when disabled.

```csharp
ModuleIconVisuals.Apply(image, ModuleType.Spark);
Require(image.sprite == CountdownArtResources.LoadModuleSprite(ModuleType.Spark),
    "UI icon did not use the formal Spark sprite.");
Require(image.color == Color.white, "Formal module icon received a flat gameplay tint.");
```

- [ ] **Step 2: Run Unity art regression and verify RED**

Expected: compile failure because `ModuleIconVisuals` does not exist.

- [ ] **Step 3: Implement `ModuleIconVisuals`**

Set the shared formal sprite, preserve aspect, disable raycasts, and use white with alpha `0.42`
for disabled cards or `1.0` otherwise.

- [ ] **Step 4: Replace every card and drag fallback**

Use `ModuleIconVisuals.Apply` from:

- `ModuleSlotView.SetCard` and `ApplyState`;
- `HandSlot.CreateDragIcon`;
- `ShopSlot.CreateDragIcon`;
- the module branch of `ModuleTooltipView.Refresh`.

Keep enchant-only tooltip symbols outside this module mapping.

- [ ] **Step 5: Make slot fills translucent and neutral**

Change occupied slot backgrounds to dark neutral alpha no greater than `0.72`. Hover and selected
states use a border plus alpha no greater than `0.82`; do not fill the whole card green or blue.
Keep the rarity bar.

- [ ] **Step 6: Verify Unity art regression GREEN**

Also run localization regression because shop and hand text share the same views.

- [ ] **Step 7: Commit unified card icons**

```bash
git commit -m "feat: use formal module icons across the interface"
```

---

### Task 4: Replace Placed-Module Color Blocks With Persistent Skins

**Files:**
- Modify: `Assets/Scripts/Modules/ModuleSkinApplicator.cs`
- Modify: `Assets/Scripts/UI/PlacementController.cs`
- Modify: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**
- Consumes: `CountdownArtResources.LoadModuleSprite(ModuleType type): Sprite`
- Produces: `ModuleSkinApplicator.Apply(ModuleBase module): bool`
- Produces: `ModuleSkinApplicator.RefreshNow(): void`

- [ ] **Step 1: Write failing placed-module test**

Create a real `SparkModule`, apply its card data, apply the skin, and assert:

```csharp
bool applied = ModuleSkinApplicator.Apply(module);
SpriteRenderer body = module.GetComponent<SpriteRenderer>();
Require(applied, "Spark formal skin was not applied.");
Require(body.sprite == CountdownArtResources.LoadModuleSprite(ModuleType.Spark),
    "Placed Spark retained its prototype body.");
Require(module.transform.Find("CountdownClockwork") == null,
    "Old decorative clock face is still stacked over the module body.");
```

Call `module.RefreshVisual()` and `applicator.RefreshNow()` and require the formal sprite remains.

- [ ] **Step 2: Run Unity art regression and verify RED**

Expected: FAIL because `Apply` returns void and the root body remains a prototype sprite.

- [ ] **Step 3: Convert the applicator into a persistent skin controller**

`Apply` adds or reuses a `ModuleSkinApplicator` component, caches the module and root
`SpriteRenderer`, removes the old `CountdownClockwork` child, and calls `RefreshNow`.

`RefreshNow` sets the mapped sprite, sorting order, and a restrained tint:

```csharp
Color accent = ModuleCatalog.GetDisplayColor(module.ModuleType);
body.color = Color.Lerp(Color.white, accent, 0.18f);
```

`LateUpdate` calls `RefreshNow` only when a module class has overwritten the sprite or made the
body fully opaque with a flat gameplay tint.

- [ ] **Step 4: Preserve placement, fusion, and ghost wiring**

Keep the existing calls in `CreateModule` and `EnsureGhost`. Ensure type-changing fusion creates
the replacement through `CreateModule(fused)`, so it automatically receives the correct formal
skin.

- [ ] **Step 5: Verify Unity art and gameplay regressions GREEN**

Run both `CountdownArtRegressionChecks.Run` and `GmtkBugfixRegressionChecks.Run`.

- [ ] **Step 6: Commit placed-module skins**

```bash
git commit -m "fix: replace placed module color blocks with skins"
```

---

### Task 5: Wire the Complete Ring, Hourglass, and Neutral Surfaces

**Files:**
- Create runtime copies under: `Assets/Resources/Countdown/UI/`
- Create runtime copies under: `Assets/Resources/Countdown/Environment/`
- Modify: `Assets/Scripts/UI/CountdownRingView.cs`
- Modify: `Assets/Scripts/UI/SandClockPanel.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Modify: `Assets/Scripts/Board/GridBoard.cs`
- Modify: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**
- Consumes: `CountdownArtResources.LoadSprite(string path, Sprite fallback): Sprite`
- Produces: `CountdownRingView.OrnamentRenderer`
- Produces: `CountdownRingView.MaxSortingOrder`
- Produces: `SandClockPanel.FrameImage`

- [ ] **Step 1: Write failing ring and hourglass hierarchy tests**

Require the ring and hourglass resources to load. Instantiate `CountdownRingView`, initialize it,
and require an ornament plus sixty ticks with maximum sorting order lower than board gameplay
sprites.

```csharp
Require(Resources.Load<Texture2D>(CountdownArtResources.RingOrnamentPath) != null,
    "Countdown ring ornament is not runtime-loadable.");
Require(Resources.Load<Texture2D>(CountdownArtResources.HourglassFramePath) != null,
    "Hourglass frame is not runtime-loadable.");
Require(ring.TickRendererCount == 60, "Countdown ring lost its sixty ticks.");
Require(ring.OrnamentRenderer != null, "Countdown ring ornament is missing.");
Require(ring.MaxSortingOrder < 0, "Countdown ring must remain below gameplay.");
```

- [ ] **Step 2: Run Unity art regression and verify RED**

Expected: FAIL because the resource constants and ornament renderer do not exist.

- [ ] **Step 3: Copy formal runtime resources**

Copy the cleaned hourglass frame, timer plaque, ring ornament, panel background, board frame, and
board state overlay into the matching `Assets/Resources/Countdown` subdirectories.

- [ ] **Step 4: Implement the complete ring hierarchy**

Create the ornament at sorting order `-4` and sixty dynamic ticks at `-3`. Center and size the
ring from a camera viewport center `(0.38, 0.53)` and radius `0.27`, converting those values to
world coordinates so the composition remains stable across aspect ratios.

Tint the ornament dark brass with alpha `0.48`. Unlit ticks use alpha `0.28`; normal lit ticks use
alpha `0.72`; warning ticks use alpha `0.9`. Board cells remain at sorting order `0+`.

- [ ] **Step 5: Replace the left hourglass placeholders**

In `CreateSandClockPanel`, enlarge the panel while preserving its endpoint center. Add two masked
or cropped sand fill images behind a formal frame image. Load the frame from
`HourglassFramePath`, set `preserveAspect = true`, and remove the opaque black background.

Pass the frame into `SandClockPanel.Bind`. Keep the existing top/bottom ratio colors, penalty
shake, gain text, and `GetWorldLeakPosition` behavior.

- [ ] **Step 6: Neutralize board and sidebar color blocks**

Use the panel resource for the sidebar plate, hand panel, and shop panel with white or low-alpha
neutral tint. Reduce board valid/invalid/hover overlays to alpha `0.18–0.32` and retain brass
cell art as the dominant surface. Keep locked hatching and enchant readability.

- [ ] **Step 7: Verify Unity regressions GREEN**

Run art, localization, and gameplay regression entry points.

- [ ] **Step 8: Commit integrated environment and UI art**

```bash
git commit -m "fix: integrate countdown ring hourglass and panels"
```

---

### Task 6: Visual QA and Final Regression

**Files:**
- Modify only files required by defects found during this task.

**Interfaces:**
- Consumes all completed visual systems.
- Produces a verified 1920×1080 Game view.

- [ ] **Step 1: Restart Play mode**

Stop Play, wait for compilation/import, then enter Play again so `GameBootstrap` rebuilds every
runtime-created object.

- [ ] **Step 2: Capture the normal-time Game view**

At 1920×1080, capture a view containing:

- the complete readable ring;
- the formal left hourglass;
- occupied shop offers;
- at least one purchased hand card;
- at least three different placed modules;
- valid and invalid placement states.

- [ ] **Step 3: Inspect against the screenshot defects**

Reject the result if any of these remain:

- scattered detached ring bars with no recognizable clock;
- gray square occupied-card icons;
- large opaque colored blocks behind placed modules;
- square hourglass chambers without a formal frame;
- board or sidebar flat fills hiding their textures;
- green chroma fringe.

- [ ] **Step 4: Run complete automated verification**

Run:

```bash
NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules" \
/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node \
--test tools/Art/process-countdown-assets.test.mjs
```

Then run these Unity batch entry points in the clean verification worktree:

```text
CountdownArtRegressionChecks.Run
LocalizationRegressionChecks.Run
GmtkBugfixRegressionChecks.Run
```

All commands must exit `0` and log their `PASS` marker.

- [ ] **Step 5: Check the exact Git scope**

Run `git diff --check`, inspect `git status --short`, and confirm no user-owned Package Manager,
ProjectSettings, `.codex`, or unrelated documentation file is staged.

- [ ] **Step 6: Commit any final visual-only correction**

Only if Step 3 found and fixed a concrete defect:

```bash
git commit -m "fix: polish countdown game view readability"
```

- [ ] **Step 7: Report local completion**

Report commit hashes, test commands, PASS markers, generated asset paths, and the requirement to
run `git push origin main` separately. Do not claim that GitHub was updated.
