using UnityEngine.SceneManagement;

/// <summary>主菜单 / 中英文游戏场景名与加载入口。</summary>
public static class GameFlow
{
    public const string MainMenuScene = "MainMenu";
    public const string GameSceneEnglish = "Game_EN";
    public const string GameSceneChinese = "Game_ZH";

    public static bool IsMainMenuScene(string sceneName) =>
        sceneName == MainMenuScene;

    public static bool IsGameScene(string sceneName) =>
        sceneName == GameSceneEnglish
        || sceneName == GameSceneChinese
        || sceneName == "SampleScene";

    public static string GetGameSceneForLanguage(GameLanguage language) =>
        language == GameLanguage.SimplifiedChinese
            ? GameSceneChinese
            : GameSceneEnglish;

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void LoadGameForCurrentLanguage()
    {
        GameSettings.EnsureLoaded();
        SceneManager.LoadScene(GetGameSceneForLanguage(GameLocalization.CurrentLanguage));
    }
}
