using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 可由 Unity -executeMethod 调用的轻量回归检查。
/// 项目没有独立 runtime asmdef，因此用 Editor 入口直接验证真实玩法组件。
/// </summary>
public static class GmtkBugfixRegressionChecks
{
    const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

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

    static void RetargetsNearestLivingEnemy()
    {
        var created = new List<GameObject>();
        try
        {
            Enemy original = CreateEnemy("Original", new Vector3(5f, 0f, 0f), created);
            Enemy nearest = CreateEnemy("Nearest", new Vector3(1f, 0f, 0f), created);
            CreateEnemy("Farther", new Vector3(3f, 0f, 0f), created);
            SetEnemyAlive(original, false);

            ArcSparkProjectile bolt = ArcSparkProjectile.Spawn(
                Vector3.zero,
                original,
                null,
                1,
                CombatDamage.HitEffects.None,
                ArcSparkProjectile.Style.Ember);
            created.Add(bolt.gameObject);

            InvokeProjectileUpdate(bolt);

            Enemy actual = GetPrivateField<Enemy>(bolt, "_target");
            Require(
                actual == nearest,
                "Arc projectile did not retarget the nearest living enemy after its original target died.");
        }
        finally
        {
            DestroyAll(created);
        }
    }

    static void RetargetKeepsBurnEffect()
    {
        var created = new List<GameObject>();
        try
        {
            Enemy original = CreateEnemy("BurnOriginal", new Vector3(5f, 0f, 0f), created);
            Enemy receiver = CreateEnemy("BurnReceiver", Vector3.zero, created);
            SetEnemyAlive(original, false);
            int hpBefore = receiver.CurrentHitPoints;

            ArcSparkProjectile bolt = ArcSparkProjectile.Spawn(
                Vector3.zero,
                original,
                null,
                1,
                CombatDamage.HitEffects.Burn(2f),
                ArcSparkProjectile.Style.Ember);
            created.Add(bolt.gameObject);

            InvokeProjectileUpdate(bolt);

            Require(
                receiver.CurrentHitPoints == hpBefore - 1,
                "Retargeted ember did not apply its configured damage.");
            Require(receiver.IsBurning, "Retargeted ember lost its burn effect.");
        }
        finally
        {
            DestroyAll(created);
        }
    }

    static void RetargetKeepsChillEffect()
    {
        var created = new List<GameObject>();
        try
        {
            Enemy original = CreateEnemy("ChillOriginal", new Vector3(5f, 0f, 0f), created);
            Enemy receiver = CreateEnemy("ChillReceiver", Vector3.zero, created);
            SetEnemyAlive(original, false);
            int hpBefore = receiver.CurrentHitPoints;

            ArcSparkProjectile bolt = ArcSparkProjectile.Spawn(
                Vector3.zero,
                original,
                null,
                5,
                CombatDamage.HitEffects.Chill(2f, 0.3f),
                ArcSparkProjectile.Style.Snowflake);
            created.Add(bolt.gameObject);

            InvokeProjectileUpdate(bolt);

            Require(
                receiver.CurrentHitPoints == hpBefore - 5,
                "Retargeted snowflake did not apply its configured damage.");
            Require(receiver.IsChilled, "Retargeted snowflake lost its chill effect.");
        }
        finally
        {
            DestroyAll(created);
        }
    }

    static void NoLivingTargetRemainsSafe()
    {
        var created = new List<GameObject>();
        try
        {
            Enemy original = CreateEnemy("OnlyTarget", new Vector3(5f, 0f, 0f), created);
            SetEnemyAlive(original, false);

            ArcSparkProjectile bolt = ArcSparkProjectile.Spawn(
                Vector3.zero,
                original,
                null,
                1,
                CombatDamage.HitEffects.None,
                ArcSparkProjectile.Style.Ember);
            created.Add(bolt.gameObject);

            InvokeProjectileUpdate(bolt);

            Require(GetPrivateField<Enemy>(bolt, "_target") == null, "Dead enemy remained targeted.");
            Require(!GetPrivateField<bool>(bolt, "_hit"), "Projectile registered a hit without a living target.");
        }
        finally
        {
            DestroyAll(created);
        }
    }

    static void EnchantLayoutIgnoresPlacementCell()
    {
        var boardGo = new GameObject("RegressionBoard");
        try
        {
            GridBoard board = boardGo.AddComponent<GridBoard>();
            board.Initialize(1f);
            const int seed = 24681357;
            const int level = 4;

            List<GridCoord> expected = EnchantSeedUtil.BuildTargets(
                board,
                new GridCoord(0, 0),
                ModuleType.FireEnchant,
                seed,
                level);
            GridCoord[] origins =
            {
                new GridCoord(3, 3),
                new GridCoord(6, 6)
            };

            for (int i = 0; i < origins.Length; i++)
            {
                List<GridCoord> actual = EnchantSeedUtil.BuildTargets(
                    board,
                    origins[i],
                    ModuleType.FireEnchant,
                    seed,
                    level);
                Require(
                    SameCoords(expected, actual),
                    $"Enchant layout changed when module origin moved to {origins[i]}.");
            }

            for (int i = 0; i < expected.Count; i++)
            {
                GridCoord coord = expected[i];
                Require(
                    coord.Col >= 0 && coord.Col < GridBoard.Width &&
                    coord.Row >= 0 && coord.Row < GridBoard.Height,
                    $"Enchant target {coord} is outside the 7x7 board.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(boardGo);
        }
    }

    static void EnchantFusionKeepsTargetSeed()
    {
        FieldInfo seedField = typeof(ModuleCardData).GetField("InstanceSeed");
        Require(seedField != null, "ModuleCardData is missing the persistent InstanceSeed field.");

        ModuleCardData target = ModuleCardData.Create(ModuleType.FireEnchant, 1, 10);
        ModuleCardData incoming = ModuleCardData.Create(ModuleType.FireEnchant, 1, 10);
        int targetSeed = (int)seedField.GetValue(target);
        int incomingSeed = (int)seedField.GetValue(incoming);
        Require(targetSeed != 0, "New FireEnchant card received a zero instance seed.");
        Require(incomingSeed != 0, "Second FireEnchant card received a zero instance seed.");

        ModuleCardData fused = target.FusedWith(incoming);
        int fusedSeed = (int)seedField.GetValue(fused);
        Require(fusedSeed == targetSeed, "Enchant fusion did not retain the target card's seed.");
    }

    static Enemy CreateEnemy(string name, Vector3 position, List<GameObject> created)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        Enemy enemy = go.AddComponent<Enemy>();
        enemy.Initialize(null, null, null, 1);
        created.Add(go);
        return enemy;
    }

    static void SetEnemyAlive(Enemy enemy, bool alive)
    {
        FieldInfo field = typeof(Enemy).GetField("_alive", InstancePrivate);
        Require(field != null, "Enemy._alive field was not found.");
        field.SetValue(enemy, alive);
    }

    static void InvokeProjectileUpdate(ArcSparkProjectile bolt)
    {
        MethodInfo update = typeof(ArcSparkProjectile).GetMethod("Update", InstancePrivate);
        Require(update != null, "ArcSparkProjectile.Update was not found.");
        update.Invoke(bolt, null);
    }

    static T GetPrivateField<T>(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(name, InstancePrivate);
        Require(field != null, $"{instance.GetType().Name}.{name} was not found.");
        return (T)field.GetValue(instance);
    }

    static bool SameCoords(List<GridCoord> a, List<GridCoord> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    static void DestroyAll(List<GameObject> objects)
    {
        for (int i = objects.Count - 1; i >= 0; i--)
        {
            if (objects[i] != null)
            {
                UnityEngine.Object.DestroyImmediate(objects[i]);
            }
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
