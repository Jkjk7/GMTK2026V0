using UnityEngine;

/// <summary>
/// 查理激光塔（攻击输出）。
/// 职责：被光球命中时吸收能量；只要有能量，按射速消耗能量攻击最近（最左）敌人。
/// 有储能时头顶显示能量条。
/// </summary>
public class ProjectileModule : ModuleBase
{
    [Header("Energy")]
    [SerializeField] int energyCapacity = 10;
    [SerializeField] int currentEnergy;

    [Header("Firing")]
    [Tooltip("有能量时的开火间隔（秒）。")]
    [SerializeField] float fireInterval = 0.1f;

    [SerializeField] int baseDamagePerShot = 5;
    [SerializeField] int damagePerShot = 5;
    [SerializeField] int baseEnergyCapacity = 10;

    [Header("VFX")]
    [SerializeField] float trailVisibleSeconds = 0.08f;

    float _fireTimer;
    float _trailTimer;
    LineRenderer _line;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudBg;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;

    public override ModuleType ModuleType => global::ModuleType.Projectile;

    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => energyCapacity;
    public int DamagePerShot => damagePerShot;
    public float FireInterval => fireInterval;

    public void ClearEnergy()
    {
        currentEnergy = 0;
        ClearEnergyResidue();
        RefreshVisual();
    }

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        ApplyLevelStats(data.Level);
    }

    void ApplyLevelStats(int level)
    {
        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        damagePerShot = ModuleCatalog.GetDamagePerShot(lvl);
        energyCapacity = ModuleCatalog.GetEnergyCapacity(lvl);
        fireInterval = ModuleCatalog.GetFireInterval(lvl);
        currentEnergy = Mathf.Min(currentEnergy, energyCapacity);
        EnsureLevelLabel(lvl);
        RefreshVisual();
    }

    void EnsureLevelLabel(int level)
    {
        if (level <= 1)
        {
            if (_levelLabel != null)
            {
                _levelLabel.gameObject.SetActive(false);
            }

            return;
        }

        if (_levelLabel == null)
        {
            var go = new GameObject("LevelLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            go.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            _levelLabel = go.AddComponent<TextMesh>();
            _levelLabel.anchor = TextAnchor.MiddleCenter;
            _levelLabel.fontSize = 40;
            _levelLabel.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 12;
            }
        }

        _levelLabel.gameObject.SetActive(true);
        _levelLabel.text = $"Lv{level}";
    }

    void Awake()
    {
        EnsureVisual();
        EnsureLine();
        RefreshVisual();
    }

    void Update()
    {
        UpdateTrailFade();

        if (currentEnergy <= 0)
        {
            // 能量不足时保持就绪：补能后立刻开火
            _fireTimer = fireInterval * EnchantFireIntervalMultiplier;
            return;
        }

        if (FindLeftmostEnemy() == null)
        {
            return;
        }

        float interval = fireInterval * EnchantFireIntervalMultiplier;
        _fireTimer += Time.deltaTime;
        while (_fireTimer >= interval && currentEnergy > 0)
        {
            _fireTimer -= interval;
            ConsumeEnergyAndFire();
        }
    }

    /// <summary>
    /// 光球进入：吸收能量（不超过容量）。
    /// </summary>
    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        currentEnergy = AbsorbBallEnergy(ball, currentEnergy, energyCapacity);
        RefreshVisual();
    }

    /// <summary>
    /// 消耗 1 点能量，向最左侧敌人开火并造成伤害。
    /// </summary>
    void ConsumeEnergyAndFire()
    {
        Enemy target = FindLeftmostEnemy();
        if (target == null)
        {
            return;
        }

        currentEnergy = Mathf.Max(0, currentEnergy - 1);
        CombatDamage.Apply(this, target, damagePerShot, CombatDamage.HitEffects.None);
        ShowTrail(target.transform.position);
        RefreshVisual();
    }

    /// <summary>
    /// 选择世界坐标 X 最小的敌人（“最左边”）。
    /// </summary>
    Enemy FindLeftmostEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        if (enemies == null || enemies.Length == 0)
        {
            return null;
        }

        Enemy best = null;
        float bestX = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.isActiveAndEnabled || !e.IsAlive)
            {
                continue;
            }

            float x = e.transform.position.x;
            if (x < bestX)
            {
                bestX = x;
                best = e;
            }
        }

        return best;
    }

    void ShowTrail(Vector3 targetWorld)
    {
        if (_line == null)
        {
            return;
        }

        _line.enabled = true;
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, targetWorld);
        _trailTimer = trailVisibleSeconds;
    }

    void UpdateTrailFade()
    {
        if (_line == null || !_line.enabled)
        {
            return;
        }

        _trailTimer -= Time.deltaTime;
        if (_trailTimer <= 0f)
        {
            _line.enabled = false;
        }
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureEnergyHud();

        float fill = energyCapacity > 0 ? (float)currentEnergy / energyCapacity : 0f;
        bool showBar = currentEnergy > 0;
        if (_energyHud != null)
        {
            _energyHud.gameObject.SetActive(showBar);
        }

        if (showBar && _energyHudFill != null)
        {
            const float barWidth = 0.85f;
            const float barHeight = 0.12f;
            _energyHudFill.transform.localScale = new Vector3(Mathf.Max(0.04f, barWidth * fill), barHeight, 1f);
            _energyHudFill.transform.localPosition = new Vector3((-barWidth + barWidth * fill) * 0.5f, 0f, 0f);
            _energyHudFill.color = Color.Lerp(
                new Color(0.95f, 0.55f, 0.2f, 1f),
                new Color(1f, 0.85f, 0.25f, 1f),
                fill);
        }

        if (_body != null)
        {
            float tint = showBar ? Mathf.Lerp(0.85f, 1f, fill) : 0.9f;
            _body.color = new Color(0.9f * tint, 0.35f * tint, 0.25f * tint, 1f);
        }
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = gameObject.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Square;
        _body.color = new Color(0.9f, 0.35f, 0.25f, 1f);
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.6f;
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        // 抵消父级 0.6 缩放，使头顶条接近世界单位尺寸
        float inv = 1f / 0.6f;
        var hudGo = new GameObject("EnergyHud");
        hudGo.transform.SetParent(transform, false);
        hudGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        hudGo.transform.localScale = new Vector3(inv, inv, 1f);
        _energyHud = hudGo.transform;

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(hudGo.transform, false);
        bgGo.transform.localPosition = Vector3.zero;
        bgGo.transform.localScale = new Vector3(0.9f, 0.16f, 1f);
        _energyHudBg = bgGo.AddComponent<SpriteRenderer>();
        _energyHudBg.sprite = PrototypeSprites.Square;
        _energyHudBg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        _energyHudBg.sortingOrder = 18;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hudGo.transform, false);
        _energyHudFill = fillGo.AddComponent<SpriteRenderer>();
        _energyHudFill.sprite = PrototypeSprites.Square;
        _energyHudFill.sortingOrder = 19;
    }

    void EnsureLine()
    {
        if (_line != null)
        {
            return;
        }

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.06f;
        _line.endWidth = 0.02f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = new Color(1f, 0.85f, 0.4f, 1f);
        _line.endColor = new Color(1f, 0.4f, 0.2f, 0.2f);
        _line.sortingOrder = 25;
        _line.enabled = false;
        _line.useWorldSpace = true;
    }
}
