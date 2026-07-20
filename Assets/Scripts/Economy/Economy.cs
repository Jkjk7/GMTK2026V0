using System;
using UnityEngine;

/// <summary>
/// 全局金币经济：整数金币、入账/扣费、不足事件。
/// </summary>
public class Economy : MonoBehaviour
{
    public static Economy Instance { get; private set; }

    public const int StartingGold = 50;

    [SerializeField] int currentGold = StartingGold;

    public int CurrentGold => currentGold;

    public event Action<int> OnGoldChanged;
    public event Action OnGoldInsufficient;
    public event Action<int> OnGoldGained;
    public event Action<int> OnGoldSpent;

    void Awake()
    {
        Instance = this;
        currentGold = StartingGold;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResetToStart()
    {
        currentGold = StartingGold;
        OnGoldChanged?.Invoke(currentGold);
    }

    public void AddGold(int amount, bool silent = false)
    {
        if (amount <= 0)
        {
            return;
        }

        currentGold += amount;
        if (!silent)
        {
            OnGoldGained?.Invoke(amount);
        }

        OnGoldChanged?.Invoke(currentGold);
    }

    public bool CanAfford(int cost) => cost <= 0 || currentGold >= cost;

    public bool TrySpend(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (currentGold < cost)
        {
            OnGoldInsufficient?.Invoke();
            return false;
        }

        currentGold -= cost;
        OnGoldSpent?.Invoke(cost);
        OnGoldChanged?.Invoke(currentGold);
        return true;
    }

    /// <summary>仅触发不足反馈（不尝试扣费）。</summary>
    public void NotifyInsufficient()
    {
        OnGoldInsufficient?.Invoke();
    }
}
