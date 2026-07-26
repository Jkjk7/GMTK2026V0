using System;
using UnityEditor;
using UnityEngine;

/// <summary>Batch-mode checks for English-first language selection.</summary>
public static class LocalizationRegressionChecks
{
    public static void Run()
    {
        try
        {
            GameLocalization.ResetForTests();
            Require(
                GameLocalization.CurrentLanguage == GameLanguage.English,
                "Fresh fallback must be English.");
            Require(
                GameLocalization.Text("Shop", "商店") == "Shop",
                "English selection failed.");

            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese, false);
            Require(
                GameLocalization.Text("Shop", "商店") == "商店",
                "Chinese selection failed.");

            GameLocalization.SetLanguage((GameLanguage)99, false);
            Require(
                GameLocalization.CurrentLanguage == GameLanguage.English,
                "Invalid values must fall back to English.");

            GameLocalization.ResetForTests();
            Debug.Log("[Localization Regression] PASS");
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
}
