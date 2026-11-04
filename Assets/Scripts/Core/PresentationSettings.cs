using UnityEngine;

/// <summary>Presentation-only preferences. Never changes simulation speed or combat rules.</summary>
public static class PresentationSettings
{
    public enum EffectsQuality { Reduced, Balanced, Cinematic }
    const string QualityKey = "timesands.fx_quality";
    const string MotionKey = "timesands.motion";
    static bool _loaded;
    static EffectsQuality _quality;
    static bool _motion;

    public static EffectsQuality Quality { get { Load(); return _quality; } }
    public static bool MotionEnabled { get { Load(); return _motion; } }
    public static float ParticleIntensity
    {
        get
        {
            switch (Quality)
            {
                case EffectsQuality.Reduced: return 0.25f;
                case EffectsQuality.Balanced: return 0.65f;
                default: return 1f;
            }
        }
    }

    static void Load()
    {
        if (_loaded) return;
        _quality = Validate((EffectsQuality)PlayerPrefs.GetInt(QualityKey, (int)EffectsQuality.Cinematic));
        _motion = PlayerPrefs.GetInt(MotionKey, 1) != 0;
        _loaded = true;
    }

    static EffectsQuality Validate(EffectsQuality value) =>
        value >= EffectsQuality.Reduced && value <= EffectsQuality.Cinematic ? value : EffectsQuality.Cinematic;

    public static void CycleQuality() => SetQuality((EffectsQuality)(((int)Quality + 1) % 3));
    public static void ToggleMotion() => SetMotion(!MotionEnabled);

    public static void SetQuality(EffectsQuality value, bool save = true)
    {
        Load();
        _quality = Validate(value);
        if (save) { PlayerPrefs.SetInt(QualityKey, (int)_quality); PlayerPrefs.Save(); }
    }

    public static void SetMotion(bool enabled, bool save = true)
    {
        Load();
        _motion = enabled;
        if (save) { PlayerPrefs.SetInt(MotionKey, enabled ? 1 : 0); PlayerPrefs.Save(); }
    }
}
