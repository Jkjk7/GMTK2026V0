# Projectile Retarget and Enchant Seed Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make snowflake and ember projectiles reuse otherwise-lost damage by retargeting the nearest living enemy, and make each enchant module keep one board-position-independent layout throughout its lifetime.

**Architecture:** Keep projectile recovery inside `ArcSparkProjectile`, because both affected towers already share it. Persist an enchant-only `InstanceSeed` in `ModuleCardData`, then make `EnchantSeedUtil` ignore placement coordinates while retaining its existing public signatures to minimize call-site churn and allow a direct position-independence regression check.

**Tech Stack:** Unity 6000.5.4f1, C#, Unity Editor batch mode, existing runtime bootstrap architecture.

## Global Constraints

- Preserve the existing uncommitted random visual phase change in `Assets/Scripts/Combat/ArcSparkProjectile.cs`.
- Do not touch the unrelated dirty changes in `MinerModule.cs`, `Packages/`, or `ProjectSettings/`.
- Do not change bomb, black-hole, laser, or heatwave targeting.
- Do not change projectile damage, status effects, speed, homing strength, or 1.6-second lifetime.
- Keep curse and locked-cell skipping behavior unchanged.
- Do not create a Git commit unless the user explicitly requests one.

---

## File Map

- Create `Assets/Editor/GmtkBugfixRegressionChecks.cs`: executable editor regression checks for retargeting and enchant stability.
- Modify `Assets/Scripts/Combat/ArcSparkProjectile.cs`: select the nearest living enemy when the current target is invalid.
- Modify `Assets/Scripts/Economy/ModuleCardData.cs`: persist an enchant module instance seed through card movement and fusion.
- Modify `Assets/Scripts/Modules/FireEnchantModule.cs`: source the seed from card data and remove origin coordinates from random hashes.
- Modify `Assets/Scripts/Modules/SurpriseModule.cs`: use the persisted seed for deterministic enchant kinds.
- Modify `docs/agent-handoff/04-systems-modules.md`: document stable, card-persisted enchant seeds.
- Modify `docs/agent-handoff/05-systems-combat.md`: document projectile retargeting.

### Task 1: Establish failing regression checks

**Files:**

- Create: `Assets/Editor/GmtkBugfixRegressionChecks.cs`

**Interfaces:**

- Consumes: existing `ArcSparkProjectile.Spawn`, `Enemy.Initialize`, `Enemy.TakeDamage`, `EnchantSeedUtil.BuildTargets`, `ModuleCardData.Create`, and `ModuleCardData.FusedWith`.
- Produces: `GmtkBugfixRegressionChecks.Run()` callable with Unity `-executeMethod`.

- [ ] **Step 1: Write a real-behavior editor regression runner**

Create an editor-only runner with these checks:

```csharp
public static void Run()
{
    try
    {
        RetargetsNearestLivingEnemy();
        RetargetKeepsBurnEffect();
        RetargetKeepsChillEffect();
        NoLivingTargetRemainsSafe();
        EnchantLayoutIgnoresPlacementCell();
        EnchantFusionKeepsTargetSeed();
        Debug.Log("[GMTK Regression] PASS");
        EditorApplication.Exit(0);
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
        EditorApplication.Exit(1);
    }
}
```

`RetargetsNearestLivingEnemy` must create a projectile at `(0,0)`, kill its original target at `(5,0)`, leave living enemies at `(1,0)` and `(3,0)`, invoke the projectile's current update once through reflection, and assert its private `_target` is the enemy at `(1,0)`.

`RetargetKeepsBurnEffect` must put a living receiver within the ember projectile hit radius, kill the original target, invoke one update, and assert the receiver lost exactly one HP and became burning.

`RetargetKeepsChillEffect` must repeat the receiver scenario with a snowflake projectile and assert the receiver lost the configured damage and became chilled.

`NoLivingTargetRemainsSafe` must kill the only enemy, invoke one projectile update, and assert no exception, no hit, and a null `_target`; this protects the existing straight-flight fallback until normal lifetime cleanup.

`EnchantLayoutIgnoresPlacementCell` must initialize a real `GridBoard`, call the existing `BuildTargets` overload with the same type, seed, and level but origins `(0,0)`, `(3,3)`, and `(6,6)`, and assert identical ordered coordinate lists whose coordinates all remain in the 7×7 bounds.

`EnchantFusionKeepsTargetSeed` must use reflection to require a public `InstanceSeed` field on `ModuleCardData`, assert newly created `FireEnchant` cards receive nonzero seeds, fuse two cards, and assert the result retains the target card's seed.

- [ ] **Step 2: Copy the project to a temporary verification directory**

The main Unity Editor currently has this project open, so avoid its project lock:

```bash
VERIFY_PROJECT="$(mktemp -d)"
rsync -a \
  --exclude '.git' \
  --exclude '.codex' \
  --exclude 'Library' \
  --exclude 'Temp' \
  --exclude 'Logs' \
  --exclude 'obj' \
  ./ "$VERIFY_PROJECT/"
```

- [ ] **Step 3: Run the regression runner and verify RED**

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -projectPath "$VERIFY_PROJECT" \
  -executeMethod GmtkBugfixRegressionChecks.Run \
  -logFile "$VERIFY_PROJECT/unity-regression-red.log"
```

Expected: nonzero exit status. The log must show at least the retarget failure (`_target` is null instead of the nearest living enemy) and, after running checks independently if necessary, the enchant position or missing `InstanceSeed` failure. Compilation errors are not an acceptable RED state.

- [ ] **Step 4: Review scope without committing**

Run `git diff -- Assets/Editor/GmtkBugfixRegressionChecks.cs` and verify the only new production-independent artifact is the editor regression runner.

### Task 2: Retarget invalid arc-projectile targets

**Files:**

- Modify: `Assets/Scripts/Combat/ArcSparkProjectile.cs`
- Test: `Assets/Editor/GmtkBugfixRegressionChecks.cs`

**Interfaces:**

- Consumes: scene `Enemy` objects and `Enemy.IsAlive`.
- Produces: private `Enemy FindNearestAliveEnemy(Vector3 origin)` and updated `Update()` retarget behavior.

- [ ] **Step 1: Replace target clearing with recovery**

At the start of projectile movement, normalize the current position and use:

```csharp
if (_target == null || !_target.IsAlive)
{
    _target = FindNearestAliveEnemy(pos);
}
```

- [ ] **Step 2: Add the nearest-living selector**

```csharp
Enemy FindNearestAliveEnemy(Vector3 origin)
{
    Enemy[] enemies = FindObjectsOfType<Enemy>();
    Enemy best = null;
    float bestDistanceSq = float.PositiveInfinity;
    for (int i = 0; i < enemies.Length; i++)
    {
        Enemy enemy = enemies[i];
        if (enemy == null || !enemy.IsAlive)
        {
            continue;
        }

        Vector3 delta = enemy.transform.position - origin;
        delta.z = 0f;
        float distanceSq = delta.sqrMagnitude;
        if (distanceSq < bestDistanceSq)
        {
            bestDistanceSq = distanceSq;
            best = enemy;
        }
    }

    return best;
}
```

Do not alter the existing no-target drift branch, swept collision, impact effects, or user-authored `_randomPhase` visual code.

- [ ] **Step 3: Recopy the changed files and verify the projectile checks GREEN**

Refresh the temporary project copy or create a fresh one, then rerun `GmtkBugfixRegressionChecks.Run`. At this checkpoint, retarget checks must pass while the enchant checks remain the expected failures.

- [ ] **Step 4: Review scope without committing**

Run:

```bash
git diff -- Assets/Scripts/Combat/ArcSparkProjectile.cs Assets/Editor/GmtkBugfixRegressionChecks.cs
```

Confirm the pre-existing random phase change remains present and untouched.

### Task 3: Persist enchant seeds and remove placement-position dependence

**Files:**

- Modify: `Assets/Scripts/Economy/ModuleCardData.cs`
- Modify: `Assets/Scripts/Modules/FireEnchantModule.cs`
- Modify: `Assets/Scripts/Modules/SurpriseModule.cs`
- Test: `Assets/Editor/GmtkBugfixRegressionChecks.cs`

**Interfaces:**

- Produces: `ModuleCardData.InstanceSeed`.
- Produces: `ModuleCardData.Create(ModuleType type, int level, int investedGold, bool bent = false, int instanceSeed = 0)`.
- Retains: `EnchantSeedUtil.BuildTargets(GridBoard board, GridCoord origin, ModuleType type, int instanceSeed, int level)`; `origin` remains for compatibility but is not hashed.
- Retains: `EnchantSeedUtil.RollKind(GridCoord origin, ModuleType type, int instanceSeed, int index)`; `origin` remains for compatibility but is not hashed.

- [ ] **Step 1: Add and normalize `InstanceSeed`**

Add the public serialized struct field:

```csharp
/// <summary>附魔模块实例种子；随卡牌移动和合成保留。</summary>
public int InstanceSeed;
```

Extend `Create` with the optional `instanceSeed` parameter. For `FireEnchant` and `Surprise`, preserve a nonzero supplied seed or generate `Random.Range(1, int.MaxValue)`; for other module types, store `0`.

- [ ] **Step 2: Preserve the target card seed during fusion**

For same-type upgrades, return:

```csharp
return Create(
    Type,
    Level + 1,
    InvestedGold + other.InvestedGold,
    Bent,
    InstanceSeed);
```

For redirector/function fusion, forward `func.InstanceSeed` even though current bendable module types normalize it to zero.

- [ ] **Step 3: Make `FireEnchantModule` use card data as the single seed source**

Remove the component-local `salt` field and `EnsureSalt`. Replace calls with `CardData.InstanceSeed`, including the protected property used by `SurpriseModule`:

```csharp
protected int InstanceSeed => CardData.InstanceSeed;
```

`BindToCell` continues to call `base.BindToCell` before `ReapplyEnchants`, so default card data is normalized before target generation.

- [ ] **Step 4: Stop hashing placement coordinates**

Keep the existing method signatures but change the hashes to:

```csharp
int seed = Hash((int)type, instanceSeed, step);
```

and:

```csharp
int seed = Hash((int)type, instanceSeed, 100 + index);
```

Replace the five-argument hash helper with a three-argument helper. The `origin` parameters are deliberately unused compatibility parameters and must be named `_` or documented to avoid implying they affect behavior.

- [ ] **Step 5: Update `SurpriseModule`**

Use the persisted card seed:

```csharp
return EnchantSeedUtil.RollKind(Cell, ModuleType, InstanceSeed, index);
```

- [ ] **Step 6: Run the full regression runner and verify GREEN**

Run the Unity batch command against a fresh temporary copy. Expected exit status: `0`; expected log marker: `[GMTK Regression] PASS`.

- [ ] **Step 7: Review scope without committing**

Run:

```bash
git diff --check
git diff -- \
  Assets/Scripts/Economy/ModuleCardData.cs \
  Assets/Scripts/Modules/FireEnchantModule.cs \
  Assets/Scripts/Modules/SurpriseModule.cs \
  Assets/Editor/GmtkBugfixRegressionChecks.cs
```

### Task 4: Synchronize handoff documentation and perform final verification

**Files:**

- Modify: `docs/agent-handoff/04-systems-modules.md`
- Modify: `docs/agent-handoff/05-systems-combat.md`
- Reference: `docs/superpowers/specs/2026-07-26-projectile-retarget-enchant-seed-design.md`

**Interfaces:**

- Documents the final player-visible and data-lifecycle behavior.

- [ ] **Step 1: Update module documentation**

Replace the current seed description with:

```markdown
- 每个火附魔/惊喜卡在 `ModuleCardData.InstanceSeed` 保存独立种子。
- 目标格只由模块类型 + 实例种子 + 等级步进决定，与模块放置格无关。
- 移动、回手牌再放置与升级均保留种子；升级确定性增加 1 格。
- 诅咒/不可建造格跳过且不补抽；目标坐标始终位于 7×7 棋盘内。
```

- [ ] **Step 2: Update combat documentation**

Add to the snowflake/fire targeting description:

```markdown
- 弧线雪花/火花弹的首要目标死亡后，会在剩余寿命内重新锁定距离弹丸最近的存活敌人。
```

- [ ] **Step 3: Run final automated verification**

Run a fresh temporary-copy Unity regression pass and retain the exit code plus `[GMTK Regression] PASS` log evidence.

- [ ] **Step 4: Check compilation and unexpected errors**

Inspect the temporary Unity log for `error CS`, `Unhandled Exception`, and failed assertions. Expected: none.

- [ ] **Step 5: Inspect final diff and working tree**

```bash
git diff --check
git status --short
git diff -- \
  Assets/Editor/GmtkBugfixRegressionChecks.cs \
  Assets/Scripts/Combat/ArcSparkProjectile.cs \
  Assets/Scripts/Economy/ModuleCardData.cs \
  Assets/Scripts/Modules/FireEnchantModule.cs \
  Assets/Scripts/Modules/SurpriseModule.cs \
  docs/agent-handoff/04-systems-modules.md \
  docs/agent-handoff/05-systems-combat.md \
  docs/superpowers/specs/2026-07-26-projectile-retarget-enchant-seed-design.md \
  docs/superpowers/plans/2026-07-26-projectile-retarget-enchant-seed.md
```

Confirm unrelated dirty files are unchanged and do not commit.
