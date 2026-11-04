using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Run with -executeMethod PixelVfxRegressionChecks.Run.</summary>
public static class PixelVfxRegressionChecks
{
    public static void Run()
    {
        try
        {
            ReusedAccentStartsFresh();
            AccentBurstStaysBounded();
            ShieldBurstKeepsRandomSequence();
            PixelTexturesHaveCrispEdges();
            Debug.Log("[Pixel VFX Regression] PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static void ReusedAccentStartsFresh()
    {
        TransientSpriteVfx first = CombatVfxService.SpawnHit(Vector3.zero);
        GameObject root = first.transform.parent.gameObject;
        try
        {
            TransientSpriteVfx freshMelt = CombatVfxService.SpawnMelt(Vector3.left);
            Color freshMeltColor = freshMelt.GetComponent<SpriteRenderer>().color;
            // A reused hit must receive the Melt palette, even after its previous tint has faded.
            first.GetComponent<SpriteRenderer>().color = new Color(0.1f, 0.2f, 0.3f, 0f);
            typeof(TransientSpriteVfx).GetField("_age", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(first, 5f);
            first.transform.rotation = Quaternion.Euler(0f, 0f, 123f);
            first.gameObject.SetActive(false);
            // Explicitly drive the callback in EditMode, where normal player updates are suspended.
            typeof(TransientSpriteVfx).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(first, null);
            TransientSpriteVfx reused = CombatVfxService.SpawnMelt(Vector3.one);
            Require(first == reused, "Expired accents must be reusable.");
            Require(reused.GetComponents<SpriteRenderer>().Length == 1, "Reuse must not add renderers.");
            Require(reused.transform.rotation == Quaternion.identity, "Reuse must reset rotation.");
            Require(reused.transform.position == Vector3.one, "Reuse must reset position.");
            Require((float)typeof(TransientSpriteVfx).GetField("_age", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(reused) == 0f, "Reuse must reset age.");
            Require(reused.GetComponent<SpriteRenderer>().color == freshMeltColor,
                "Reuse must restore the same color and opacity as a fresh Melt accent.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void AccentBurstStaysBounded()
    {
        TransientSpriteVfx effect = CombatVfxService.SpawnHit(Vector3.zero);
        GameObject root = effect.transform.parent.gameObject;
        try
        {
            var instances = new HashSet<int>();
            instances.Add(effect.GetInstanceID());
            for (int i = 0; i < 300; i++)
            {
                effect = CombatVfxService.SpawnHit(Vector3.zero);
                Require(effect != null && effect.gameObject.activeSelf, "SpawnHit must return a live accent.");
                instances.Add(effect.GetInstanceID());
            }
            Require(instances.Count <= 96, "Cosmetic burst must not create unbounded accent objects.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ShieldBurstKeepsRandomSequence()
    {
        UnityEngine.Random.State original = UnityEngine.Random.state;
        GameObject root = null;
        try
        {
            UnityEngine.Random.InitState(1926);
            for (int spawn = 0; spawn < 20; spawn++)
                for (int shard = 0; shard < 8; shard++)
                {
                    UnityEngine.Random.Range(-0.2f, 0.2f);
                    UnityEngine.Random.Range(0.18f, 0.32f);
                    UnityEngine.Random.Range(0.55f, 1f);
                    UnityEngine.Random.Range(0f, 360f);
                    UnityEngine.Random.Range(2.2f, 4.2f);
                    UnityEngine.Random.Range(-420f, 420f);
                }
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(1926);
            for (int i = 0; i < 20; i++) CombatVfxService.SpawnShieldShatter(Vector3.zero);
            float actual = UnityEngine.Random.value;
            ShieldShatterVfx[] shields = UnityEngine.Object.FindObjectsByType<ShieldShatterVfx>(FindObjectsSortMode.None);
            Require(shields.Length > 0 && shields.Length <= 12, "Shield accents must remain bounded.");
            root = shields[0].transform.parent.gameObject;
            Require(expected == actual, "Cosmetic pooling must preserve gameplay random sequence.");
            foreach (ShieldShatterVfx shield in shields)
                Require(shield.transform.childCount == 9, "Shield reuse must not duplicate shard children.");
        }
        finally
        {
            UnityEngine.Random.state = original;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void PixelTexturesHaveCrispEdges()
    {
        foreach (Sprite sprite in new[] { PixelVfxSprites.Burst, PixelVfxSprites.Ring, PixelVfxSprites.Chip })
        {
            Require(sprite.texture.filterMode == FilterMode.Point, "VFX textures must use point filtering.");
            foreach (Color32 pixel in sprite.texture.GetPixels32())
                Require(pixel.a == 0 || pixel.a == 255, "VFX texture edges must remain pixel crisp.");
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
