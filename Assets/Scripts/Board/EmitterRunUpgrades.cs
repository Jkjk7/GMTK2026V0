using UnityEngine;

public enum EmitterUpgradeKind
{
    FireRate = 0,
    BallSpeed = 1,
    Mass = 2,
    Lifetime = 3
}

/// <summary>
/// 本局熔炉四维升级。档位偏保守，避免后期球洪水。
/// FireRate 档 = 熔炉容量（毫秒）：容量越小攒沙越快出球。
/// 质量 = 球能量值（进入模块时加算）。
/// </summary>
public class EmitterRunUpgrades : MonoBehaviour
{
    public const int TierCount = 4;
    public const int MaxLevel = TierCount - 1;

    // 熔炉容量（毫秒）：2000/1400/1050/800 ≈ 0.5/0.7/0.95/1.25 球每秒
    public static readonly int[] FurnaceCapsMs = { 2000, 1400, 1050, 800 };
    public static readonly float[] BallSpeedCells = { 4.0f, 5.5f, 7.0f, 8.5f };
    public static readonly int[] MassEnergy = { 1, 2, 3, 4 }; // 削弱：原 1/2/5/10 太强
    public static readonly float[] LifetimeSeconds = { 12f, 20f, 32f, 50f };

    public static EmitterRunUpgrades Instance { get; private set; }

    int _fireRateLevel;
    int _ballSpeedLevel;
    int _massLevel;
    int _lifetimeLevel;

    public int FireRateLevel => _fireRateLevel;
    public int BallSpeedLevel => _ballSpeedLevel;
    public int MassLevel => _massLevel;
    public int LifetimeLevel => _lifetimeLevel;

    public int FurnaceCapMs => FurnaceCapsMs[_fireRateLevel];
    public float BallSpeed => BallSpeedCells[_ballSpeedLevel];
    public int Mass => MassEnergy[_massLevel];
    public float Lifetime => LifetimeSeconds[_lifetimeLevel];

    void Awake()
    {
        Instance = this;
        ResetRun();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResetRun()
    {
        _fireRateLevel = 0;
        _ballSpeedLevel = 0;
        _massLevel = 0;
        _lifetimeLevel = 0;
    }

    public int GetLevel(EmitterUpgradeKind kind)
    {
        switch (kind)
        {
            case EmitterUpgradeKind.FireRate: return _fireRateLevel;
            case EmitterUpgradeKind.BallSpeed: return _ballSpeedLevel;
            case EmitterUpgradeKind.Mass: return _massLevel;
            default: return _lifetimeLevel;
        }
    }

    public bool CanUpgrade(EmitterUpgradeKind kind) => GetLevel(kind) < MaxLevel;

    public bool TryUpgrade(EmitterUpgradeKind kind)
    {
        if (!CanUpgrade(kind))
        {
            return false;
        }

        switch (kind)
        {
            case EmitterUpgradeKind.FireRate:
                _fireRateLevel++;
                break;
            case EmitterUpgradeKind.BallSpeed:
                _ballSpeedLevel++;
                break;
            case EmitterUpgradeKind.Mass:
                _massLevel++;
                break;
            case EmitterUpgradeKind.Lifetime:
                _lifetimeLevel++;
                break;
        }

        return true;
    }

    public string GetDisplayName(EmitterUpgradeKind kind)
    {
        switch (kind)
        {
            case EmitterUpgradeKind.FireRate: return GameLocalization.Text("Furnace Capacity", "熔炉容量");
            case EmitterUpgradeKind.BallSpeed: return GameLocalization.Text("Ball Speed", "球速");
            case EmitterUpgradeKind.Mass: return GameLocalization.Text("Mass", "质量");
            default: return GameLocalization.Text("Lifetime", "存活");
        }
    }

    public string FormatOptionLabel(EmitterUpgradeKind kind)
    {
        int lv = GetLevel(kind);
        int next = lv + 1;
        string name = GetDisplayName(kind);
        switch (kind)
        {
            case EmitterUpgradeKind.FireRate:
                return GameLocalization.Text(
                    $"{name}\n{FurnaceCapsMs[lv]} → {FurnaceCapsMs[next]}ms\nCrystallize balls faster",
                    $"{name}\n{FurnaceCapsMs[lv]} → {FurnaceCapsMs[next]}ms\n攒沙更快结晶出球");
            case EmitterUpgradeKind.BallSpeed:
                return GameLocalization.Text(
                    $"{name}\n{BallSpeedCells[lv]:0.#} → {BallSpeedCells[next]:0.#} cells/s\nBalls fly faster",
                    $"{name}\n{BallSpeedCells[lv]:0.#} → {BallSpeedCells[next]:0.#} 格/秒\n球飞得更快");
            case EmitterUpgradeKind.Mass:
                return GameLocalization.Text(
                    $"{name}\nEnergy {MassEnergy[lv]} → {MassEnergy[next]}\nMore charge per ball",
                    $"{name}\n能量 {MassEnergy[lv]} → {MassEnergy[next]}\n单球充能更多");
            default:
                return GameLocalization.Text(
                    $"{name}\n{LifetimeSeconds[lv]:0} → {LifetimeSeconds[next]:0}s\nBalls last longer",
                    $"{name}\n{LifetimeSeconds[lv]:0} → {LifetimeSeconds[next]:0} 秒\n球更耐用");
        }
    }
}
