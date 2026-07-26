using UnityEngine;

public enum GameLanguage
{
    English = 0,
    SimplifiedChinese = 1
}

/// <summary>Small dependency-free localization service for the jam build.</summary>
public static class GameLocalization
{
    const string PreferenceKey = "gmtk.language";

    static bool _initialized;
    static GameLanguage _currentLanguage;

    public static GameLanguage CurrentLanguage
    {
        get
        {
            EnsureInitialized();
            return _currentLanguage;
        }
    }

    public static bool IsChinese => CurrentLanguage == GameLanguage.SimplifiedChinese;

    public static string Text(string english, string chinese)
    {
        return IsChinese ? chinese : english;
    }

    public static void SetLanguage(GameLanguage language, bool save = true)
    {
        _currentLanguage = language == GameLanguage.SimplifiedChinese
            ? GameLanguage.SimplifiedChinese
            : GameLanguage.English;
        _initialized = true;

        if (save)
        {
            PlayerPrefs.SetInt(PreferenceKey, (int)_currentLanguage);
            PlayerPrefs.Save();
        }
    }

    public static void ResetForTests(bool clearPreference = true)
    {
        if (clearPreference)
        {
            PlayerPrefs.DeleteKey(PreferenceKey);
        }

        _initialized = false;
        _currentLanguage = GameLanguage.English;
    }

    static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        int stored = PlayerPrefs.GetInt(PreferenceKey, (int)GameLanguage.English);
        _currentLanguage = stored == (int)GameLanguage.SimplifiedChinese
            ? GameLanguage.SimplifiedChinese
            : GameLanguage.English;
        _initialized = true;
    }
}
