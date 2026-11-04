using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Also compiled by the standalone smoke harness; no player dependency on UnityEditor.</summary>
public static class TutorialRegressionChecks
{
    public static void Run()
    {
        try
        {
            Verify();
            Debug.Log("[Tutorial Regression] PASS");
#if UNITY_EDITOR
            EditorApplication.Exit(0);
#endif
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
#if UNITY_EDITOR
            EditorApplication.Exit(1);
#else
            throw;
#endif
        }
    }

    public static void Verify()
    {
        float previous = Time.timeScale;
        GameLanguage previousLanguage = GameLocalization.CurrentLanguage;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        try
        {
            foreach (float scale in new[] { 1f, 0.35f, 0f })
            {
                Time.timeScale = scale;
                TutorialOverlay.Show(font);
                Require(TutorialOverlay.IsOpen, "Guide must open only on request.");
                Require(Time.timeScale == 0f, "Guide must pause the simulation.");
                TutorialOverlay.Show(font, 5);
                Require(UnityEngine.Object.FindObjectsByType<TutorialOverlay>(FindObjectsSortMode.None).Length == 1,
                    "Reopening must reuse the active guide.");
                TutorialOverlay.Close();
                Require(!TutorialOverlay.IsOpen, "Closing must release the modal lock immediately.");
                Require(Time.timeScale == scale, "Closing must restore the exact previous time scale, including zero.");
                Require(TutorialOverlay.BlocksInput, "Closing must consume the close frame's gameplay inputs.");
            }

            TutorialOverlay.Show(font);
            TutorialOverlay overlay = UnityEngine.Object.FindFirstObjectByType<TutorialOverlay>();
            Require(overlay.GetComponent<GraphicRaycaster>() != null, "Guide must block UI raycasts.");
            Require(overlay.GetComponent<Canvas>().sortingOrder >= 100, "Guide must sit above game settings.");
            Require(overlay.transform.Find("Shade/GuidePanel/Lessons/Lesson5") != null,
                "All six lessons must be directly selectable.");
            Require(overlay.GetComponentsInChildren<Button>().Length >= 12, "Guide needs lesson, tab and navigation buttons.");
            foreach (Button button in overlay.GetComponentsInChildren<Button>())
            {
                Require(button.navigation.mode == Navigation.Mode.Explicit, "Keyboard focus must remain within the guide.");
                Require(button.navigation.selectOnLeft == null && button.navigation.selectOnRight == null,
                    "Horizontal lesson paging must not also change button focus.");
            }
            UnityEngine.Object.DestroyImmediate(overlay.gameObject);
            Require(!TutorialOverlay.IsOpen, "Scene destruction must not leave a stale lock.");
            VerifyLessons(font, GameLanguage.English);
            VerifyLessons(font, GameLanguage.SimplifiedChinese);
        }
        finally
        {
            TutorialOverlay.Close();
            Time.timeScale = previous;
            GameLocalization.SetLanguage(previousLanguage, false);
        }
    }

    static void VerifyLessons(Font font, GameLanguage language)
    {
        GameLocalization.SetLanguage(language, false);
        TutorialOverlay.Show(font);
        TutorialOverlay overlay = UnityEngine.Object.FindFirstObjectByType<TutorialOverlay>();
        bool chinese = language == GameLanguage.SimplifiedChinese;
        Require(overlay.transform.Find("Shade/GuidePanel/Title").GetComponent<Text>().text ==
            (chinese ? "新手指南" : "Field Guide"), "Guide must follow the selected language.");
        string[] topics = chinese
            ? new[] { "使用金币购买模块", "按 R 旋转", "发射器消耗沙漏中的沙", "25 个普通波次", "会触发融化", "03  规划金币支出" }
            : new[] { "Buy a module with coins", "R rotates", "emitter spends hourglass sand", "25 regular waves", "triggers Melt", "03  PLAN YOUR SPENDING" };
        Transform content = overlay.transform.Find("Shade/GuidePanel/LessonContent");
        for (int lesson = 0; lesson < topics.Length; lesson++)
        {
            overlay.SelectLesson(lesson);
            // Old lesson objects are inactive until Unity's deferred Destroy runs.
            // Inspect only the visible page, including when paging twice in one frame.
            string body = ActiveText(content, "Body");
            Require(body.Contains(topics[lesson]) && body.Contains("\n\n"),
                language + " lesson " + (lesson + 1) + " must explain its own topic in multiple paragraphs.");
            ActiveText(content, "LessonTitle");
            ActiveText(content, "Heading");
            ActiveText(content, "Note");
        }
        TutorialOverlay.Close();
    }

    static string ActiveText(Transform content, string name)
    {
        Text found = null;
        foreach (Text label in content.GetComponentsInChildren<Text>())
        {
            if (label.name != name) continue;
            Require(found == null, "The visible lesson must have only one " + name + ".");
            found = label;
        }
        Require(found != null && !string.IsNullOrWhiteSpace(found.text), "The visible lesson needs a " + name + ".");
        return found.text;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
