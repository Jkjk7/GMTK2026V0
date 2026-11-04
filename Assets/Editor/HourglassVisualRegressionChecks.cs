using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Real-clock visual checks; run via -executeMethod HourglassVisualRegressionChecks.Run.</summary>
public static class HourglassVisualRegressionChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        try { Verify(); Debug.Log("[Hourglass Visual Regression] PASS"); EditorApplication.Exit(0); }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        bool motion = PresentationSettings.MotionEnabled;
        float timeScale = Time.timeScale;
        UnityEngine.Random.State random = UnityEngine.Random.state;
        var root = new GameObject("HourglassRegression", typeof(RectTransform));
        try
        {
            PresentationSettings.SetMotion(true, false);
            Time.timeScale = 1f;
            var clock = root.AddComponent<SandClock>();
            var panel = root.AddComponent<SandClockPanel>();
            Text countdown = Child<Text>(root, "Countdown");
            Text delta = Child<Text>(root, "Delta");
            Image top = Child<Image>(root, "Top");
            Image bottom = Child<Image>(root, "Bottom");
            Image frame = Child<Image>(root, "Frame");
            countdown.rectTransform.anchorMin = new Vector2(.02f, .72f);
            countdown.rectTransform.anchorMax = new Vector2(.98f, .98f);
            frame.rectTransform.anchorMin = new Vector2(.08f, .02f);
            frame.rectTransform.anchorMax = new Vector2(.92f, .74f);
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(212, 194);
            panel.Bind(clock, countdown, delta, top, bottom, frame);
            Require(frame.sprite != null && frame.sprite.name == "PixelHourglassFrame",
                "The hourglass needs a crisp pixel frame instead of the old illustration.");
            Require(frame.sprite.texture.filterMode == FilterMode.Point,
                "Hourglass artwork must use point filtering.");
            Require(countdown.text == "01:40.000", "Binding must show the actual initial resource.");
            Sprite full = top.sprite;
            int fullPixels = OpaquePixels(full);
            Require(fullPixels > 0 && OpaquePixels(bottom.sprite) == 0,
                "Full resources must fill the upper chamber and leave the lower chamber empty.");
            clock.TryDrain(50000);
            Refresh(panel);
            Require(countdown.text == "00:50.000", "Hourglass must read the exact clock value.");
            Require(OpaquePixels(top.sprite) < fullPixels && OpaquePixels(top.sprite) > 0
                && OpaquePixels(bottom.sprite) > 0, "Drain must move visible sand between chambers.");
            MethodInfo tickFlow = typeof(SandClockPanel).GetMethod("TickFlow", Private);
            tickFlow.Invoke(panel, new object[] { true });
            Image[] grains = (Image[])typeof(SandClockPanel).GetField("_grains", Private).GetValue(panel);
            Require(grains.Length > 0 && grains.Length <= 8, "Sand flow must use a small fixed pool.");
            foreach (Image grain in grains)
                Require(grain.enabled && !grain.raycastTarget, "Actual draining must show nonblocking sand grains.");
            float age = (float)typeof(SandClockPanel).GetField("_flowAge", Private).GetValue(panel);
            Time.timeScale = 0f;
            tickFlow.Invoke(panel, new object[] { true });
            Require((float)typeof(SandClockPanel).GetField("_flowAge", Private).GetValue(panel) == age,
                "Paused gameplay must not advance flowing sand.");
            foreach (Image grain in grains) Require(!grain.enabled, "Pause must stop the running-sand indication.");
            Time.timeScale = 1f;
            clock.AddSand(100000);
            Refresh(panel);
            Require(countdown.text == "02:30.000" && top.sprite == full && OpaquePixels(bottom.sprite) == 0,
                "Overflow must cap the sand illustration while keeping the full timer value.");
            PresentationSettings.SetMotion(false, false);
            clock.TryDrain(145000);
            Refresh(panel);
            Require(countdown.text == "00:05.000" && countdown.transform.localScale == Vector3.one,
                "Reduced motion must keep the timer exact without warning scale pulses.");
            Require(top.color != Color.white, "Low sand must remain visibly warned with motion disabled.");
            tickFlow.Invoke(panel, new object[] { false });
            foreach (Image grain in grains) Require(!grain.enabled, "Reduced motion must disable decorative sand flow.");
            clock.TryDrain(5000);
            Refresh(panel);
            Require(countdown.text == "00:00.000" && OpaquePixels(top.sprite) == 0,
                "The empty clock must not show sand remaining in the upper chamber.");
            int childCount = root.GetComponentsInChildren<Transform>(true).Length;
            UnityEngine.Random.InitState(9001);
            float expectedRandom = UnityEngine.Random.value;
            UnityEngine.Random.InitState(9001);
            panel.Bind(clock, countdown, delta, top, bottom, frame);
            for (int i = 0; i < 60; i++) Refresh(panel);
            Require(root.GetComponentsInChildren<Transform>(true).Length == childCount,
                "Rebinding and refreshing must reuse the same bounded visual objects.");
            Require(UnityEngine.Random.value == expectedRandom, "Hourglass visuals must not consume gameplay RNG.");
            Require(clock.RemainingMs == 0, "Rendering must not change the clock resource.");
            Require(top.sprite.texture == full.texture && frame.sprite.texture == full.texture,
                "Sand levels and frame must reuse the same cached pixel atlas.");
            Require(frame.rectTransform.anchorMax.y <= countdown.rectTransform.anchorMin.y,
                "The hourglass silhouette must stay below the timer number.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            PresentationSettings.SetMotion(motion, false);
            Time.timeScale = timeScale;
            UnityEngine.Random.state = random;
        }
    }

    static T Child<T>(GameObject root, string name) where T : Graphic
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root.transform, false);
        return go.AddComponent<T>();
    }

    static int OpaquePixels(Sprite sprite)
    {
        Require(sprite != null, "Sand must use a cached pixel sprite.");
        Rect rect = sprite.textureRect;
        Color[] pixels = sprite.texture.GetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
        int count = 0;
        foreach (Color c in pixels)
        {
            Require(c.a == 0 || c.a == 1, "Sand must have crisp pixel edges.");
            if (c.a > .5f) count++;
        }
        return count;
    }

    static void Refresh(SandClockPanel panel) => typeof(SandClockPanel).GetMethod("Refresh", Private).Invoke(panel, null);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
