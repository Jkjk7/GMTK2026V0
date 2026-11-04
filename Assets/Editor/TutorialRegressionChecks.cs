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
            for (int lesson = 0; lesson < 6; lesson++)
            {
                overlay.SelectLesson(lesson);
                Require(overlay.transform.Find("Shade/GuidePanel/LessonContent/Body").GetComponent<Text>().text.Length > 100,
                    "Every lesson needs useful explanatory copy.");
            }
            UnityEngine.Object.DestroyImmediate(overlay.gameObject);
            Require(!TutorialOverlay.IsOpen, "Scene destruction must not leave a stale lock.");
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese, false);
            TutorialOverlay.Show(font);
            overlay = UnityEngine.Object.FindFirstObjectByType<TutorialOverlay>();
            Require(overlay.transform.Find("Shade/GuidePanel/Title").GetComponent<Text>().text == "新手指南",
                "Guide must follow the selected language.");
            TutorialOverlay.Close();
        }
        finally
        {
            TutorialOverlay.Close();
            Time.timeScale = previous;
            GameLocalization.SetLanguage(previousLanguage, false);
        }
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
