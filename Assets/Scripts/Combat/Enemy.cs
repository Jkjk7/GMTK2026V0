using UnityEngine;

/// <summary>
/// 战斗区移动敌人：向右→左移动，受击死亡，到达魔法师触发漏怪。
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] int maxHitPoints = 40;
    [SerializeField] float moveSpeed = 1.5f;

    int _currentHp;
    bool _alive = true;

    BattleLane _lane;
    Mage _mage;
    WaveManager _waveManager;
    SpriteRenderer _visual;

    public bool IsAlive => _alive;

    /// <summary>
    /// 由 WaveManager 刷怪时调用。
    /// </summary>
    public void Initialize(BattleLane lane, Mage mage, WaveManager waveManager)
    {
        _lane = lane;
        _mage = mage;
        _waveManager = waveManager;
        _currentHp = maxHitPoints;
        _alive = true;
        EnsureVisual();
    }

    void Update()
    {
        if (!_alive || _lane == null)
        {
            return;
        }

        if (GameSession.Instance != null && !GameSession.Instance.IsPlaying)
        {
            return;
        }

        float newX = transform.position.x - moveSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, _lane.LaneY, 0f);

        if (newX <= _lane.EndX)
        {
            ReachMage();
        }
    }

    /// <summary>
    /// 受到伤害；HP 归零则死亡。
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (!_alive || amount <= 0)
        {
            return;
        }

        _currentHp = Mathf.Max(0, _currentHp - amount);
        if (DamageTracker.Instance != null)
        {
            DamageTracker.Instance.AddDamage(amount);
        }

        FlashHit();

        if (_currentHp <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 漏怪清屏时强制移除，不触发漏怪逻辑。
    /// </summary>
    public void ForceDespawn()
    {
        if (!_alive)
        {
            return;
        }

        _alive = false;
        _waveManager?.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    void ReachMage()
    {
        if (!_alive)
        {
            return;
        }

        _alive = false;
        _mage?.OnEnemyBreach();
        _waveManager?.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    void Die()
    {
        if (!_alive)
        {
            return;
        }

        _alive = false;
        _waveManager?.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    void FlashHit()
    {
        if (_visual == null)
        {
            return;
        }

        _visual.color = Color.white;
        CancelInvoke(nameof(RestoreColor));
        Invoke(nameof(RestoreColor), 0.05f);
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
