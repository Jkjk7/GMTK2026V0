# Countdown Art Pass Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current prototype squares with a coherent “dark clockwork alchemy” art pass centered on a giant countdown ring, 17 readable module skins, four clockwork enemy silhouettes, and state-driven combat VFX.

**Architecture:** `GameSkin` remains the single resource registry and gains enum-keyed module, enemy, environment, and VFX entries with safe prototype fallbacks. Runtime behavior is split between `CountdownRingView`, `ModuleSkinApplicator`, `EnemyVisualController`, and `CombatVfxService`; generated bitmap assets are processed by a deterministic Node/Sharp pipeline and assigned by an editor builder.

**Tech Stack:** Unity 6000.5.4f1, C#, Unity UI/SpriteRenderer, Unity Editor batch mode, built-in ImageGen, Node.js with bundled `sharp`, existing runtime bootstrap architecture.

## Global Constraints

- Follow `docs/superpowers/specs/2026-07-26-countdown-art-direction-design.md`.
- Keep gameplay values, targeting, wave composition, enemy paths, damage timing, enchant logic, and module effects unchanged.
- Preserve safe fallback to `PrototypeSprites.Square` or `PrototypeSprites.Circle` for every missing art asset.
- Store production images under `Assets/Art/Generated/Countdown/`.
- Do not bake readable words or numbers into generated textures.
- Use 60 countdown ticks and a 20,000 ms warning threshold.
- Clamp the displayed countdown ratio to `[0, 1]`; overfill produces one gold pulse and never creates a second ring.
- Do not add Unity packages or stage the existing dirty `Packages/` and `ProjectSettings/` changes.
- Preserve the user's uncommitted `MinerModule.cs` change from `GetInstanceID()` to `GetEntityId()`.
- Keep `.superpowers/brainstorm/` local and out of commits.
- Start execution on an isolated `codex/countdown-art-pass` branch or worktree; only merge after all verification is green.

## Jam-deadline fast path

When less than one hour remains, Task 9 supersedes Tasks 2–8 as the submission MVP. It must
stay procedural and self-contained so missing generated bitmaps never block the build. Generated
environment/UI sprites may be layered in when present, but every visual must retain a prototype
fallback.

### Task 9: Ship the procedural countdown-theme MVP

**Files:**

- Create: `Assets/Scripts/UI/CountdownVisualRules.cs`
- Create: `Assets/Scripts/UI/CountdownRingView.cs`
- Create: `Assets/Scripts/Modules/ModuleSkinApplicator.cs`
- Create: `Assets/Scripts/Combat/EnemyVisualController.cs`
- Create: `Assets/Scripts/Combat/CombatVfxService.cs`
- Create: `Assets/Scripts/Combat/TransientSpriteVfx.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Modify: `Assets/Scripts/UI/SandClockPanel.cs`
- Modify: `Assets/Scripts/UI/PlacementController.cs`
- Modify: `Assets/Scripts/Combat/Enemy.cs`
- Modify: `Assets/Scripts/Combat/CombatDamage.cs`
- Test/Create: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Required submission behavior:**

- Build one giant world-space ring behind the board and battle lane from exactly 60 tick
  renderers. The lit arc equals the clamped remaining-time ratio. At or below 20 seconds,
  alternate the lit tick color between warning orange and red without covering units.
- Keep the precise countdown text/hourglass UI visible and make its warning state visually
  prominent.
- Give every placed module a readable clockwork face using `ModuleType`-specific color and at
  least one procedural symbol/hand/gear detail. Apply the same visual to placement ghosts and
  fused results. Do not change module function, footprint, or board logic.
- Give enemies a clockwork core and rotating clock hand, scaling details by the existing enemy
  variant when available. Burn shows ember/flame overlays, chill shows a cyan frost rim, and
  sand buff shows a gold orbit. Do not change enemy stats or movement.
- Melt creates a brief white-hot burst; other hit/death accents are bounded one-shot sprites
  that self-destroy. Persistent status visuals must follow the enemy and clear when status ends.
- All visuals use `PrototypeSprites.Square`/`Circle` and the palette `#0B0910`, `#211826`,
  `#B58248`, `#FFD676`, with clear sorting so the ring remains behind gameplay.
- Add regression checks for exactly 60 ticks, ratio clamping, 20,000 ms warning threshold,
  module-type coverage, and no gameplay constants changed by this task.
- Run the existing Unity regression entry point and the new countdown-art checks in Unity
  `6000.5.4f1` batch mode. If a separate editor entry point is too costly, include the art
  checks in the existing regression entry point.
- Never stage or commit the pre-existing `Assets/Scripts/Modules/MinerModule.cs` compatibility
  edit. Commit the MVP in one focused commit and write the SDD report.

---

## File Map

### Asset tooling

- Create `Tools/Art/process-countdown-assets.mjs`: remove chroma-key backgrounds, trim, pad, resize, and slice fixed-grid sheets.
- Create `Tools/Art/process-countdown-assets.test.mjs`: Node tests for transparency, subject preservation, sizing, and sheet-name validation.
- Create `Tools/Art/countdown-assets.json`: exact source-sheet grid order and production output paths.
- Create `Assets/Art/Generated/Countdown/ART_PROMPTS.md`: exact prompts, source image names, output mapping, and generation date.

### Skin registry and editor build

- Modify `Assets/Scripts/Core/GameSkin.cs`: enum-keyed module/enemy entries, environment/UI fields, VFX fields, and safe getters.
- Create `Assets/Scripts/Core/CountdownVfxKind.cs`: stable VFX keys shared by registry and runtime services.
- Create `Assets/Editor/CountdownSkinAssetBuilder.cs`: import fixed PNG paths and create/update `Assets/Resources/Game/GameSkin.asset`.
- Create `Assets/Editor/CountdownArtRegressionChecks.cs`: editor entry point for all art-system regression checks.

### Countdown and environment

- Create `Assets/Scripts/UI/CountdownVisualRules.cs`: shared 60-tick and warning-threshold calculations.
- Create `Assets/Scripts/UI/CountdownRingView.cs`: world-space tick ring and penalty/gain/overfill pulses.
- Modify `Assets/Scripts/UI/SandClockPanel.cs`: use shared rules and formal hourglass/timer skin.
- Modify `Assets/Scripts/UI/GridCellView.cs`: switch normal/hover/valid/invalid sprites as well as colors.
- Modify `Assets/Scripts/Game/GameBootstrap.cs`: load skin before world construction and create the ring, skinned backdrop, timer, and VFX service.
- Modify `Assets/Scripts/Board/GridBoard.cs`: use skinned cells and frame while retaining state colors.
- Modify `Assets/Scripts/Board/Emitter.cs`: use the sand-furnace sprite and existing buffer animation.
- Modify `Assets/Scripts/Ball/EnergyBall.cs`: use the generated time-crystal ball sprite.

### Modules

- Create `Assets/Scripts/Modules/ModuleSkinApplicator.cs`: set a module root renderer from `ModuleType`.
- Modify `Assets/Scripts/UI/PlacementController.cs`: apply skins to placed, fused, and ghost modules.
- Modify `Assets/Scripts/UI/ModuleSlotView.cs`: keep formal grayscale icons white enough for existing functional tints.

### Enemies and VFX

- Create `Assets/Scripts/Combat/EnemyVisualController.cs`: persistent burn, chill, and sand-buff overlays.
- Create `Assets/Scripts/Combat/CombatVfxService.cs`: bounded transient hit, melt, and death effects.
- Create `Assets/Scripts/Combat/TransientSpriteVfx.cs`: one-shot position, scale, rotation, and alpha animation.
- Modify `Assets/Scripts/Combat/Enemy.cs`: choose type skin and notify visual components without changing combat semantics.
- Modify `Assets/Scripts/Combat/CombatDamage.cs`: notify the VFX service when Melt triggers.
- Modify `Assets/Scripts/Combat/SandVfxService.cs`: pass the skin to flying sand particles.
- Modify `Assets/Scripts/Combat/SandGrainVfx.cs`: use the generated sand-grain sprite with circle fallback.

### Documentation

- Modify `Assets/Resources/Game/README.md`: describe automatic skin construction and regeneration.
- Modify `docs/agent-handoff/04-systems-modules.md`: document module skin lookup.
- Modify `docs/agent-handoff/05-systems-combat.md`: document enemy silhouettes and status VFX.
- Modify `docs/agent-handoff/09-ui-layout.md`: document the world-space countdown ring and visual sorting.

---

### Task 1: Build the deterministic image-processing pipeline

**Files:**

- Create: `Tools/Art/process-countdown-assets.mjs`
- Create: `Tools/Art/process-countdown-assets.test.mjs`
- Create: `Tools/Art/countdown-assets.json`

**Interfaces:**

- Consumes: ImageGen PNGs placed in `Assets/Art/Generated/Countdown/Sources/`.
- Produces: `processSingle(entry, rootDir): Promise<void>`, `processSheet(entry, rootDir): Promise<void>`, and CLI `node Tools/Art/process-countdown-assets.mjs --manifest Tools/Art/countdown-assets.json`.
- Produces: square transparent sprites at 512×512 and an opaque battle backdrop at 1536×512.

- [ ] **Step 1: Write failing Node tests**

Use bundled `sharp` to create test fixtures in a temporary directory. The tests must assert:

```javascript
test("green key becomes transparent without erasing a cyan subject", async () => {
  const pixels = await sharp(output).ensureAlpha().raw().toBuffer();
  assert.equal(pixels[3], 0);              // keyed corner
  assert.ok(pixels[7] > 240);              // cyan subject alpha
});

test("single sprites are padded to exactly 512 square pixels", async () => {
  const info = await sharp(output).metadata();
  assert.deepEqual([info.width, info.height], [512, 512]);
});

test("sheet entry rejects a name count that does not match columns times rows", async () => {
  await assert.rejects(
    () => processSheet({ columns: 2, rows: 2, names: ["a"] }, root),
    /requires exactly 4 names/
  );
});
```

- [ ] **Step 2: Run the processor tests and verify RED**

```bash
COUNTDOWN_NODE="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node"
COUNTDOWN_NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules"
NODE_PATH="$COUNTDOWN_NODE_PATH" "$COUNTDOWN_NODE" --test Tools/Art/process-countdown-assets.test.mjs
```

Expected: FAIL because `process-countdown-assets.mjs` does not exist.

- [ ] **Step 3: Implement chroma removal and square fitting**

Export this key-alpha calculation and apply it to RGBA pixels from `sharp(...).ensureAlpha().raw()`:

```javascript
export function keyedAlpha(r, g, b, originalAlpha) {
  const distance = Math.hypot(r, g - 255, b);
  if (distance <= 42) return 0;
  if (distance >= 112) return originalAlpha;
  return Math.round(originalAlpha * ((distance - 42) / 70));
}
```

For transparent sprites:

1. remove `#00FF00`;
2. reconstruct a PNG from the modified raw buffer;
3. call `trim({ background: { r: 0, g: 0, b: 0, alpha: 0 } })`;
4. call `resize(size, size, { fit: "contain", background: transparent })`;
5. write the configured output path.

For opaque backgrounds, use:

```javascript
image.resize(1536, 512, { fit: "cover", position: "centre" }).png();
```

- [ ] **Step 4: Implement fixed-grid sheet slicing**

Validate `names.length === columns * rows`, resize the source so both dimensions divide evenly by the grid, then extract cells in row-major order:

```javascript
const index = row * entry.columns + col;
const region = {
  left: col * cellWidth,
  top: row * cellHeight,
  width: cellWidth,
  height: cellHeight
};
await processTransparentBuffer(await source.clone().extract(region).png().toBuffer(), output);
```

The CLI reads the manifest path after `--manifest`, resolves all relative paths from the repository root, and exits nonzero on an invalid grid. A missing source exits nonzero unless that entry has `"allowMissing": true`; allowed missing sources print one `SKIP` line and produce no outputs.

- [ ] **Step 5: Add the exact manifest**

Use these source files and row-major output names:

```json
{
  "root": "Assets/Art/Generated/Countdown",
  "entries": [
    {
      "mode": "sheet",
      "source": "Sources/environment_ui_sheet.png",
      "columns": 4,
      "rows": 2,
      "names": [
        "UI/hourglass_frame.png",
        "UI/timer_plaque.png",
        "Environment/board_cell.png",
        "Environment/board_state_overlay.png",
        "Environment/board_frame.png",
        "Environment/countdown_ring_ornament.png",
        "Environment/panel_background.png",
        "Environment/energy_ball.png"
      ],
      "size": 512
    },
    {
      "mode": "opaque",
      "source": "Sources/battle_lane_backdrop.png",
      "output": "Environment/battle_lane_backdrop.png",
      "width": 1536,
      "height": 512
    },
    {
      "mode": "sheet",
      "source": "Sources/core_module_sheet.png",
      "allowMissing": true,
      "columns": 4,
      "rows": 2,
      "names": [
        "Environment/emitter_body.png",
        "Modules/module_redirector.png",
        "Modules/module_projectile.png",
        "Modules/module_bomb.png",
        "Modules/module_ice_laser.png",
        "Modules/module_black_hole.png",
        "Modules/module_spark.png",
        "Modules/module_heatwave.png"
      ],
      "size": 512
    },
    {
      "mode": "sheet",
      "source": "Sources/utility_module_sheet.png",
      "allowMissing": true,
      "columns": 5,
      "rows": 2,
      "names": [
        "Modules/module_miner.png",
        "Modules/module_flame_amp.png",
        "Modules/module_splitter.png",
        "Modules/module_portal.png",
        "Modules/module_relay.png",
        "Modules/module_accelerator.png",
        "Modules/module_fusion.png",
        "Modules/module_fission.png",
        "Modules/module_fire_enchant.png",
        "Modules/module_surprise.png"
      ],
      "size": 512
    },
    {
      "mode": "sheet",
      "source": "Sources/enemy_sheet.png",
      "allowMissing": true,
      "columns": 2,
      "rows": 2,
      "names": [
        "Enemies/enemy_swarm.png",
        "Enemies/enemy_normal.png",
        "Enemies/enemy_tank.png",
        "Enemies/enemy_boss.png"
      ],
      "size": 512
    },
    {
      "mode": "sheet",
      "source": "Sources/vfx_sheet.png",
      "allowMissing": true,
      "columns": 4,
      "rows": 2,
      "names": [
        "VFX/ember.png",
        "VFX/flame_crack.png",
        "VFX/frost_rim.png",
        "VFX/melt_burst.png",
        "VFX/sand_shard.png",
        "VFX/clock_tick.png",
        "VFX/gear_fragment.png",
        "VFX/soft_glow.png"
      ],
      "size": 512
    }
  ]
}
```

- [ ] **Step 6: Run Node tests and verify GREEN**

Run the Step 2 command. Expected: all processor tests pass.

- [ ] **Step 7: Commit the tooling**

```bash
git add Tools/Art/process-countdown-assets.mjs Tools/Art/process-countdown-assets.test.mjs Tools/Art/countdown-assets.json
git commit -m "build: add countdown art processing pipeline"
```

---

### Task 2: Expand `GameSkin` and automate the Unity asset

**Files:**

- Create: `Assets/Scripts/Core/CountdownVfxKind.cs`
- Modify: `Assets/Scripts/Core/GameSkin.cs`
- Create: `Assets/Editor/CountdownSkinAssetBuilder.cs`
- Create: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Produces: `GameSkin.GetModuleBody(ModuleType)`, `GameSkin.GetModuleIcon(ModuleType)`, `GameSkin.GetEnemyBody(EnemyGoldType)`, and `GameSkin.GetVfxSprite(CountdownVfxKind)`.
- Produces: `CountdownSkinAssetBuilder.Build()` callable with Unity `-executeMethod`.
- Produces: `CountdownArtRegressionChecks.Run()` callable with Unity `-executeMethod`.

- [ ] **Step 1: Write a reflection-based RED regression runner**

Create `CountdownArtRegressionChecks.Run()` with exception handling and `EditorApplication.Exit(0/1)`. Before the implementation exists, use reflection so the project still compiles:

```csharp
static void RequiresSkinLookupSurface()
{
    Require(typeof(GameSkin).GetMethod("GetModuleBody") != null, "GameSkin.GetModuleBody is missing.");
    Require(typeof(GameSkin).GetMethod("GetEnemyBody") != null, "GameSkin.GetEnemyBody is missing.");
    Require(typeof(GameSkin).GetMethod("GetVfxSprite") != null, "GameSkin.GetVfxSprite is missing.");
}
```

Include these shared helpers for later RED checks:

```csharp
static Type FindRuntimeType(string name)
{
    Type type = Type.GetType(name + ", Assembly-CSharp");
    Require(type != null, name + " is missing.");
    return type;
}

static object InvokeStatic(string typeName, string methodName, params object[] args)
{
    MethodInfo method = FindRuntimeType(typeName).GetMethod(
        methodName,
        BindingFlags.Public | BindingFlags.Static);
    Require(method != null, typeName + "." + methodName + " is missing.");
    return method.Invoke(null, args);
}

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
```

Also require `ModuleCatalog.GetSellableTypes().Length == 17`.

- [ ] **Step 2: Run the Unity runner and verify RED**

Copy the project so the currently open editor does not lock it:

```bash
COUNTDOWN_VERIFY="$(mktemp -d)"
rsync -a \
  --exclude '.git' --exclude '.codex' --exclude '.superpowers' \
  --exclude 'Library' --exclude 'Temp' --exclude 'Logs' --exclude 'obj' \
  ./ "$COUNTDOWN_VERIFY/"
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$COUNTDOWN_VERIFY" \
  -executeMethod CountdownArtRegressionChecks.Run \
  -logFile "$COUNTDOWN_VERIFY/countdown-art-red.log"
```

Expected: nonzero exit status with `GameSkin.GetModuleBody is missing`; compilation errors are not an acceptable RED state.

- [ ] **Step 3: Add stable registry types**

Create:

```csharp
public enum CountdownVfxKind
{
    Ember,
    FlameCrack,
    FrostRim,
    MeltBurst,
    SandShard,
    ClockTick,
    GearFragment,
    SoftGlow
}
```

Add serializable `ModuleVisualEntry`, `EnemyVisualEntry`, and `VfxVisualEntry` structs to `GameSkin.cs`. Each entry stores its enum key and Sprite. `ModuleVisualEntry` stores both `body` and `icon`.

- [ ] **Step 4: Implement safe getters**

Use a linear scan because the arrays contain at most 17 entries:

```csharp
public Sprite GetModuleBody(ModuleType type)
{
    for (int i = 0; moduleVisuals != null && i < moduleVisuals.Length; i++)
        if (moduleVisuals[i].type == type && moduleVisuals[i].body != null)
            return moduleVisuals[i].body;
    return ResolveSquare(null);
}

public Sprite GetEnemyBody(EnemyGoldType type)
{
    for (int i = 0; enemyVisuals != null && i < enemyVisuals.Length; i++)
        if (enemyVisuals[i].type == type && enemyVisuals[i].body != null)
            return enemyVisuals[i].body;
    return ResolveSquare(null);
}
```

`GetModuleIcon` uses its assigned icon and falls back to `GetModuleBody(type)`. `GetVfxSprite` returns `null` when missing so VFX callers can skip the decorative layer without affecting gameplay.

Cache `LoadOrCreateRuntime()` in a private static `GameSkin s_runtimeSkin`: first prefer `Resources.Load<GameSkin>("Game/GameSkin")`, otherwise create one fallback instance, and return the same object on later calls. This prevents one fallback ScriptableObject allocation per spawned enemy.

Add the new fields below and retain the existing `emitterBody` and `panelBackground` fields:

```csharp
public Sprite countdownRingOrnament;
public Sprite battleBackdrop;
public Sprite boardFrame;
public Sprite boardStateOverlay;
public Sprite hourglassFrame;
public Sprite timerPlaque;
```

- [ ] **Step 5: Implement the editor builder**

`CountdownSkinAssetBuilder.Build()` must:

1. call `AssetDatabase.Refresh()`;
2. set every production PNG importer to `TextureImporterType.Sprite`, single mode, transparent where applicable, 512 pixels per unit, bilinear filtering, and compression disabled; 512 PPU keeps each processed 512×512 module or enemy sprite at one world unit before its existing transform scale;
3. create or load `Assets/Resources/Game/GameSkin.asset`;
4. populate all 17 module entries, four enemy entries, eight VFX entries, and environment/UI fields from the manifest output paths;
5. call `EditorUtility.SetDirty`, `AssetDatabase.SaveAssets`, and `AssetDatabase.Refresh`.

Use one exact loader:

```csharp
static Sprite LoadSprite(string relativePath)
{
    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
        "Assets/Art/Generated/Countdown/" + relativePath);
    if (sprite == null)
        Debug.LogWarning("[CountdownSkin] Missing " + relativePath);
    return sprite;
}
```

For the seven refined modules, use the same sprite as body and icon. For the ten utility/path modules, also use the generated icon as their first-pass body.

- [ ] **Step 6: Replace reflection checks with behavior checks**

Create an in-memory `GameSkin`, assign one test module entry and one enemy entry using `PrototypeSprites.Circle`, and assert:

- exact enum lookup returns the assigned sprite;
- an unassigned module returns a non-null square fallback;
- an unassigned enemy returns a non-null square fallback;
- all 17 sellable module types return non-null body and icon;
- every `CountdownVfxKind` call is safe when the VFX array is empty.
- two consecutive `LoadOrCreateRuntime()` calls return the same instance.

- [ ] **Step 7: Run the Unity runner and verify GREEN**

Repeat the temporary-copy command. Expected exit status `0` and log marker `[Countdown Art Regression] PASS`.

- [ ] **Step 8: Commit the registry and builder**

```bash
git add Assets/Scripts/Core/CountdownVfxKind.cs Assets/Scripts/Core/GameSkin.cs Assets/Editor/CountdownSkinAssetBuilder.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: add countdown skin registry"
```

---

### Task 3: Add the 60-tick countdown ring and shared warning rules

**Files:**

- Create: `Assets/Scripts/UI/CountdownVisualRules.cs`
- Create: `Assets/Scripts/UI/CountdownRingView.cs`
- Modify: `Assets/Scripts/UI/SandClockPanel.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Test: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Produces: `CountdownVisualRules.TickCount`, `WarningThresholdMs`, `GetLitTickCount(int)`, `GetRatio01(int)`, and `IsWarning(int)`.
- Produces: `CountdownRingView.Initialize(SandClock, GameSkin, Camera, Vector2, float)`.
- Exposes for verification: `CountdownRingView.LitTickCount` and `CountdownRingView.WarningActive`.

- [ ] **Step 1: Add RED checks for exact countdown math**

The runner must assert through reflection:

```csharp
Require((int)InvokeStatic("CountdownVisualRules", "GetLitTickCount", 100_000) == 60, "Initial sand must light 60 ticks.");
Require((int)InvokeStatic("CountdownVisualRules", "GetLitTickCount", 50_000) == 30, "Half sand must light 30 ticks.");
Require((int)InvokeStatic("CountdownVisualRules", "GetLitTickCount", 150_000) == 60, "Overfill must clamp to 60 ticks.");
Require((int)InvokeStatic("CountdownVisualRules", "GetLitTickCount", 0) == 0, "Zero sand must light zero ticks.");
Require((bool)InvokeStatic("CountdownVisualRules", "IsWarning", 20_000), "20 seconds must be warning state.");
Require(!(bool)InvokeStatic("CountdownVisualRules", "IsWarning", 20_001), "20.001 seconds must not be warning state.");
```

- [ ] **Step 2: Run the Unity runner and verify RED**

Expected: runtime assertion that `CountdownVisualRules` is missing, with no compilation errors.

- [ ] **Step 3: Implement shared countdown rules**

```csharp
public static class CountdownVisualRules
{
    public const int TickCount = 60;
    public const int WarningThresholdMs = 20_000;

    public static float GetRatio01(int remainingMs) =>
        Mathf.Clamp01(remainingMs / (float)SandClock.InitialSandMs);

    public static int GetLitTickCount(int remainingMs) =>
        Mathf.Clamp(Mathf.CeilToInt(GetRatio01(remainingMs) * TickCount), 0, TickCount);

    public static bool IsWarning(int remainingMs) =>
        remainingMs <= WarningThresholdMs;
}
```

Replace `SandClockPanel`'s private warning constant and ratio calculation with these methods.

In `GameBootstrap.CreateSandClockPanel`, keep the physical hourglass panel centered on the Mage/end-of-lane viewport position, but create the countdown Text as a sibling under the Canvas at anchors `(0.27, 0.63)` to `(0.49, 0.71)`. Pass that Text into the same `SandClockPanel.Bind`; this keeps one source of formatting and warning color while making the precise readout part of the giant ring rather than the small hourglass.

- [ ] **Step 4: Implement `CountdownRingView`**

`Initialize` creates one optional ornament renderer plus exactly 60 tick `SpriteRenderer`s. Scale the ornament to the ring diameter at sorting order `-2`. Place each tick around a circle, rotate it radially, set sorting order `-1`, and use `PrototypeSprites.Square` if no formal tick sprite exists. Use:

```csharp
float degrees = 90f - i * (360f / CountdownVisualRules.TickCount);
float radians = degrees * Mathf.Deg2Rad;
tick.transform.position = center + new Vector3(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
tick.transform.rotation = Quaternion.Euler(0f, 0f, degrees - 90f);
tick.transform.localScale = new Vector3(radius * 0.035f, radius * 0.12f, 1f);
```

Subscribe to `OnSandChanged`, `OnPenalty`, and `OnSandGained`. `Refresh(int ms)` enables the first `LitTickCount` ticks in sand gold, leaves the remaining ticks at low-opacity brown, and switches lit ticks to danger red at the warning threshold.

Penalty sets a negative pulse timer; gain sets a positive pulse timer. Gain while `RemainingMs > InitialSandMs` uses the same positive pulse and keeps 60 lit ticks.

`OnDestroy` unsubscribes all three handlers from the bound `SandClock`.

- [ ] **Step 5: Wire the ring into `GameBootstrap`**

Load `_skin = GameSkin.LoadOrCreateRuntime()` at the start of `BuildPrototype()`, before board and UI construction. After creating the battle backdrop, create:

```csharp
var ringGo = new GameObject("CountdownRing");
ringGo.transform.SetParent(worldRoot, false);
var ring = ringGo.AddComponent<CountdownRingView>();
ring.Initialize(sandClock, _skin, worldCam, new Vector2(0.38f, 0.53f), 0.27f);
```

Remove the later duplicate `_skin` load. The ring must have no collider, `Graphic`, or input component.

- [ ] **Step 6: Run regression checks and inspect hierarchy**

Expected:

- runner exits `0`;
- one `CountdownRing` exists;
- its internal tick collection contains exactly 60 renderers, independent of the optional ornament renderer;
- no ring object has a Collider or UI `Graphic`;
- the existing projectile/enchant runner still passes.

- [ ] **Step 7: Commit the countdown foundation**

```bash
git add Assets/Scripts/UI/CountdownVisualRules.cs Assets/Scripts/UI/CountdownRingView.cs Assets/Scripts/UI/SandClockPanel.cs Assets/Scripts/Game/GameBootstrap.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: add world-space countdown ring"
```

---

### Task 4: Generate and integrate environment, hourglass, board, and emitter art

**Files:**

- Create: `Assets/Art/Generated/Countdown/Sources/environment_ui_sheet.png`
- Create: `Assets/Art/Generated/Countdown/Sources/battle_lane_backdrop.png`
- Create: processed files in `Assets/Art/Generated/Countdown/Environment/` and `UI/`
- Create: `Assets/Art/Generated/Countdown/ART_PROMPTS.md`
- Modify: `Assets/Scripts/Board/GridBoard.cs`
- Modify: `Assets/Scripts/Board/Emitter.cs`
- Modify: `Assets/Scripts/Ball/EnergyBall.cs`
- Modify: `Assets/Scripts/UI/GridCellView.cs`
- Modify: `Assets/Scripts/UI/SandClockPanel.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Modify: `Assets/Resources/Game/GameSkin.asset`
- Test: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Consumes: the Task 1 processor and Task 2 builder.
- Produces: visible skinned board cells, battle backdrop, hourglass frame, timer plaque, countdown ornament, panel background, and emitter body.

- [ ] **Step 1: Generate the eight-cell environment/UI sheet**

Use the imagegen skill with this prompt:

```text
Production-ready 2D game sprite sheet for a dark clockwork alchemy tower-defense game themed around COUNTDOWN. Exact 4 columns by 2 rows, eight isolated assets, centered in equal cells, wide safe margins, no labels, no letters, no numbers, no grid dividers. Row 1: ornate brass-and-glass hourglass frame with empty transparent glass center; compact dark brass timer plaque with blank center; square dark-steel board tile with subtle brass bevel; square transparent hatched clock-tick overlay for hover and placement states. Row 2: modular brass board-frame ornament; large thin circular clock ornament with gears and minute ticks, mostly empty center; dark purple-black brass-edged UI panel; small glowing faceted sand-gold time crystal energy orb. Orthographic 2D game asset style, clean readable silhouettes at small size, restrained detail, colors #0B0910 #211826 #B58248 #FFD676. Every asset isolated on a perfectly flat pure green #00FF00 background, no cast shadow outside each asset, no text, no watermark.
```

Save the returned source image as `Sources/environment_ui_sheet.png`.

- [ ] **Step 2: Generate the battle-lane backdrop**

Use:

```text
Wide production-ready 2D battle-lane background for a dark clockwork alchemy game, 3:1 horizontal composition. A mechanical conveyor corridor made of dark steel and aged brass, subtle broken clock markings and drifting sand, darker edges and a calm low-contrast center so bright enemies and projectiles remain readable. The central region must allow a giant glowing countdown ring to show through visually. No characters, no towers, no bullets, no UI, no words, no numbers, no watermark. Flat orthographic side-view game background, restrained detail, palette #0B0910 #211826 #B58248, opaque full-bleed image.
```

Save as `Sources/battle_lane_backdrop.png`.

- [ ] **Step 3: Record prompts and process both sources**

Write the exact prompts, row-major mappings, generation date, and “built-in ImageGen” into `ART_PROMPTS.md`. Then run:

```bash
COUNTDOWN_NODE="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node"
COUNTDOWN_NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules"
NODE_PATH="$COUNTDOWN_NODE_PATH" "$COUNTDOWN_NODE" \
  Tools/Art/process-countdown-assets.mjs \
  --manifest Tools/Art/countdown-assets.json
```

At this checkpoint, the four later source sheets have `"allowMissing": true` and must print one `SKIP` line each. Environment outputs must exist and have the configured dimensions.

- [ ] **Step 4: Build the `GameSkin.asset`**

Run Unity with `-executeMethod CountdownSkinAssetBuilder.Build` directly in the isolated implementation worktree. The original project may stay open because it is a different path. Verify the builder log contains no import error and keep the `.meta` files Unity generates beside every production PNG.

- [ ] **Step 5: Apply skin to board and backdrop**

Add optional `GameSkin skin = null` to `GridBoard.Initialize`. Store it and choose:

```csharp
sr.sprite = _skin != null ? _skin.ResolveSquare(_skin.normalCell) : PrototypeSprites.Square;
```

Change `GameBootstrap` to call `board.Initialize(cellSize, Vector2.zero, _skin)`.

Pass the skin to `GridCellView.Bind(SpriteRenderer, GameSkin)`. `Bind` keeps the base renderer on `normalCell` and creates one child state-overlay renderer at sorting order `base + 1`. `SetHovered`, `SetValid`, and `SetInvalid` enable `boardStateOverlay` with white/green/red tints; `SetNormal` and `SetOccupied` disable that child without replacing the base sprite. Keep existing curse and enchant overlays. Use `boardFrame` for border bars when present.

In `CreateBattleBackdrop`, resolve `battleBackdrop`. For formal art set `drawMode = SpriteDrawMode.Sliced`, set `size = new Vector2(width, 2.4f)`, keep transform scale at one, and use `new Color(1f, 1f, 1f, 0.82f)`. Fallback retains the existing square sprite, transform scaling, and opaque procedural color.

- [ ] **Step 6: Apply skin to hourglass, timer, and emitter**

Pass `_skin` into `CreateSandClockPanel` and `Emitter.Initialize`.

- `SandClockPanel` uses `hourglassFrame` as a non-raycast overlay while retaining the two existing colored fill Images behind it.
- Enlarge the physical hourglass panel from viewport half-size `(0.045, 0.07)` to `(0.055, 0.09)` while keeping its center fixed on the lane endpoint.
- The separate countdown Text receives a `timerPlaque` non-raycast sibling background, remains actual Unity text, and stays at anchors `(0.27, 0.63)` to `(0.49, 0.71)`.
- `Emitter.EnsureVisual` resolves `emitterBody`; existing pulse and buffer HUD remain unchanged.
- `EnergyBall.EnsureVisual` resolves `energyBall`; movement, lifetime, energy, and collision stay unchanged.
- When a formal sprite is assigned, renderer/Image tint is white except for the existing sand fill and buffer fill.

- [ ] **Step 7: Add asset-presence checks**

After running the builder, the regression runner loads `Resources.Load<GameSkin>("Game/GameSkin")` and requires non-null:

- `normalCell`, `boardStateOverlay`, `boardFrame`, `countdownRingOrnament`, `battleBackdrop`;
- `hourglassFrame`, `timerPlaque`, `panelBackground`, `emitterBody`, `energyBall`.

- [ ] **Step 8: Verify and commit environment art**

Run Node tests, both Unity regression runners, and `git diff --check`. In Game view, verify the ring remains behind board cells and enemies, the timer has no baked text, and the emitter still points right.

```bash
git add Tools/Art/countdown-assets.json Assets/Art/Generated/Countdown Assets/Resources/Game/GameSkin.asset Assets/Scripts/Board/GridBoard.cs Assets/Scripts/Board/Emitter.cs Assets/Scripts/Ball/EnergyBall.cs Assets/Scripts/UI/GridCellView.cs Assets/Scripts/UI/SandClockPanel.cs Assets/Scripts/Game/GameBootstrap.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: add countdown environment art"
```

---

### Task 5: Generate and apply all 17 module skins

**Files:**

- Create: `Assets/Art/Generated/Countdown/Sources/core_module_sheet.png`
- Create: `Assets/Art/Generated/Countdown/Sources/utility_module_sheet.png`
- Create: 17 processed PNGs in `Assets/Art/Generated/Countdown/Modules/`
- Create: `Assets/Scripts/Modules/ModuleSkinApplicator.cs`
- Modify: `Assets/Scripts/UI/PlacementController.cs`
- Modify: `Assets/Scripts/UI/ModuleSlotView.cs`
- Modify: `Assets/Resources/Game/GameSkin.asset`
- Modify: `Assets/Art/Generated/Countdown/ART_PROMPTS.md`
- Test: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Produces: `ModuleSkinApplicator.Apply(ModuleBase module, GameSkin skin): bool`.
- Consumes: `GameSkin.GetModuleBody(ModuleType)` and `GetModuleIcon(ModuleType)`.

- [ ] **Step 1: Generate the refined 4×2 module sheet**

Use:

```text
Production-ready grayscale-tintable 2D tower sprite sheet for a dark brass clockwork alchemy game. Exact 4 columns by 2 rows, eight isolated square-cell assets, identical visual scale and shared octagonal brass base, centered with wide safe margins, no labels, no letters, no numbers, no grid dividers. Row 1: sand-furnace emitter with glass reservoir and right-facing piston nozzle; 90-degree redirector pipe with unmistakable two-way flow; precision laser turret with long focusing lens; squat bomb mortar with pressure chamber. Row 2: snowflake turret with crystalline fork muzzle; black-hole turret with suspended concentric gravity rings; spark turret with twin electrical coils; heatwave turret with circular radiator fins. Neutral silver-gray and pale brass materials designed to accept Unity color tint, only tiny restrained value accents, orthographic top-down 2D sprite, strong readable silhouettes at 64 pixels. Every asset isolated on perfectly flat pure green #00FF00, no cast shadow outside assets, no text, no watermark.
```

Save as `Sources/core_module_sheet.png`.

- [ ] **Step 2: Generate the utility/path 5×2 module sheet**

Use:

```text
Production-ready grayscale-tintable 2D module icon sheet for the same dark brass clockwork alchemy game. Exact 5 columns by 2 rows, ten isolated square-cell assets, identical octagonal brass base and visual scale, centered with wide safe margins, no labels, no letters, no numbers, no grid dividers. Row 1: mining gear-drill with coin stamp; double-flame crucible amplifier; T-junction splitter pipe; paired concentric portal ring; hourglass-plus relay coil. Row 2: double-arrow accelerator turbine; five nodes converging into one fusion core; one core splitting into five fission rays; flame branding-seal enchant module; faceted multicolor surprise prism with an abstract asymmetric spark symbol, not a question mark character. Neutral silver-gray and pale brass designed for Unity color tint, orthographic top-down 2D, clean readable silhouettes at 64 pixels. Every asset isolated on perfectly flat pure green #00FF00, no cast shadows, no text, no watermark.
```

Save as `Sources/utility_module_sheet.png`.

- [ ] **Step 3: Process, inspect, and rebuild the skin**

Remove `allowMissing` from both module manifest entries, run the processor, and inspect all 17 outputs at original detail. Regenerate a sheet once if any cell has merged objects, clipped silhouettes, baked characters, or an incorrect row-major count. Run `CountdownSkinAssetBuilder.Build`.

- [ ] **Step 4: Write a RED applicator check**

Create a `RedirectorModule`, call `ApplyCardData`, assign a test skin whose redirector body is `PrototypeSprites.Circle`, call the missing applicator through reflection, and require its root `SpriteRenderer.sprite` to become the circle.

- [ ] **Step 5: Implement the centralized applicator**

```csharp
public static bool Apply(ModuleBase module, GameSkin skin)
{
    if (module == null || skin == null)
        return false;
    SpriteRenderer body = module.GetComponent<SpriteRenderer>();
    if (body == null)
        return false;
    body.sprite = skin.GetModuleBody(module.ModuleType);
    return body.sprite != null;
}
```

Do not modify individual module classes. Generated sprites remain grayscale-tintable so their existing `RefreshVisual` colors retain functional meaning.

- [ ] **Step 6: Wire placed, fused, and ghost modules**

In `PlacementController.CreateModule(ModuleCardData)`, call the applicator after `ApplyCardData`. In `ShowGhostAt`, call it after the ghost is active and after any ghost `ApplyCardData`, then apply the valid/invalid ghost tint.

`ModuleSlotView` continues using `GetModuleIcon`; set icon color to `Color.Lerp(Color.white, ModuleCatalog.GetDisplayColor(type), 0.72f)` so brass detail remains visible.

- [ ] **Step 7: Verify all module mappings**

The runner must require, for every sellable type:

- the built `GameSkin.asset` body and icon are not fallback sprites;
- `ModuleSkinApplicator.Apply` changes a real module root sprite;
- rotating a redirector changes orientation but not its selected body sprite;
- applying ghost tint does not replace the sprite.

- [ ] **Step 8: Commit module art**

```bash
git add Assets/Art/Generated/Countdown Assets/Resources/Game/GameSkin.asset Assets/Scripts/Modules/ModuleSkinApplicator.cs Assets/Scripts/UI/PlacementController.cs Assets/Scripts/UI/ModuleSlotView.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: skin all board modules"
```

---

### Task 6: Generate clockwork enemies and persistent status overlays

**Files:**

- Create: `Assets/Art/Generated/Countdown/Sources/enemy_sheet.png`
- Create: four processed PNGs in `Assets/Art/Generated/Countdown/Enemies/`
- Create: `Assets/Scripts/Combat/EnemyVisualController.cs`
- Modify: `Assets/Scripts/Combat/Enemy.cs`
- Modify: `Assets/Resources/Game/GameSkin.asset`
- Modify: `Assets/Art/Generated/Countdown/ART_PROMPTS.md`
- Test: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Produces: `EnemyVisualController.Initialize(Enemy, SpriteRenderer, GameSkin)`.
- Produces: `EnemyVisualController.SetStates(bool burning, bool chilled, bool sandBuff)`.
- Exposes for tests: `BurnVisible`, `ChillVisible`, and `SandBuffVisible`.

- [ ] **Step 1: Generate the 2×2 enemy sheet**

Use:

```text
Production-ready grayscale-tintable 2D enemy sprite sheet for a dark clockwork alchemy countdown game. Exact 2 columns by 2 rows, four isolated side-facing clockwork monsters walking toward the left, centered in equal cells, consistent ground line, no labels, no grid dividers. Row 1: tiny fast wind-up clock flea with frantic second-hand legs; standard clockwork time thief with cracked round clock core. Row 2: bulky armored walking clock with a heavy trailing pendulum; enormous ruined walking clock-tower boss with furnace-like core and broken hands. Strong distinct silhouettes, neutral silver-gray and pale brass materials designed to accept yellow/red/blue/orange Unity tint, readable at small size, orthographic 2D game sprite, no animation frames. Perfectly flat pure green #00FF00 background, no cast shadow outside each creature, no text, no watermark.
```

Save as `Sources/enemy_sheet.png`, remove `"allowMissing": true` from the enemy manifest entry, process it, inspect all four silhouettes, and rebuild `GameSkin.asset`.

- [ ] **Step 2: Add RED state-overlay checks**

The runner creates an `EnemyVisualController`, initializes it with a body renderer and fallback skin, calls:

```csharp
controller.SetStates(burning: true, chilled: false, sandBuff: true);
```

It must require `BurnVisible == true`, `ChillVisible == false`, and `SandBuffVisible == true`. Before implementation, invoke by reflection to preserve compilation.

- [ ] **Step 3: Implement persistent overlays**

Create three child renderers:

- `BurnOverlay`: `FlameCrack`, orange-red, sorting order `body + 1`;
- `ChillOverlay`: `FrostRim`, ice blue, sorting order `body + 2`;
- `SandBuffOverlay`: `SandShard`, cyan-white, sorting order `body + 3`.

Each resolves its formal sprite from `GameSkin`; fallback uses `PrototypeSprites.Circle` with restrained alpha. `Update` rotates only the sand overlay, slowly counterclockwise, and pulses burn/chill alpha between `0.45` and `0.75`. It never changes the Enemy transform or hitbox.

- [ ] **Step 4: Integrate with `Enemy`**

In `EnsureVisual`:

```csharp
GameSkin skin = GameSkin.LoadOrCreateRuntime();
_visual.sprite = skin.GetEnemyBody(goldType);
```

Create or reuse `EnemyVisualController`, initialize it, and call one private `RefreshStateVisuals()` from `Initialize`, `ApplySlow`, `ApplyBurn`, `ClearBurn`, `ClearChill`, `ClearBurnAndChill`, and both status-expiry branches.

Retain the current type colors, scale values, movement, HP, and sorting orders.

- [ ] **Step 5: Verify enemy behavior and commit**

The runner must require:

- all four used enemy types map to non-fallback sprites;
- SandBuff combines with burn or chill without hiding the other flag;
- `ClearBurnAndChill` turns off both status overlays while preserving SandBuff;
- Swarm, Normal, Tank, and Boss retain scales `0.4`, `0.9`, `1.25`, and `2.8`.

Run both regression runners, then:

```bash
git add Assets/Art/Generated/Countdown Assets/Resources/Game/GameSkin.asset Assets/Scripts/Combat/EnemyVisualController.cs Assets/Scripts/Combat/Enemy.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: add clockwork enemy skins"
```

---

### Task 7: Generate and integrate bounded combat VFX

**Files:**

- Create: `Assets/Art/Generated/Countdown/Sources/vfx_sheet.png`
- Create: eight processed PNGs in `Assets/Art/Generated/Countdown/VFX/`
- Create: `Assets/Scripts/Combat/CombatVfxService.cs`
- Create: `Assets/Scripts/Combat/TransientSpriteVfx.cs`
- Modify: `Assets/Scripts/Combat/Enemy.cs`
- Modify: `Assets/Scripts/Combat/CombatDamage.cs`
- Modify: `Assets/Scripts/Combat/SandVfxService.cs`
- Modify: `Assets/Scripts/Combat/SandGrainVfx.cs`
- Modify: `Assets/Scripts/Game/GameBootstrap.cs`
- Modify: `Assets/Resources/Game/GameSkin.asset`
- Modify: `Assets/Art/Generated/Countdown/ART_PROMPTS.md`
- Test: `Assets/Editor/CountdownArtRegressionChecks.cs`

**Interfaces:**

- Produces: `CombatVfxService.Initialize(GameSkin, Transform)`.
- Produces: `SpawnHit(Vector3, float)`, `SpawnMelt(Vector3, float)`, and `SpawnDeath(Vector3, EnemyGoldType, float)`.
- Produces: `TransientSpriteVfx.Play(Sprite, Color, float duration, Vector3 startScale, Vector3 endScale, float rotationSpeed, Action onDone)`.
- Exposes for tests: `CombatVfxService.ActiveTransientCount` and constant `MaxTransientCount = 96`.

- [ ] **Step 1: Generate the 4×2 VFX sheet**

Use:

```text
Production-ready 2D VFX sprite sheet for a dark clockwork alchemy countdown game. Exact 4 columns by 2 rows, eight isolated effect sprites centered in equal cells, generous safe margins, no labels, no grid dividers. Row 1: small glowing ember cluster; cracked orange fire-light pattern; icy cyan frost rim with empty center; circular burst combining orange steam and blue ice shards with empty center. Row 2: luminous cyan-white hourglass sand shard; thin brass clock tick ray; small broken brass gear fragment; soft white radial glow disc. Additive-style clean shapes, crisp readable silhouettes, no scene background, no characters, no words, no numbers. Every effect isolated on perfectly flat pure green #00FF00, no watermark.
```

Save as `Sources/vfx_sheet.png`, remove `"allowMissing": true` from the VFX manifest entry, process it, inspect transparency at original detail, and rebuild `GameSkin.asset`.

- [ ] **Step 2: Write RED service-cap checks**

Through reflection, create the service, initialize a test skin whose `ClockTick` entry is `PrototypeSprites.Circle`, and require:

```csharp
Require(service.ActiveTransientCount == 0, "New VFX service must start empty.");
for (int i = 0; i < 120; i++)
    service.SpawnHit(Vector3.zero, 1f);
Require(service.ActiveTransientCount <= 96, "VFX service exceeded its hard cap.");
```

- [ ] **Step 3: Implement `TransientSpriteVfx`**

`Play` assigns a SpriteRenderer at sorting order `45`, then `Update` uses normalized `u`:

```csharp
transform.localScale = Vector3.Lerp(_startScale, _endScale, u);
transform.Rotate(0f, 0f, _rotationSpeed * Time.deltaTime);
Color color = _renderer.color;
color.a = _startAlpha * (1f - u);
_renderer.color = color;
```

At `u >= 1`, invoke `_onDone` exactly once and destroy the GameObject.

- [ ] **Step 4: Implement the bounded service**

`TrySpawn` returns immediately when `_activeTransientCount >= MaxTransientCount`. Otherwise it creates one child under the configured VFX root, increments the count, and passes a completion callback that decrements it with `Mathf.Max(0, count - 1)`.

Use:

- hit: `ClockTick`, white-gold, `0.12s`, scale `0.2 → 0.8`;
- melt: `MeltBurst`, white, `0.35s`, scale `0.35 → 1.35`;
- death: one `GearFragment` plus one `SoftGlow`, `0.3s` for normal enemies and `0.8s` for Boss.

Missing VFX sprites skip that layer and do not consume the cap.

- [ ] **Step 5: Wire gameplay notifications without changing semantics**

- `GameBootstrap` creates one `CombatVfxService` under the existing layout VFX root.
- `Enemy.TakeDamage` calls `SpawnHit` after reducing HP and before `Die`.
- `Enemy.Die` calls `SpawnDeath` before unregistering and destroying the enemy.
- `CombatDamage` calls `SpawnMelt` at the same position and moment as the existing Melt popup.
- `ForceDespawn` and `ReachMage` do not play death effects.
- `SandVfxService.Initialize` receives `GameSkin`, and `SandGrainVfx.Play` receives a Sprite; use `SandShard` with circle fallback.

- [ ] **Step 6: Verify VFX state and cap**

Require:

- all eight VFX sprites load from the built skin;
- 120 hit requests never exceed 96 active transients;
- missing sprites leave gameplay methods safe;
- Melt still clears burn and chill and retains its 1.5 damage multiplier;
- sand return still credits the amount only when the last grain arrives;
- the projectile/enchant regression runner remains green.

- [ ] **Step 7: Commit VFX**

```bash
git add Assets/Art/Generated/Countdown Assets/Resources/Game/GameSkin.asset Assets/Scripts/Combat/CombatVfxService.cs Assets/Scripts/Combat/TransientSpriteVfx.cs Assets/Scripts/Combat/EnemyVisualController.cs Assets/Scripts/Combat/Enemy.cs Assets/Scripts/Combat/CombatDamage.cs Assets/Scripts/Combat/SandVfxService.cs Assets/Scripts/Combat/SandGrainVfx.cs Assets/Scripts/Game/GameBootstrap.cs Assets/Editor/CountdownArtRegressionChecks.cs
git commit -m "feat: add countdown combat effects"
```

---

### Task 8: Final visual QA, documentation, and scope audit

**Files:**

- Modify: `Assets/Resources/Game/README.md`
- Modify: `docs/agent-handoff/04-systems-modules.md`
- Modify: `docs/agent-handoff/05-systems-combat.md`
- Modify: `docs/agent-handoff/09-ui-layout.md`
- Reference: `docs/superpowers/specs/2026-07-26-countdown-art-direction-design.md`

**Interfaces:**

- Documents final resource regeneration, sorting, fallback, and player-visible art behavior.

- [ ] **Step 1: Update resource documentation**

Document these commands verbatim:

```bash
NODE_PATH="/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules" \
  "/Users/zihanwang/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node" \
  Tools/Art/process-countdown-assets.mjs \
  --manifest Tools/Art/countdown-assets.json

"/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -quit \
  -projectPath "$(pwd)" \
  -executeMethod CountdownSkinAssetBuilder.Build \
  -logFile "Logs/countdown-skin-build.log"
```

State that every missing field falls back independently.

- [ ] **Step 2: Update handoff documentation**

- Module doc: `ModuleType → GameSkin → ModuleSkinApplicator/ModuleSlotView`.
- Combat doc: Swarm/Normal/Tank/Boss silhouettes, persistent burn/chill/sand overlays, Melt burst, and 96 transient cap.
- UI layout doc: 60-tick ring center viewport `(0.38, 0.53)`, radius `0.27`, sorting order `-1`, board cells at `0+`, enemies at `10+`, and non-raycast behavior.

- [ ] **Step 3: Run all automated checks from a fresh copy**

```bash
COUNTDOWN_VERIFY="$(mktemp -d)"
rsync -a \
  --exclude '.git' --exclude '.codex' --exclude '.superpowers' \
  --exclude 'Library' --exclude 'Temp' --exclude 'Logs' --exclude 'obj' \
  ./ "$COUNTDOWN_VERIFY/"

/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$COUNTDOWN_VERIFY" \
  -executeMethod CountdownSkinAssetBuilder.Build \
  -logFile "$COUNTDOWN_VERIFY/countdown-skin-build.log"

/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$COUNTDOWN_VERIFY" \
  -executeMethod CountdownArtRegressionChecks.Run \
  -logFile "$COUNTDOWN_VERIFY/countdown-art-final.log"

/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$COUNTDOWN_VERIFY" \
  -executeMethod GmtkBugfixRegressionChecks.Run \
  -logFile "$COUNTDOWN_VERIFY/gameplay-regression-final.log"
```

Expected: all three Unity commands exit `0`; both regression logs contain their PASS markers and contain no `error CS`, `Unhandled Exception`, or assertion failure.

- [ ] **Step 4: Perform exact visual checks**

Run the game at 1920×1080 and 1280×720 and capture one screenshot of each. Verify:

1. the time ring is the first background motif noticed but does not hide board cells;
2. `00:20.000` and lower changes ring, timer, and hourglass to danger red;
3. the ring remains at 60 lit ticks above 100,000 ms and shows one gain pulse;
4. all 17 module icons have distinct silhouettes;
5. Swarm, Normal, Tank, and Boss are recognizable without their colors;
6. burn, chill, and sand buff remain distinguishable when two are active;
7. Melt is brighter than persistent states and ends within 0.35 seconds;
8. a dense wave does not obscure projectile paths or block board dragging.

- [ ] **Step 5: Audit repository scope**

```bash
git diff --check
git status --short
git diff --name-only 0586d27..HEAD
```

Require that no commit contains:

- `Packages/manifest.json`;
- `Packages/packages-lock.json`;
- `ProjectSettings/`;
- `.superpowers/`;
- the user's `MinerModule.cs` entity-ID change unless it was already committed separately by the user.

- [ ] **Step 6: Commit documentation**

```bash
git add Assets/Resources/Game/README.md docs/agent-handoff/04-systems-modules.md docs/agent-handoff/05-systems-combat.md docs/agent-handoff/09-ui-layout.md
git commit -m "docs: document countdown art system"
```

- [ ] **Step 7: Final verification report**

Record:

- branch name and commit range;
- Node test pass count;
- both Unity PASS markers;
- screenshot resolutions;
- any generated asset that used a fallback;
- confirmation that unrelated dirty files were not staged.

Do not push or open a pull request unless the user requests it.
