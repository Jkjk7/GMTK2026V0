using UnityEngine;

/// <summary>
/// 开局/局内共享设置：语言、显示模式；开发者模式仅本进程有效。
/// </summary>
public static class GameSettings
{
    public const string DeveloperPassword = "314";
    const string LegacyDevModePrefKey = "gmtk.dev_mode";
    const string FullscreenPrefKey = "gmtk.fullscreen";

    /// <summary>窗口化固定分辨率（接近常见 IDE / Cursor 窗口体量）。</summary>
    public const int WindowedWidth = 1440;
    public const int WindowedHeight = 900;

    static bool _loaded;
    static bool _developerMode;
    static bool _fullscreen;

    public static bool DeveloperMode
    {
        get
        {
            EnsureLoaded();
            return _developerMode;
        }
    }

    public static bool IsFullscreen
    {
        get
        {
            EnsureLoaded();
            return _fullscreen;
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
        if (PlayerPrefs.HasKey(FullscreenPrefKey))
        {
            _fullscreen = PlayerPrefs.GetInt(FullscreenPrefKey, 1) != 0;
        }
        else
        {
            _fullscreen = Screen.fullScreen;
        }

        _loaded = true;
        _ = GameLocalization.CurrentLanguage;
        ApplyDisplayMode();
    }

    public static void SetLanguage(GameLanguage language)
    {
        GameLocalization.SetLanguage(language, save: true);
    }

    public static void SetFullscreen(bool fullscreen)
    {
        EnsureLoaded();
        _fullscreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenPrefKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        ApplyDisplayMode();
    }

    public static void ToggleFullscreen()
    {
        SetFullscreen(!IsFullscreen);
    }

    /// <summary>打包后可用：全屏用桌面分辨率；窗口化固定 1440×900。</summary>
    public static void ApplyDisplayMode()
    {
        EnsureLoaded();
        if (_fullscreen)
        {
            Resolution desk = Screen.currentResolution;
            Screen.SetResolution(desk.width, desk.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(WindowedWidth, WindowedHeight, FullScreenMode.Windowed);
        }
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

    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
