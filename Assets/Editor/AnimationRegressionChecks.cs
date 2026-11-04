using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Visual isolation checks; run via -executeMethod AnimationRegressionChecks.Run.</summary>
public static class AnimationRegressionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try { Verify(); Debug.Log("[Animation Regression] PASS"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        bool motion = PresentationSettings.MotionEnabled;
        UnityEngine.Random.State random = UnityEngine.Random.state;
        try
        {
            PresentationSettings.SetMotion(true, false);
            RootAndColliderStayUnchanged();
            FreezeStopsAndResetsPose();
            ReducedMotionKeepsStatuses();
            DamageProducesVisualFlinch();
            ArchetypesHaveDistinctMotion();
            CasterPoseObservesChargeVisual();
            FurnaceMotionStaysOnChildren();
        }
        finally { PresentationSettings.SetMotion(motion, false); UnityEngine.Random.state = random; }
    }

    static EnemyVisualController Create(EnemyGoldType type, out GameObject root)
    {
        root = new GameObject("AnimationRegression_" + type);
        var enemy = root.AddComponent<Enemy>();
        enemy.enabled = false;
        typeof(Enemy).GetField("goldType", Private).SetValue(enemy, type);
        typeof(Enemy).GetField("_currentHp", Private).SetValue(enemy, 100);
        root.AddComponent<SpriteRenderer>().color = new Color(0.4f, 0.5f, 0.6f);
        var visual = root.AddComponent<EnemyVisualController>();
        visual.Initialize(enemy);
        return visual;
    }

    static void Tick(EnemyVisualController visual, float delta)
    {
        MethodInfo tick = typeof(EnemyVisualController).GetMethod("Tick", Private);
        Require(tick != null, "Animation requires a deterministic visual tick for isolation checks.");
        tick.Invoke(visual, new object[] { delta });
    }

    static void RootAndColliderStayUnchanged()
    {
        var visual = Create(EnemyGoldType.Swarm, out var root);
        try
        {
            root.transform.SetPositionAndRotation(new Vector3(3, 4, 0), Quaternion.Euler(0, 0, 12));
            root.transform.localScale = new Vector3(2, 3, 1);
            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.8f, 0.6f);
            Vector3 position = root.transform.position, scale = root.transform.localScale;
            Quaternion rotation = root.transform.rotation;
            UnityEngine.Random.InitState(813);
            float expected = UnityEngine.Random.value;
            UnityEngine.Random.InitState(813);
            for (int i = 0; i < 100; i++) Tick(visual, 1f / 30f);
            Require(root.transform.position == position && root.transform.localScale == scale
                && root.transform.rotation == rotation, "Added animation must only change visual children.");
            Require(collider.size == new Vector2(0.8f, 0.6f), "Animation must preserve colliders.");
            Require(UnityEngine.Random.value == expected, "Animation must preserve gameplay randomness.");
            int count = root.GetComponentsInChildren<Transform>(true).Length;
            visual.Initialize(root.GetComponent<Enemy>());
            for (int i = 0; i < 200; i++) Tick(visual, 1f / 30f);
            Require(root.GetComponentsInChildren<Transform>(true).Length == count,
                "Animation and reinitialization must reuse bounded visual children.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void FreezeStopsAndResetsPose()
    {
        var visual = Create(EnemyGoldType.Elite, out var root);
        try
        {
            Tick(visual, 0.3f);
            root.GetComponent<Enemy>().ApplyFreeze(2f);
            Tick(visual, 0.1f);
            Transform body = root.transform.Find("EnemySilhouette");
            Require(body.localPosition == Vector3.zero && body.localScale == Vector3.one
                && body.localRotation == Quaternion.identity, "Freeze must reset locomotion to a legible rest pose.");
            Tick(visual, 0.5f);
            Require(body.localPosition == Vector3.zero && body.localRotation == Quaternion.identity,
                "Frozen silhouettes must remain still.");
            Require(visual.ChillVisible, "Freeze indication must remain visible.");
            typeof(Enemy).GetField("_freezeTimer", Private).SetValue(root.GetComponent<Enemy>(), 0f);
            Tick(visual, 0.2f);
            Require(body.localPosition != Vector3.zero || body.localRotation != Quaternion.identity,
                "Locomotion must resume after freeze.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ReducedMotionKeepsStatuses()
    {
        var visual = Create(EnemyGoldType.Normal, out var root);
        try
        {
            var enemy = root.GetComponent<Enemy>();
            enemy.ApplyBurn(2); enemy.ApplySlow(0.3f, 2);
            Tick(visual, 0.3f);
            PresentationSettings.SetMotion(false, false);
            Tick(visual, 0.2f);
            Transform body = root.transform.Find("EnemySilhouette");
            Require(body.localPosition == Vector3.zero && body.localScale == Vector3.one
                && body.localRotation == Quaternion.identity, "Reduced motion must restore the rest pose.");
            Require(visual.BurnVisible && visual.ChillVisible, "Reduced motion must preserve status information.");
            var renderer = body.Find("NormalMarker").GetComponent<SpriteRenderer>();
            Color beforeHit = renderer.color;
            typeof(Enemy).GetField("_currentHp", Private).SetValue(enemy, 90);
            Tick(visual, 0f);
            Require(renderer.color.r > beforeHit.r && body.localPosition == Vector3.zero,
                "Reduced motion must preserve hit flash without positional recoil.");
        }
        finally { PresentationSettings.SetMotion(true, false); UnityEngine.Object.DestroyImmediate(root); }
    }

    static void DamageProducesVisualFlinch()
    {
        var visual = Create(EnemyGoldType.Tank, out var root);
        try
        {
            Tick(visual, 0f);
            typeof(Enemy).GetField("_currentHp", Private).SetValue(root.GetComponent<Enemy>(), 90);
            Tick(visual, 0f);
            Require(root.transform.Find("EnemySilhouette").localPosition.x > 0f,
                "Observed HP loss must give a brief child-only recoil.");
            Require(root.GetComponent<Enemy>().CurrentHitPoints == 90, "Visual recoil must not modify HP.");
            Tick(visual, 0.5f);
            Require(root.transform.Find("EnemySilhouette").localPosition.x < 0.03f,
                "Hit recoil must settle without cumulative offset.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void ArchetypesHaveDistinctMotion()
    {
        var signatures = new HashSet<string>();
        foreach (EnemyGoldType type in Enum.GetValues(typeof(EnemyGoldType)))
        {
            var visual = Create(type, out var root);
            try
            {
                string signature = "";
                typeof(EnemyVisualController).GetField("_phaseOffset", Private).SetValue(visual, 0f);
                for (int i = 0; i < 4; i++)
                {
                    Tick(visual, 0.13f);
                    Transform body = root.transform.Find("EnemySilhouette");
                    signature += body.localPosition.ToString("F4") + body.localScale.ToString("F4")
                        + body.localEulerAngles.z.ToString("F3");
                }
                Require(signatures.Add(signature), "Each archetype needs its own motion silhouette: " + type);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }

    static void CasterPoseObservesChargeVisual()
    {
        var visual = Create(EnemyGoldType.Disassembler, out var root);
        try
        {
            var charge = new GameObject("PurpleChargeFx");
            charge.transform.SetParent(root.transform, false);
            charge.SetActive(false);
            Tick(visual, 0f);
            Transform body = root.transform.Find("EnemySilhouette");
            Vector3 restScale = body.localScale;
            int hp = root.GetComponent<Enemy>().CurrentHitPoints;
            charge.SetActive(true);
            Tick(visual, 0f);
            Require(body.localScale.y > restScale.y && body.localScale.x < restScale.x,
                "A visible caster charge must accent its silhouette without driving the ability.");
            Require(root.GetComponent<Enemy>().CurrentHitPoints == hp && charge.activeSelf,
                "Charge observation must preserve combat state and its source visual.");
            charge.SetActive(false);
            Tick(visual, 0f);
            Require(body.localScale == restScale, "The charge accent must settle as soon as casting ends.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    static void FurnaceMotionStaysOnChildren()
    {
        var root = new GameObject("FurnaceAnimationRegression");
        try
        {
            var emitter = root.AddComponent<Emitter>();
            emitter.enabled = false;
            Type type = typeof(Emitter).Assembly.GetType("WorldVisualAnimator");
            Require(type != null, "Furnace needs a visual-only child animator.");
            type.GetMethod("AttachEmitter").Invoke(null, new object[] { emitter });
            Component animator = root.GetComponent(type);
            Require(animator != null, "Furnace animation must attach once.");
            type.GetMethod("AttachEmitter").Invoke(null, new object[] { emitter });
            Require(root.GetComponents(type).Length == 1, "Furnace attachment must be idempotent.");
            Vector3 scale = root.transform.localScale, position = root.transform.position;
            MethodInfo tick = type.GetMethod("Tick", Private);
            for (int i = 0; i < 30; i++) tick.Invoke(animator, new object[] { 1f / 30f });
            Require(root.transform.localScale == scale && root.transform.position == position,
                "Furnace accent must preserve emitter spawn transform.");
            typeof(Emitter).GetField("_bufferMs", Private).SetValue(emitter, emitter.CapMs - 1);
            tick.Invoke(animator, new object[] { 0f });
            typeof(Emitter).GetField("_bufferMs", Private).SetValue(emitter, 0);
            tick.Invoke(animator, new object[] { 0f });
            Transform nozzle = root.transform.Find("FurnacePresentation/Nozzle");
            Require(nozzle.localPosition.x < 0.47f && emitter.BufferMs == 0,
                "Observed buffer wrap must recoil only the nozzle without changing stored energy.");
            PresentationSettings.SetMotion(false, false);
            tick.Invoke(animator, new object[] { 0.1f });
            Require(nozzle != null && nozzle.localPosition == new Vector3(0.47f, 0f, 0f),
                "Reduced motion must restore the furnace nozzle rest pose.");
        }
        finally { PresentationSettings.SetMotion(true, false); UnityEngine.Object.DestroyImmediate(root); }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
