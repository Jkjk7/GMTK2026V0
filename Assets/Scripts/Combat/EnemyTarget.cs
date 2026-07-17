using UnityEngine;

/// <summary>
/// 战斗区静止敌人靶子。
/// 职责：接收伤害、通知 DamageTracker；本原型不移动、不反击。
/// </summary>
public class EnemyTarget : MonoBehaviour
{
    [SerializeField] int hitPoints = 999999;
    [SerializeField] int currentHp;

    SpriteRenderer _visual;

    /// <summary>当前生命值（可选显示）。</summary>
    public int CurrentHp => currentHp;

    void Awake()
    {
        currentHp = hitPoints;
        EnsureVisual();
    }

    /// <summary>
    /// 由 GameBootstrap 放置到战斗区右上角时调用。
    /// </summary>
    public void Initialize(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        currentHp = hitPoints;
        EnsureVisual();
    }

    /// <summary>
    /// 受到伤害；累计值交给 DamageTracker。
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentHp = Mathf.Max(0, currentHp - amount);
        if (DamageTracker.Instance != null)
        {
            DamageTracker.Instance.AddDamage(amount);
        }

        // 受击闪一下
        if (_visual != null)
        {
            _visual.color = Color.white;
            CancelInvoke(nameof(RestoreColor));
            Invoke(nameof(RestoreColor), 0.05f);
        }
    }

    void RestoreColor()
    {
        if (_visual != null)
        {
            _visual.color = new Color(0.85f, 0.25f, 0.3f, 1f);
        }
    }

    void EnsureVisual()
    {
        if (_visual == null)
        {
            _visual = GetComponent<SpriteRenderer>();
            if (_visual == null)
            {
                _visual = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        _visual.sprite = PrototypeSprites.Square;
        _visual.color = new Color(0.85f, 0.25f, 0.3f, 1f);
        _visual.sortingOrder = 10;
        transform.localScale = new Vector3(0.9f, 0.9f, 1f);
    }
}
