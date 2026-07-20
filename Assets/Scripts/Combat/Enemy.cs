using UnityEngine;

/// <summary>
/// 战斗区移动敌人：类型掉落金币；清屏不掉落。
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] int maxHitPoints = 10;
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] EnemyGoldType goldType = EnemyGoldType.Normal;

    int _currentHp;
    bool _alive = true;

    BattleLane _lane;
    Mage _mage;
    WaveManager _waveManager;
    SpriteRenderer _visual;

    public bool IsAlive => _alive;
    public EnemyGoldType GoldType => goldType;

    public void Initialize(BattleLane lane, Mage mage, WaveManager waveManager, EnemyGoldType type = EnemyGoldType.Normal)
    {
        _lane = lane;
        _mage = mage;
        _waveManager = waveManager;
        goldType = type;
        if (type == EnemyGoldType.Swarm)
        {
            maxHitPoints = 1;
            moveSpeed = 2.2f;
        }

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

        if (GameSession.Instance != null && !GameSession.Instance.IsCombatActive)
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
        WaveGoldBudget.Instance?.NotifyBreach();
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
        Vector3 pos = transform.position;
        int gold = WaveGoldBudget.Instance != null
            ? WaveGoldBudget.Instance.RollKillGold(goldType)
            : 0;
        if (gold > 0 && GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(gold, pos);
        }

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
            _visual.color = goldType == EnemyGoldType.Swarm
                ? new Color(0.95f, 0.55f, 0.35f, 1f)
                : new Color(0.85f, 0.25f, 0.3f, 1f);
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
        RestoreColor();
        _visual.sortingOrder = 10;
        float s = goldType == EnemyGoldType.Swarm ? 0.45f : 0.9f;
        transform.localScale = new Vector3(s, s, 1f);
    }
}
