using UnityEngine;

/// <summary>
/// 终点法师：仅作为防线终点的占位视觉与位置锚点。
/// 漏怪不再清屏扣命，改为按敌人类型向 SandClock 罚沙。
/// </summary>
public class Mage : MonoBehaviour
{
    SpriteRenderer _visual;

    /// <summary>
    /// 放置到路线最左端。
    /// </summary>
    public void Initialize(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        EnsureVisual();
    }

    /// <summary>
    /// 敌人到达终点：按类型罚沙（不清屏）。
    /// </summary>
    public void OnEnemyBreach(EnemyGoldType type, bool sandBuff = false)
    {
        SandClock.Instance?.ApplyBreachPenalty(type, sandBuff);
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
        _visual.color = new Color(0.35f, 0.55f, 0.95f, 1f);
        _visual.sortingOrder = 9;
        transform.localScale = new Vector3(0.85f, 0.85f, 1f);
    }
}
