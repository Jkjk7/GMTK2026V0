using System;
using UnityEditor;
using UnityEngine;

public static class PresentationSettingsRegressionChecks
{
    public static void Run()
    {
        try { Verify(); Debug.Log("[Presentation Settings] PASS"); EditorApplication.Exit(0); }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }

    public static void Verify()
    {
        var quality = PresentationSettings.Quality;
        bool motion = PresentationSettings.MotionEnabled;
        float timeScale = Time.timeScale;
        int savedQuality = PlayerPrefs.GetInt("timesands.fx_quality", -1);
        int savedMotion = PlayerPrefs.GetInt("timesands.motion", -1);
        var random = UnityEngine.Random.state;
        try
        {
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Reduced, false);
            float reduced = PresentationSettings.ParticleIntensity;
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Balanced, false);
            float balanced = PresentationSettings.ParticleIntensity;
            PresentationSettings.SetQuality(PresentationSettings.EffectsQuality.Cinematic, false);
            float cinematic = PresentationSettings.ParticleIntensity;
            Require(reduced > 0 && reduced < balanced && balanced < cinematic && cinematic <= 1,
                "Particle budget levels must be ordered, bounded and nonzero.");
            PresentationSettings.SetQuality((PresentationSettings.EffectsQuality)99, false);
            Require(PresentationSettings.Quality == PresentationSettings.EffectsQuality.Cinematic,
                "Invalid preferences need a valid fallback.");
            PresentationSettings.SetMotion(false, false);
            Require(!PresentationSettings.MotionEnabled, "Reduced motion did not apply.");
            Require(Time.timeScale == timeScale, "Presentation options must not change game speed.");
            Require(PlayerPrefs.GetInt("timesands.fx_quality", -1) == savedQuality
                && PlayerPrefs.GetInt("timesands.motion", -1) == savedMotion,
                "Nonpersistent preview settings altered saved preferences.");
            float observed = UnityEngine.Random.value;
            UnityEngine.Random.state = random;
            Require(observed == UnityEngine.Random.value, "Presentation preferences consumed gameplay RNG.");
        }
        finally
        {
            PresentationSettings.SetQuality(quality, false);
            PresentationSettings.SetMotion(motion, false);
            UnityEngine.Random.state = random;
        }
    }

    static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
