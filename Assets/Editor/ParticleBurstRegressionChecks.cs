using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Run with -executeMethod ParticleBurstRegressionChecks.Run, or Verify in a player harness.</summary>
public static class ParticleBurstRegressionChecks
{
    public static void Run()
    {
        try { Verify(); Debug.Log("[Particle Burst Regression] PASS"); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        // Removing the pool cap, recycling reset, expiry or private RNG breaks these observations.
        Type type = typeof(CombatVfxService).Assembly.GetType("PixelParticleBurst");
        Require(type != null, "Combat explosions must provide pooled moving pixel fragments.");
        MethodInfo spawn = typeof(CombatVfxService).GetMethod("SpawnExplosion");
        Require(spawn != null, "Module callers need a bounded explosion API.");
        MethodInfo advance = type.GetMethod("Advance", BindingFlags.NonPublic | BindingFlags.Instance);
        var initialRandom = UnityEngine.Random.state;
        GameObject root = null;
        PresentationSettings.EffectsQuality quality = PresentationSettings.Quality;
        try
        {
            UnityEngine.Random.InitState(9917);
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(9917);
            var instances = new HashSet<int>();
            Component first = null;
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Cinematic, false);
            for (int i = 0; i < 240; i++)
            {
                var effect = (Component)spawn.Invoke(null, new object[] { Vector3.one, new Color(1f, .4f, .1f), 2f });
                Require(effect != null && effect.gameObject.activeSelf, "An explosion must immediately display fragments.");
                if (first == null) { first = effect; root = effect.transform.parent.gameObject; }
                instances.Add(effect.GetInstanceID());
            }
            Require(instances.Count <= 24, "Dense combat must recycle at most 24 particle bursts.");
            Require(UnityEngine.Random.value == expected, "Visual particles must not consume gameplay RNG.");
            var renderers = first.GetComponentsInChildren<SpriteRenderer>(true);
            Require(renderers.Length <= 23, "A burst must keep its fragment, trail, ring and flash renderers bounded.");
            Require(renderers.Length >= 15, "Explosions must include multiple fragments and trails.");
            foreach (var renderer in renderers)
                Require(renderer.sortingOrder < 30, "Particle layers must leave enemy health bars readable.");
            foreach (Transform child in root.transform)
            {
                Component effect = child.GetComponent(type);
                if (effect == null) continue;
                advance.Invoke(effect, new object[] { 1.1f });
                Require(!effect.gameObject.activeSelf, "Expired bursts must return inactive to the pool.");
                foreach (var renderer in effect.GetComponentsInChildren<SpriteRenderer>(true))
                    Require(!renderer.enabled, "Expired bursts must leave no hidden active renderers.");
            }
            var reused = (Component)spawn.Invoke(null, new object[] { Vector3.right, Color.cyan, 1f });
            Require(instances.Contains(reused.GetInstanceID()), "Finished bursts must reuse existing objects.");
            Require(reused.transform.position == Vector3.right, "Reuse must reset the emission position.");
            Require(reused.transform.localScale == Vector3.one, "Reuse must clear any stale scale.");
            advance.Invoke(reused, new object[] { .08f });
            bool moving = false;
            foreach (var renderer in reused.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled && renderer.transform.localPosition.sqrMagnitude > .001f) moving = true;
            Require(moving, "Fragments must visibly move away from the impact.");
            advance.Invoke(reused, new object[] { 1.1f });
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Reduced, false);
            var reduced = (Component)spawn.Invoke(null, new object[] { Vector3.zero, Color.cyan, 1f });
            int reducedVisible = 0;
            foreach (var renderer in reduced.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.enabled) reducedVisible++;
            Require(reducedVisible < renderers.Length, "Reduced intensity must reduce visible fragment count.");
        }
        finally
        {
            PresentationSettings.SetQuality(quality, false);
            UnityEngine.Random.state = initialRandom;
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
