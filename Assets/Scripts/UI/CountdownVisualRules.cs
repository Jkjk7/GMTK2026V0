using UnityEngine;

public static class CountdownVisualRules
{
    public const int TickCount = 60;
    public const int WarningThresholdMs = 20_000;

    public static float GetRatio(int remainingMs, int fullMs) =>
        fullMs <= 0 ? 0f : Mathf.Clamp01(remainingMs / (float)fullMs);

    public static int GetLitTickCount(int remainingMs, int fullMs) =>
        Mathf.Clamp(Mathf.CeilToInt(GetRatio(remainingMs, fullMs) * TickCount), 0, TickCount);

    public static bool IsWarning(int remainingMs) => remainingMs <= WarningThresholdMs;
}
