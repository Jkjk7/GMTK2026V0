using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全局伤害累计与 HUD 文本。
/// 单例便于射弹模块上报；也可改为事件总线，原型用单例足够。
/// </summary>
public class DamageTracker : MonoBehaviour
{
    public static DamageTracker Instance { get; private set; }

    [SerializeField] Text damageText;

    int _totalDamage;

    /// <summary>本局累计伤害。</summary>
    public int TotalDamage => _totalDamage;

    void Awake()
    {
        Instance = this;
        RefreshLabel();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 绑定 UI Text（由 GameBootstrap 创建 Canvas 后注入）。
    /// </summary>
    public void BindLabel(Text label)
    {
        damageText = label;
        RefreshLabel();
    }

    /// <summary>
    /// 累加伤害并刷新显示。
    /// </summary>
    public void AddDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        _totalDamage += amount;
        RefreshLabel();
    }

    void RefreshLabel()
    {
        if (damageText != null)
        {
            damageText.text = $"Total Damage: {_totalDamage}";
        }
    }
}
