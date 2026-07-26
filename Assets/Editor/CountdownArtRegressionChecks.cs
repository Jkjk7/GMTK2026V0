using System;
using UnityEditor;
using UnityEngine;

/// <summary>Batch-mode checks for the procedural countdown art fallback.</summary>
public static class CountdownArtRegressionChecks
{
    public static void Run()
    {
        try
        {
            Require(CountdownVisualRules.TickCount == 60, "Countdown ring must contain exactly 60 ticks.");
            Require(CountdownVisualRules.GetLitTickCount(-1, 100) == 0, "Negative ratios must clamp empty.");
            Require(CountdownVisualRules.GetLitTickCount(250, 100) == 60, "Overfill ratios must clamp full.");
            Require(CountdownVisualRules.GetLitTickCount(50, 100) == 30, "Half time must light 30 ticks.");
            Require(CountdownVisualRules.IsWarning(20_000), "20,000 ms must enter warning state.");
            Require(!CountdownVisualRules.IsWarning(20_001), "20,001 ms must remain normal.");

            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                Require(ModuleSkinApplicator.HasStyle(type), $"Missing module visual style for {type}.");
            }

            Require(SandClock.InitialSandMs == 100_000, "Initial sand gameplay constant changed.");
            Require(SandClock.BreachPenaltySwarmMs == 3_000, "Swarm penalty gameplay constant changed.");
            Require(SandClock.BreachPenaltyNormalMs == 10_000, "Normal penalty gameplay constant changed.");
            Require(SandClock.BreachPenaltyTankMs == 30_000, "Tank penalty gameplay constant changed.");
            Debug.Log("[Countdown Art Regression] PASS");
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
