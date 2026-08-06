using UnityEngine;

/// <summary>
/// 开局设置：语言走 <see cref="GameLocalization"/>；开发者模式仅本进程有效，每次启动默认关闭。
/// </summary>
public static class GameSettings
{
    public const string DeveloperPassword = "314";
    const string LegacyDevModePrefKey = "gmtk.dev_mode";

    static bool _loaded;
    static bool _developerMode;

    public static bool DeveloperMode
    {
        get
        {
            EnsureLoaded();
            return _developerMode;
        }
    }

    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        // 开发者模式不持久化；清掉旧 PlayerPrefs，避免误读
        if (PlayerPrefs.HasKey(LegacyDevModePrefKey))
        {
            PlayerPrefs.DeleteKey(LegacyDevModePrefKey);
            PlayerPrefs.Save();
        }

        _developerMode = false;
        _loaded = true;
        _ = GameLocalization.CurrentLanguage;
    }

    public static void SetLanguage(GameLanguage language)
    {
        GameLocalization.SetLanguage(language, save: true);
    }

    public static bool TryEnableDeveloperMode(string password)
    {
        if (password != DeveloperPassword)
        {
            return false;
        }

        _developerMode = true;
        _loaded = true;
        return true;
    }

    public static void DisableDeveloperMode()
    {
        _developerMode = false;
        _loaded = true;
    }
}
