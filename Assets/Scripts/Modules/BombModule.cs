using UnityEngine;

/// <summary>
/// 大卫炸弹塔：投掷黑色小弹飞向开火时最左敌人位置，落地爆炸 AOE。
/// </summary>
public class BombModule : ModuleBase
{
    [SerializeField] int energyCapacity = 20;
    [SerializeField] int currentEnergy;
    [SerializeField] float fireInterval = 1f / 1.5f;
    [SerializeField] int energyPerShot = 5;
    [SerializeField] int damage = 15;
    [SerializeField] float aoeRadius = 1.5f;

    float _fireTimer;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;

    public override ModuleType ModuleType => global::ModuleType.Bomb;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => energyCapacity;
    public int DamagePerShot => damage;
    public float FireInterval => fireInterval;
    public float AoeRadius => aoeRadius;

    public void ClearEnergy()
    {
        currentEnergy = 0;
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
        damage = ModuleCatalog.GetBombDamage(lvl);
        aoeRadius = ModuleCatalog.GetBombRadius(lvl);
        energyCapacity = 20 + (lvl - 1) * 2;
        currentEnergy = Mathf.Min(currentEnergy, energyCapacity);
        fireInterval = 1f / 1.5f;
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
        if (currentEnergy < energyPerShot)
        {
            _fireTimer = 0f;
            return;
        }

        float interval = fireInterval * EnchantFireIntervalMultiplier;
        _fireTimer += Time.deltaTime;
        if (_fireTimer < interval)
        {
            return;
        }

        Enemy target = FindLeftmostEnemy();
        if (target == null)
        {
            // 有能无怪：保持就绪，不空耗能量也不推进射速节拍
            _fireTimer = interval;
            return;
        }

        _fireTimer -= interval;
        currentEnergy -= energyPerShot;
        Vector3 aim = target.transform.position;
        aim.z = 0f;
        var go = new GameObject("BombProjectile");
        var proj = go.AddComponent<BombProjectile>();
        proj.Launch(transform.position, aim, damage, aoeRadius, this);
        RefreshVisual();
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        currentEnergy = Mathf.Min(energyCapacity, currentEnergy + ball.Energy);
        RefreshVisual();
    }

    Enemy FindLeftmostEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Enemy best = null;
        float bestX = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
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

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureEnergyHud();
        float fill = energyCapacity > 0 ? (float)currentEnergy / energyCapacity : 0f;
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
            _energyHudFill.color = new Color(1f, 0.65f, 0.2f, 1f);
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

        _body.sprite = PrototypeSprites.Square;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.65f;
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        float inv = 1f / 0.65f;
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
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        bg.sortingOrder = 18;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hudGo.transform, false);
        _energyHudFill = fillGo.AddComponent<SpriteRenderer>();
        _energyHudFill.sprite = PrototypeSprites.Square;
        _energyHudFill.sortingOrder = 19;
    }
}
