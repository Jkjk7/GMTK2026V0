using UnityEngine;

/// <summary>
/// 奥数飞弹：能耗1、容量5、射速5发/秒；紫色弧线飞弹锁定最右敌人。
/// 伤害 10/20/30/50/80。
/// </summary>
public class ArcaneMissileModule : ModuleBase
{
    public const int EnergyCap = 5;
    public const int EnergyPerShot = 1;
    public const float FireRate = 5f;

    [SerializeField] int currentEnergy;
    [SerializeField] int damagePerShot = 10;
    [SerializeField] float spreadDegrees = 55f;

    float _fireTimer;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;

    public override ModuleType ModuleType => global::ModuleType.ArcaneMissile;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => EnergyCap;
    public int DamagePerShot => damagePerShot;
    public float FireInterval => 1f / FireRate;

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
        damagePerShot = ModuleCatalog.GetArcaneMissileDamage(lvl);
        currentEnergy = Mathf.Min(currentEnergy, EnergyCap);
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
        RefreshVisual();
    }

    void Update()
    {
        float interval = FireInterval * EnchantFireIntervalMultiplier;
        if (currentEnergy < EnergyPerShot)
        {
            _fireTimer = interval;
            return;
        }

        _fireTimer += Time.deltaTime;
        while (_fireTimer >= interval && currentEnergy >= EnergyPerShot)
        {
            _fireTimer -= interval;
            Fire();
        }
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        currentEnergy = AbsorbBallEnergy(ball, currentEnergy, EnergyCap);
        RefreshVisual();
    }

    void Fire()
    {
        Enemy target = FindRightmostEnemy();
        if (target == null)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - EnergyPerShot);
            RefreshVisual();
            return;
        }

        currentEnergy -= EnergyPerShot;
        ArcSparkProjectile.Spawn(
            transform.position,
            target,
            this,
            damagePerShot,
            CombatDamage.HitEffects.None,
            ArcSparkProjectile.Style.Arcane,
            spreadDegrees);
        RefreshVisual();
    }

    Enemy FindRightmostEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Enemy best = null;
        float bestX = float.NegativeInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            float x = e.transform.position.x;
            if (x > bestX)
            {
                bestX = x;
                best = e;
            }
        }

        return best;
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureEnergyHud();
        float fill = EnergyCap > 0 ? (float)currentEnergy / EnergyCap : 0f;
        bool show = currentEnergy > 0;
        if (_energyHud != null)
        {
            _energyHud.gameObject.SetActive(show);
        }

        if (show && _energyHudFill != null)
        {
            const float barWidth = 0.85f;
            _energyHudFill.transform.localScale = new Vector3(Mathf.Max(0.04f, barWidth * fill), 0.12f, 1f);
            _energyHudFill.transform.localPosition = new Vector3((-barWidth + barWidth * fill) * 0.5f, 0f, 0f);
            _energyHudFill.color = new Color(0.75f, 0.4f, 1f, 1f);
        }

        if (_body != null)
        {
            _body.color = ModuleCatalog.GetDisplayColor(ModuleType);
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

        _body.sprite = PrototypeSprites.Circle;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.55f;
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        float inv = 1f / 0.55f;
        var hudGo = new GameObject("EnergyHud");
        hudGo.transform.SetParent(transform, false);
        hudGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        hudGo.transform.localScale = new Vector3(inv, inv, 1f);
        _energyHud = hudGo.transform;

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(hudGo.transform, false);
        bgGo.transform.localScale = new Vector3(0.9f, 0.16f, 1f);
        var bg = bgGo.AddComponent<SpriteRenderer>();
        bg.sprite = PrototypeSprites.Square;
        bg.color = new Color(0.12f, 0.1f, 0.16f, 0.85f);
        bg.sortingOrder = 18;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hudGo.transform, false);
        _energyHudFill = fillGo.AddComponent<SpriteRenderer>();
        _energyHudFill.sprite = PrototypeSprites.Square;
        _energyHudFill.sortingOrder = 19;
    }
}
