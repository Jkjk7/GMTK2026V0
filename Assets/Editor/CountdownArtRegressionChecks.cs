using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Batch-mode checks for the procedural countdown art fallback.</summary>
public static class CountdownArtRegressionChecks
{
    public static void Run()
    {
        try
        {
            Require(CountdownVisualRules.TickCount == 60, "Countdown ring must contain exactly 60 ticks.");
            Require(CountdownVisualRules.GetLitTickCount(-1, 100) == 0, "Negative ratios must clamp empty.");
            Require(CountdownVisualRules.GetLitTickCount(250, 100) == 60, "Overfill ratios must clamp full.");
            Require(CountdownVisualRules.GetLitTickCount(50, 100) == 30, "Half time must light 30 ticks.");
            Require(CountdownVisualRules.IsWarning(20_000), "20,000 ms must enter warning state.");
            Require(!CountdownVisualRules.IsWarning(20_001), "20,001 ms must remain normal.");

            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                Require(ModuleSkinApplicator.HasStyle(type), $"Missing module visual style for {type}.");
                Sprite direct = CountdownArtResources.LoadModuleSprite(type);
                Sprite skinIcon = GameSkin.LoadOrCreateRuntime().GetModuleIcon(type);
                Require(direct != null, $"Missing runtime module sprite for {type}.");
                Require(direct != PrototypeSprites.Square, $"{type} still uses the square fallback.");
                Require(skinIcon == direct, $"{type} does not use the shared module sprite.");
            }

            VisibleCountdownResourcesExist();
            EnemyTypesHaveDistinctCountdownSilhouettes();
            EnemyStatusVisualsFollowAndClear();
            CombatAccentsAreBounded();
            SharedModuleIconVisuals();
            PlacedModuleUsesFormalSkin();
            RingAndHourglassUseFormalArt();

            Require(SandClock.InitialSandMs == 100_000, "Initial sand gameplay constant changed.");
            Require(SandClock.BreachPenaltySwarmMs == 3_000, "Swarm penalty gameplay constant changed.");
            Require(SandClock.BreachPenaltyNormalMs == 10_000, "Normal penalty gameplay constant changed.");
            Require(SandClock.BreachPenaltyTankMs == 30_000, "Tank penalty gameplay constant changed.");
            Debug.Log("[Countdown Art Regression] PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    static void EnemyStatusVisualsFollowAndClear()
    {
        var go = new GameObject("EnemyVisualRegression");
        try
        {
            Enemy enemy = go.AddComponent<Enemy>();
            enemy.Initialize(null, null, null, 1, EnemyGoldType.Normal, true);
            EnemyVisualController visuals = go.GetComponent<EnemyVisualController>();
            Require(visuals != null && visuals.HasClockworkCore, "Enemy clockwork core/hand missing.");
            Require(visuals.SandVisible, "Sand-buffed enemy is missing its gold orbit.");

            enemy.ApplyBurn(1f);
            visuals.Refresh();
            Require(visuals.BurnVisible, "Burn overlay did not follow active burn.");
            enemy.ClearBurn();
            visuals.Refresh();
            Require(!visuals.BurnVisible, "Burn overlay did not clear with burn.");

            enemy.ApplySlow(0.25f, 1f);
            visuals.Refresh();
            Require(visuals.ChillVisible, "Chill rim did not follow active chill.");
            enemy.ClearChill();
            visuals.Refresh();
            Require(!visuals.ChillVisible, "Chill rim did not clear with chill.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void EnemyTypesHaveDistinctCountdownSilhouettes()
    {
        foreach (EnemyGoldType type in Enum.GetValues(typeof(EnemyGoldType)))
        {
            var go = new GameObject($"EnemyVisual_{type}");
            try
            {
                Enemy enemy = go.AddComponent<Enemy>();
                enemy.Initialize(null, null, null, 1, type);
                Require(
                    go.transform.Find($"EnemySilhouette/{type}Marker") != null,
                    $"{type} enemy is missing its distinct countdown silhouette.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    static void VisibleCountdownResourcesExist()
    {
        Require(
            Resources.LoadAll<Sprite>("Countdown/board_cell").Length > 0,
            "Generated board-cell art is not loadable at runtime.");
        Require(
            Resources.LoadAll<Sprite>("Countdown/battle_lane_backdrop").Length > 0,
            "Generated battle-lane art is not loadable at runtime.");
    }

    static void CombatAccentsAreBounded()
    {
        TransientSpriteVfx hit = CombatVfxService.SpawnHit(Vector3.zero);
        TransientSpriteVfx melt = CombatVfxService.SpawnMelt(Vector3.zero);
        TransientSpriteVfx death = CombatVfxService.SpawnDeath(Vector3.zero);
        try
        {
            Require(hit.LifetimeSeconds <= 1f, "Hit accent is not bounded.");
            Require(melt.LifetimeSeconds <= 1f, "Melt accent is not bounded.");
            Require(death.LifetimeSeconds <= 1f, "Death accent is not bounded.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hit.gameObject);
            UnityEngine.Object.DestroyImmediate(melt.gameObject);
            UnityEngine.Object.DestroyImmediate(death.gameObject);
        }
    }

    static void SharedModuleIconVisuals()
    {
        var go = new GameObject("ModuleIconVisualRegression");
        try
        {
            Image image = go.AddComponent<Image>();
            ModuleIconVisuals.Apply(image, ModuleType.Spark);
            Require(
                image.sprite == CountdownArtResources.LoadModuleSprite(ModuleType.Spark),
                "UI icon did not use the formal Spark sprite.");
            Require(image.color == Color.white, "Formal module icon received a flat gameplay tint.");
            Require(image.preserveAspect, "Formal module icon must preserve its aspect ratio.");

            ModuleIconVisuals.Apply(image, ModuleType.Spark, true);
            Require(
                Mathf.Approximately(image.color.a, ModuleIconVisuals.DisabledAlpha),
                "Disabled module icon alpha is inconsistent.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void PlacedModuleUsesFormalSkin()
    {
        var go = new GameObject("PlacedModuleSkinRegression");
        try
        {
            SparkModule module = go.AddComponent<SparkModule>();
            module.ApplyCardData(ModuleCardData.Create(ModuleType.Spark, 1, 0));
            bool applied = ModuleSkinApplicator.Apply(module);
            SpriteRenderer body = module.GetComponent<SpriteRenderer>();
            Require(applied, "Spark formal skin was not applied.");
            Require(
                body != null &&
                body.sprite == CountdownArtResources.LoadModuleSprite(ModuleType.Spark),
                "Placed Spark retained its prototype body.");
            Require(
                module.transform.Find("CountdownClockwork") == null,
                "Old decorative clock face is still stacked over the module body.");

            module.RefreshVisual();
            ModuleSkinApplicator controller = module.GetComponent<ModuleSkinApplicator>();
            controller.RefreshNow();
            Require(
                body.sprite == CountdownArtResources.LoadModuleSprite(ModuleType.Spark),
                "A gameplay refresh replaced the formal module sprite.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void RingAndHourglassUseFormalArt()
    {
        Require(
            Resources.Load<Texture2D>(CountdownArtResources.RingOrnamentPath) != null,
            "Countdown ring ornament is not runtime-loadable.");
        Require(
            Resources.Load<Texture2D>(CountdownArtResources.HourglassFramePath) != null,
            "Hourglass frame is not runtime-loadable.");

        var go = new GameObject("CountdownRingHierarchyRegression");
        try
        {
            CountdownRingView ring = go.AddComponent<CountdownRingView>();
            ring.Initialize(null, Vector3.zero, 4f);
            Require(ring.TickRendererCount == 60, "Countdown ring lost its sixty ticks.");
            Require(ring.OrnamentRenderer != null, "Countdown ring ornament is missing.");
            Require(ring.MaxSortingOrder < 0, "Countdown ring must remain below gameplay.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
