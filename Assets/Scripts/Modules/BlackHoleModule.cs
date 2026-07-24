using UnityEngine;

/// <summary>
/// 黑洞发射器（史诗）：向最近敌人投掷黑洞，落地后吸引场聚怪。
/// </summary>
public class BlackHoleModule : ModuleBase
{
    [SerializeField] int energyCapacity = 5;
    [SerializeField] int currentEnergy;
    [SerializeField] float fireInterval = 3f;
    [SerializeField] int energyPerShot = 5;
    [SerializeField] float pullRadius = 2.2f;
    [SerializeField] float pullDuration = 2.2f;
    [SerializeField] float pullStrength = 3.5f;

    float _fireTimer;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;

    public override ModuleType ModuleType => global::ModuleType.BlackHole;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => energyCapacity;
    public float FireInterval => fireInterval;
    public float PullRadius => pullRadius;
    public float PullDuration => pullDuration;

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
        pullRadius = ModuleCatalog.GetBlackHoleRadius(lvl);
        pullDuration = ModuleCatalog.GetBlackHoleDuration(lvl);
        pullStrength = ModuleCatalog.GetBlackHolePullStrength(lvl);
        energyCapacity = 5;
        energyPerShot = 5;
        fireInterval = 3f;
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

        Enemy target = FindNearestEnemy();
        if (target == null)
        {
            _fireTimer = interval;
            return;
        }

        _fireTimer -= interval;
        currentEnergy -= energyPerShot;
        Vector3 aim = target.transform.position;
        aim.z = 0f;
        var go = new GameObject("BlackHoleProjectile");
        var proj = go.AddComponent<BlackHoleProjectile>();
        proj.Launch(transform.position, aim, pullRadius, pullDuration, pullStrength);
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

    Enemy FindNearestEnemy()
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Enemy best = null;
        float bestDist = float.PositiveInfinity;
        Vector3 origin = transform.position;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            float d = (e.transform.position - origin).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
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
            _energyHudFill.color = new Color(0.65f, 0.35f, 1f, 1f);
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
        transform.localScale = Vector3.one * 0.7f;
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        float inv = 1f / 0.7f;
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
