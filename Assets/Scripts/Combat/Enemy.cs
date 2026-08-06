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
    float _freezeTimer;
    float _burnTimer;
    float _burnTickAcc;
    float _hitFlashTimer;
    Vector3 _externalPull;
    float _shieldTimer;
    bool _moveLocked;
    const int ShieldBlockThreshold = 30;
    SpriteRenderer _shieldAura;
    SpriteRenderer _shieldRing;

    BattleLane _lane;
    Mage _mage;
    WaveManager _waveManager;
    SpriteRenderer _visual;

    public bool IsAlive => _alive;
    public bool HasSandBuff => _sandBuff;
    public bool IsFrozen => _freezeTimer > 0f;
    public bool IsBurning => _burnTimer > 0f;
    public bool IsChilled => _slowTimer > 0f && _slowPercent > 0f;
    public EnemyGoldType GoldType => goldType;
    public int MaxHitPoints => maxHitPoints;
    public int CurrentHitPoints => _currentHp;

    EnemyHpBar _hpBar;

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
        _freezeTimer = 0f;
        _burnTimer = 0f;
        _burnTickAcc = 0f;
        _hitFlashTimer = 0f;
        EnsureVisual();
        _hpBar = EnemyHpBar.Attach(this, goldType == EnemyGoldType.Boss);
        _hpBar?.Refresh(_currentHp, maxHitPoints);

        if (goldType == EnemyGoldType.Disassembler || goldType == EnemyGoldType.ShieldCaster)
        {
            var caster = GetComponent<EnemyCasterAbility>();
            if (caster == null)
            {
                caster = gameObject.AddComponent<EnemyCasterAbility>();
            }

            caster.Initialize(this, goldType);
        }
    }

    public void SetMoveLocked(bool locked)
    {
        _moveLocked = locked;
    }

    public bool HasDamageShield => _shieldTimer > 0f;

    public void ApplyDamageShield(float durationSeconds)
    {
        if (!_alive || durationSeconds <= 0f)
        {
            return;
        }

        _shieldTimer = Mathf.Max(_shieldTimer, durationSeconds);
        EnsureShieldFx();
        SetShieldFxVisible(true);
        RefreshDisplayColor();
    }

    public void ClearDamageShield(bool shatter = false)
    {
        if (_shieldTimer <= 0f)
        {
            return;
        }

        Vector3 pos = transform.position;
        float size = Mathf.Max(0.6f, transform.localScale.x);
        _shieldTimer = 0f;
        SetShieldFxVisible(false);
        RefreshDisplayColor();
        if (shatter)
        {
            CombatVfxService.SpawnShieldShatter(pos, size);
        }
    }

    public static void ClearAllDamageShields(bool shatter = false)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i]?.ClearDamageShield(shatter);
        }
    }

    public static void ApplyShieldToAllAlive(float durationSeconds)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e != null && e.IsAlive)
            {
                e.ApplyDamageShield(durationSeconds);
            }
        }
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
            case EnemyGoldType.Disassembler:
            case EnemyGoldType.ShieldCaster:
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

    public void ApplyFreeze(float duration)
    {
        if (!_alive || duration <= 0f)
        {
            return;
        }

        bool wasFrozen = IsFrozen;
        _freezeTimer = Mathf.Max(_freezeTimer, duration);
        if (!wasFrozen)
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

        if (_freezeTimer > 0f)
        {
            _freezeTimer -= Time.deltaTime;
            if (_freezeTimer <= 0f)
            {
                _freezeTimer = 0f;
                RefreshDisplayColor();
            }
        }

        TickBurn();

        if (_shieldTimer > 0f)
        {
            _shieldTimer -= Time.deltaTime;
            PulseShieldFx();
            if (_shieldTimer <= 0f)
            {
                _shieldTimer = 0f;
                SetShieldFxVisible(false);
                RefreshDisplayColor();
            }
        }

        float haste = RunModifiers.Instance != null ? RunModifiers.Instance.EnemySpeedMult : 1f;
        float speed = (_moveLocked || IsFrozen) ? 0f : _baseMoveSpeed * haste * (1f - _slowPercent);
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

        if (_shieldTimer > 0f)
        {
            if (amount <= ShieldBlockThreshold)
            {
                FlashHit();
                return;
            }

            ClearDamageShield(shatter: true);
        }

        _currentHp = Mathf.Max(0, _currentHp - amount);
        if (DamageTracker.Instance != null)
        {
            DamageTracker.Instance.AddDamage(amount);
        }

        DamageNumberPopup.TrySpawn(transform.position, amount);
        FlashHit();
        _hpBar?.Refresh(_currentHp, maxHitPoints);

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

    /// <summary>开发者跳波等：按正常击杀结算金币/爆沙。</summary>
    public void KillForReward()
    {
        Die();
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
        if (goldType == EnemyGoldType.ShieldCaster)
        {
            ClearAllDamageShields(shatter: true);
        }

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

    void EnsureShieldFx()
    {
        if (_shieldAura != null)
        {
            return;
        }

        var root = new GameObject("GoldShieldFx");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;

        var auraGo = new GameObject("Aura");
        auraGo.transform.SetParent(root.transform, false);
        _shieldAura = auraGo.AddComponent<SpriteRenderer>();
        _shieldAura.sprite = PrototypeSprites.Circle;
        _shieldAura.color = new Color(1f, 0.9f, 0.4f, 0.12f);
        _shieldAura.sortingOrder = 13;

        var ringGo = new GameObject("Ring");
        ringGo.transform.SetParent(root.transform, false);
        _shieldRing = ringGo.AddComponent<SpriteRenderer>();
        _shieldRing.sprite = PrototypeSprites.Circle;
        _shieldRing.color = new Color(1f, 0.95f, 0.6f, 0.28f);
        _shieldRing.sortingOrder = 14;
    }

    void SetShieldFxVisible(bool visible)
    {
        if (!visible)
        {
            if (_shieldAura != null)
            {
                _shieldAura.gameObject.SetActive(false);
            }

            if (_shieldRing != null)
            {
                _shieldRing.gameObject.SetActive(false);
            }

            return;
        }

        EnsureShieldFx();
        if (_shieldAura != null)
        {
            _shieldAura.gameObject.SetActive(true);
        }

        if (_shieldRing != null)
        {
            _shieldRing.gameObject.SetActive(true);
        }

        PulseShieldFx();
    }

    void PulseShieldFx()
    {
        if (!HasDamageShield || _shieldAura == null)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 9f);
        float aura = 1.7f + 0.12f * pulse;
        float ring = 2.05f + 0.16f * pulse;
        _shieldAura.transform.localScale = new Vector3(aura, aura, 1f);
        Color ac = _shieldAura.color;
        ac.a = 0.08f + 0.07f * pulse;
        _shieldAura.color = ac;

        if (_shieldRing != null)
        {
            _shieldRing.transform.localScale = new Vector3(ring, ring, 1f);
            Color rc = _shieldRing.color;
            rc.a = 0.18f + 0.12f * pulse;
            _shieldRing.color = rc;
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
            case EnemyGoldType.Disassembler:
                return new Color(0.7f, 0.25f, 0.95f, 1f);
            case EnemyGoldType.ShieldCaster:
                return new Color(1f, 0.82f, 0.2f, 1f);
            case EnemyGoldType.Boss:
                return new Color(1f, 0.45f, 0.08f, 1f);
            default:
                return new Color(0.85f, 0.25f, 0.3f, 1f);
        }
    }

    Color GetDisplayColor()
    {
        Color baseColor = GetTypeColor();
        if (HasDamageShield)
        {
            baseColor = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.55f, 1f), 0.22f);
        }
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

        if (IsFrozen)
        {
            Color freezeTint = new Color(0.82f, 0.98f, 1f, 1f);
            baseColor = Color.Lerp(baseColor, freezeTint, 0.85f);
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

        _visual.sprite = goldType == EnemyGoldType.Tank || goldType == EnemyGoldType.Disassembler
            ? PrototypeSprites.Square
            : PrototypeSprites.Circle;
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
        else if (goldType == EnemyGoldType.Tank
                 || goldType == EnemyGoldType.Disassembler
                 || goldType == EnemyGoldType.ShieldCaster)
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
