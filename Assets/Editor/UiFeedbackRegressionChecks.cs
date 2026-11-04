using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiFeedbackRegressionChecks
{
    const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    public static void Run()
    {
        try { Verify(); Debug.Log("[UI Feedback Regression] PASS"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
    public static void Verify()
    {
        Type type = typeof(PresentationSettings).Assembly.GetType("UiFeedback");
        Require(type != null, "Interactive UI needs reusable response feedback.");
        var root = new GameObject("UiFeedbackRegression", typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)root.transform;
        rect.sizeDelta = new Vector2(160, 90);
        rect.anchoredPosition = new Vector2(33, 47);
        rect.localScale = new Vector3(.8f, .9f, 1);
        Vector2 position = rect.anchoredPosition;
        Vector3 scale = rect.localScale;
        bool motion = PresentationSettings.MotionEnabled;
        float timeScale = Time.timeScale;
        var random = UnityEngine.Random.state;
        try
        {
            PresentationSettings.SetMotion(true, false);
            Time.timeScale = 0f;
            UnityEngine.Random.InitState(7391);
            float expectedRandom = UnityEngine.Random.value;
            UnityEngine.Random.InitState(7391);
            type.GetMethod("Reject").Invoke(null, new object[] { rect, "Not enough gold" });
            Component component = root.GetComponent(type);
            MethodInfo tick = type.GetMethod("Tick", Private);
            Require(tick != null, "Feedback needs a bounded visual tick.");
            tick.Invoke(component, new object[] { .08f });
            Require(rect.anchoredPosition != position, "Denied action must visibly shake in full-motion mode.");
            Require(Time.timeScale == 0f, "Presentation must not unpause the game.");
            Require(UnityEngine.Random.value == expectedRandom, "UI effects must not consume gameplay RNG.");
            int childCount = root.GetComponentsInChildren<Transform>(true).Length;
            for (int i = 0; i < 100; i++) type.GetMethod("Receive").Invoke(null, new object[] { rect });
            Require(root.GetComponentsInChildren<Transform>(true).Length == childCount, "Repeated feedback must reuse its decorations.");
            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                if (graphic.gameObject != root) Require(!graphic.raycastTarget, "Decorations must never intercept clicks.");
            tick.Invoke(component, new object[] { 2f });
            Require(rect.anchoredPosition == position && rect.localScale == scale, "Finished effects must restore the exact layout.");
            PresentationSettings.SetMotion(false, false);
            type.GetMethod("Receive").Invoke(null, new object[] { rect });
            tick.Invoke(component, new object[] { .12f });
            Require(rect.anchoredPosition == position && rect.localScale == scale, "Reduced-motion feedback must remain stationary.");
            PresentationSettings.SetMotion(true, false);
            type.GetMethod("Reject").Invoke(null, new object[] { rect, null });
            tick.Invoke(component, new object[] { .08f });
            root.SetActive(false);
            Require(rect.anchoredPosition == position && rect.localScale == scale, "Interrupted feedback must restore the exact layout.");
            root.SetActive(true);
            type.GetMethod("Receive").Invoke(null, new object[] { rect });
            tick.Invoke(component, new object[] { 2f });
            Require(rect.anchoredPosition == position && rect.localScale == scale, "Reopening must not accumulate offsets.");
            LayoutChangesSurvive(type);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            PresentationSettings.SetMotion(motion, false);
            Time.timeScale = timeScale;
            UnityEngine.Random.state = random;
        }
    }
    static void LayoutChangesSurvive(Type type)
    {
        var parent = new GameObject("ResizingGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        try
        {
            var layout = parent.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(100, 80);
            layout.childAlignment = TextAnchor.MiddleCenter;
            var parentRect = (RectTransform)parent.transform;
            parentRect.sizeDelta = new Vector2(400, 160);
            var child = new GameObject("ResponsiveCard", typeof(RectTransform), typeof(Image));
            child.transform.SetParent(parent.transform, false);
            var card = (RectTransform)child.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            type.GetMethod("Receive").Invoke(null, new object[] { card });
            Component feedback = child.GetComponent(type);
            MethodInfo tick = type.GetMethod("Tick", Private);
            tick.Invoke(feedback, new object[] { .08f });
            parentRect.sizeDelta = new Vector2(720, 240);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            Vector2 resizedPosition = card.anchoredPosition;
            tick.Invoke(feedback, new object[] { .12f });
            tick.Invoke(feedback, new object[] { 2f });
            Require(card.anchoredPosition == resizedPosition,
                "Resizing a grid during feedback must retain the new layout position.");
            type.GetMethod("Reject").Invoke(null, new object[] { card, null });
            tick.Invoke(feedback, new object[] { .08f });
            parentRect.sizeDelta = new Vector2(560, 160);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            resizedPosition = card.anchoredPosition;
            child.SetActive(false);
            Require(card.anchoredPosition == resizedPosition,
                "Cancelling immediately after a resize must retain the layout update.");
        }
        finally { UnityEngine.Object.DestroyImmediate(parent); }
    }
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
