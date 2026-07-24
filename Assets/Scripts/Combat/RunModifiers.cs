using System;
using UnityEngine;

/// <summary>
/// 本局祝福/束缚累计状态。
/// </summary>
public class RunModifiers : MonoBehaviour
{
    public static RunModifiers Instance { get; private set; }

    public float AoeRadiusMult { get; private set; } = 1f;
    public float EnemySpeedMult { get; private set; } = 1f;
    public bool NextExpandHalfPrice { get; private set; }
    public int BlessingTier { get; private set; }
    public int BurnDamageBonus { get; private set; }
    public int FlameAmpBonus { get; private set; }

    public event Action Changed;

    /// <summary>基础灼烧：每跳伤害。</summary>
    public const int BaseBurnDamagePerTick = 3;

    /// <summary>灼烧跳动间隔（秒）。</summary>
    public const float BurnTickInterval = 0.5f;

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
        AoeRadiusMult = 1f;
        EnemySpeedMult = 1f;
        NextExpandHalfPrice = false;
        BlessingTier = 0;
        BurnDamageBonus = 0;
        FlameAmpBonus = 0;
        NotifyChanged();
    }

    public void NotifyBlessingOffered()
    {
        BlessingTier = Mathf.Min(4, BlessingTier + 1);
    }

    public int CurrentTier => Mathf.Clamp(BlessingTier, 1, 4);

    public void ApplyBombRadiusBoost()
    {
        AoeRadiusMult = Mathf.Min(2.5f, AoeRadiusMult * 1.2f);
        NotifyChanged();
    }

    public void ApplyEnemyHaste()
    {
        EnemySpeedMult = Mathf.Min(1.5f, EnemySpeedMult * 1.08f);
        NotifyChanged();
    }

    public void GrantExpandHalfPrice()
    {
        NextExpandHalfPrice = true;
        NotifyChanged();
    }

    public bool TryConsumeExpandHalfPrice()
    {
        if (!NextExpandHalfPrice)
        {
            return false;
        }

        NextExpandHalfPrice = false;
        NotifyChanged();
        return true;
    }

    public void AddBurnDamageBonus(int amount)
    {
        BurnDamageBonus = Mathf.Max(0, BurnDamageBonus + amount);
        NotifyChanged();
    }

    public int GetBurnDamagePerTick() =>
        Mathf.Max(1, BaseBurnDamagePerTick + BurnDamageBonus + FlameAmpBonus);

    public void RecalcFlameAmp()
    {
        int sum = 0;
        FlameAmpModule[] amps = UnityEngine.Object.FindObjectsOfType<FlameAmpModule>();
        for (int i = 0; i < amps.Length; i++)
        {
            FlameAmpModule amp = amps[i];
            if (amp == null || !amp.isActiveAndEnabled || amp.BoundBoard == null)
            {
                continue;
            }

            sum += amp.BurnBonus;
        }

        if (sum == FlameAmpBonus)
        {
            return;
        }

        FlameAmpBonus = sum;
        NotifyChanged();
    }

    public void NotifyUiChanged()
    {
        NotifyChanged();
    }

    void NotifyChanged()
    {
        Changed?.Invoke();
    }
}
