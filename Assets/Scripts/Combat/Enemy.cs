using UnityEngine;

/// <summary>
/// 战斗区移动敌人。沙 buff 可附着在普通怪上：约 1.5× 血/漏伤，击杀爆沙，发光闪烁。
/// </summary>
public class Enemy : MonoBehaviour
{
    [SerializeField] int maxHitPoints = 10;
    [SerializeField] float moveSpeed = 1.5f;
    [SerializeField] EnemyGoldType goldType = EnemyGoldType.Normal;

    int _currentHp;
    bool _alive = true;
    bool _sandBuff;
    float _baseMoveSpeed = 1.5f;
    float _baseScale = 0.9f;
    float _slowPercent;
    float _slowTimer;
    float _burnTimer;
    float _burnTickAcc;
    float _hitFlashTimer;
    Vector3 _externalPull;

    BattleLane _lane;
    Mage _mage;
    WaveManager _waveManager;
    SpriteRenderer _visual;

    public bool IsAlive => _alive;
    public bool HasSandBuff => _sandBuff;
    public bool IsBurning => _burnTimer > 0f;
    public bool IsChilled => _slowTimer > 0f && _slowPercent > 0f;
    public EnemyGoldType GoldType => goldType;
    public int MaxHitPoints => maxHitPoints;
    public int CurrentHitPoints => _currentHp;

    public void Initialize(
        BattleLane lane,
        Mage mage,
        WaveManager waveManager,
        int waveDisplay,
        EnemyGoldType type = EnemyGoldType.Normal,
        bool sandBuff = false)
    {
        _lane = lane;
        _mage = mage;
        _waveManager = waveManager;
        goldType = type;
        _sandBuff = sandBuff && type != EnemyGoldType.Boss;
        ApplyTypeStats(type, waveDisplay);
        _currentHp = maxHitPoints;
        _alive = true;
        _slowPercent = 0f;
        _slowTimer = 0f;
        _burnTimer = 0f;
        _burnTickAcc = 0f;
        _hitFlashTimer = 0f;
        EnsureVisual();
    }

    void ApplyTypeStats(EnemyGoldType type, int waveDisplay)
    {
        maxHitPoints = WaveSpawnBudget.GetHitPoints(waveDisplay, type, _sandBuff);
        switch (type)
        {
            case EnemyGoldType.Swarm:
                _baseMoveSpeed = 3.0f;
                break;
            case EnemyGoldType.Tank:
            case EnemyGoldType.Boss:
                _baseMoveSpeed = 0.75f;
                break;
            default:
                _baseMoveSpeed = 1.5f;
                break;
        }

        moveSpeed = _baseMoveSpeed;
    }

    public void ApplySlow(float percent, float duration)
    {
        if (!_alive)
        {
            return;
        }

        bool wasSlowed = IsChilled;
        _slowPercent = Mathf.Clamp01(Mathf.Max(_slowPercent, percent));
        _slowTimer = Mathf.Max(_slowTimer, duration);
        if (!wasSlowed)
        {
            RefreshDisplayColor();
        }
    }

    /// <summary>
    /// 灼烧：剩余更长则保留且不重置 tick；否则延长到 duration 并保持 tick 推进。
    /// </summary>
    public void ApplyBurn(float durationSeconds)
    {
        if (!_alive || durationSeconds <= 0f)
        {
            return;
        }

        bool wasBurning = IsBurning;
        if (_burnTimer > durationSeconds)
        {
            return;
        }

        _burnTimer = durationSeconds;
        if (!wasBurning)
        {
            _burnTickAcc = 0f;
            RefreshDisplayColor();
        }
    }

    public void ClearBurn()
    {
        if (_burnTimer <= 0f)
        {
            return;
        }

        _burnTimer = 0f;
        _burnTickAcc = 0f;
        RefreshDisplayColor();
    }

    public void ClearChill()
    {
        if (!IsChilled)
        {
            _slowTimer = 0f;
            _slowPercent = 0f;
            return;
        }

        _slowTimer = 0f;
        _slowPercent = 0f;
        RefreshDisplayColor();
    }

    public void ClearBurnAndChill()
    {
        bool changed = IsBurning || IsChilled;
        _burnTimer = 0f;
        _burnTickAcc = 0f;
        _slowTimer = 0f;
        _slowPercent = 0f;
        if (changed)
        {
            RefreshDisplayColor();
        }
    }

    public void ApplyExternalPull(Vector3 delta)
    {
        if (!_alive)
        {
            return;
        }

        _externalPull += delta;
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

        if (_slowTimer > 0f)
        {
            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f)
            {
                _slowPercent = 0f;
                RefreshDisplayColor();
            }
        }

        TickBurn();

        float haste = RunModifiers.Instance != null ? RunModifiers.Instance.EnemySpeedMult : 1f;
        float speed = _baseMoveSpeed * haste * (1f - _slowPercent);
        float newX = transform.position.x - speed * Time.deltaTime;
        Vector3 pos = new Vector3(newX, _lane.LaneY, 0f);
        if (_externalPull.sqrMagnitude > 0.000001f)
        {
            pos += _externalPull;
            pos.y = _lane.LaneY;
            _externalPull = Vector3.zero;
        }

        transform.position = pos;

        if (_hitFlashTimer > 0f)
        {
            _hitFlashTimer -= Time.deltaTime;
            if (_visual != null)
            {
                _visual.color = Color.white;
            }
        }
        else if (_sandBuff)
        {
            PulseSandVisual();
        }
        else if (IsBurning)
        {
            PulseBurnVisual();
        }

        if (pos.x <= _lane.EndX)
        {
            ReachMage();
        }
    }

    void TickBurn()
    {
        if (_burnTimer <= 0f)
        {
            return;
        }

        float interval = RunModifiers.BurnTickInterval;
        _burnTimer -= Time.deltaTime;
        _burnTickAcc += Time.deltaTime;
        while (_burnTickAcc >= interval && _alive)
        {
            _burnTickAcc -= interval;
            int tick = RunModifiers.Instance != null
                ? RunModifiers.Instance.GetBurnDamagePerTick()
                : RunModifiers.BaseBurnDamagePerTick;
            TakeDamage(Mathf.Max(1, tick));
            if (!_alive)
            {
                return;
            }
        }

        if (_burnTimer <= 0f)
        {
            _burnTimer = 0f;
            _burnTickAcc = 0f;
            if (_hitFlashTimer <= 0f)
            {
                RefreshDisplayColor();
            }
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
        _mage?.OnEnemyBreach(goldType, _sandBuff);
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
        CombatVfxService.SpawnDeath(pos);
        int gold = WaveGoldBudget.Instance != null
            ? WaveGoldBudget.Instance.RollKillGold(goldType)
            : 0;
        if (gold > 0 && GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(gold, pos);
        }

        if (_sandBuff)
        {
            int burst = SandClock.GetSandBuffBurstMs(
                SandClock.Instance != null ? SandClock.Instance.CurrentWaveDisplay : 1);
            if (SandVfxService.Instance != null)
            {
                SandVfxService.Instance.GrantSandWithFly(burst, pos);
            }
            else
            {
                SandClock.Instance?.GrantKillSand(goldType, true);
            }
        }

        _waveManager?.UnregisterEnemy(this);
        Destroy(gameObject);
    }

    void FlashHit()
    {
        CombatVfxService.SpawnHit(transform.position);
        if (_visual == null)
        {
            return;
        }

        _hitFlashTimer = 0.1f;
        _visual.color = Color.white;
        CancelInvoke(nameof(EndHitFlash));
        Invoke(nameof(EndHitFlash), 0.1f);
    }

    void EndHitFlash()
    {
        _hitFlashTimer = 0f;
        RefreshDisplayColor();
    }

    void RefreshDisplayColor()
    {
        if (_visual == null || _hitFlashTimer > 0f)
        {
            return;
        }

        if (!_sandBuff)
        {
            _visual.color = GetDisplayColor();
        }
    }

    Color GetTypeColor()
    {
        switch (goldType)
        {
            case EnemyGoldType.Swarm:
                return new Color(0.95f, 0.85f, 0.2f, 1f);
            case EnemyGoldType.Tank:
                return new Color(0.35f, 0.55f, 0.95f, 1f);
            case EnemyGoldType.Boss:
                return new Color(1f, 0.45f, 0.08f, 1f);
            default:
                return new Color(0.85f, 0.25f, 0.3f, 1f);
        }
    }

    Color GetDisplayColor()
    {
        Color baseColor = GetTypeColor();
        if (_sandBuff)
        {
            Color sand = new Color(0.55f, 1f, 0.92f, 1f);
            baseColor = Color.Lerp(baseColor, sand, 0.55f);
        }

        if (IsBurning)
        {
            Color burn = new Color(1f, 0.35f, 0.08f, 1f);
            baseColor = Color.Lerp(baseColor, burn, _sandBuff ? 0.4f : 0.62f);
        }

        if (IsChilled)
        {
            Color iceTint = new Color(0.35f, 0.95f, 1f, 1f);
            baseColor = Color.Lerp(baseColor, iceTint, _sandBuff || IsBurning ? 0.4f : 0.72f);
        }

        return baseColor;
    }

    void PulseSandVisual()
    {
        if (_visual == null)
        {
            return;
        }

        float pulse = 0.55f + 0.45f * (0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
        Color c = GetDisplayColor();
        Color hot = new Color(1f, 1f, 0.8f, 1f);
        _visual.color = Color.Lerp(c, hot, pulse * 0.7f);
        float s = _baseScale * (1f + 0.1f * Mathf.Sin(Time.time * 6f));
        transform.localScale = new Vector3(s, s, 1f);
    }

    void PulseBurnVisual()
    {
        if (_visual == null)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * (0.5f + 0.5f * Mathf.Sin(Time.time * 10f));
        Color c = GetDisplayColor();
        Color hot = new Color(1f, 0.85f, 0.25f, 1f);
        _visual.color = Color.Lerp(c, hot, pulse * 0.55f);
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
        RefreshDisplayColor();
        if (_sandBuff)
        {
            _visual.color = GetDisplayColor();
        }

        _visual.sortingOrder = goldType == EnemyGoldType.Boss ? 12 : (_sandBuff ? 11 : 10);
        float s = 0.9f;
        if (goldType == EnemyGoldType.Swarm)
        {
            s = 0.4f;
        }
        else if (goldType == EnemyGoldType.Tank)
        {
            s = 1.25f;
        }
        else if (goldType == EnemyGoldType.Boss)
        {
            s = 2.8f;
        }

        if (_sandBuff)
        {
            s *= 1.12f;
        }

        _baseScale = s;
        transform.localScale = new Vector3(s, s, 1f);

        var clockwork = GetComponent<EnemyVisualController>();
        if (clockwork == null)
        {
            clockwork = gameObject.AddComponent<EnemyVisualController>();
        }
        clockwork.Initialize(this);
    }
}
