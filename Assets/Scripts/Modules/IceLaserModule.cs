using UnityEngine;

/// <summary>
/// 查理寒冰塔：伤害 5、耗能 2、减速目标（刷新不叠加）。
/// </summary>
public class IceLaserModule : ModuleBase
{
    [SerializeField] int energyCapacity = 8;
    [SerializeField] int currentEnergy;
    [SerializeField] float fireInterval = 0.12f;
    [SerializeField] int energyPerShot = 2;
    [SerializeField] int damagePerShot = 5;
    [SerializeField] float slowPercent = 0.30f;
    [SerializeField] float slowDuration = 2f;
    [SerializeField] float trailVisibleSeconds = 0.1f;

    float _fireTimer;
    float _trailTimer;
    LineRenderer _line;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;

    public override ModuleType ModuleType => global::ModuleType.IceLaser;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => energyCapacity;
    public int DamagePerShot => damagePerShot;
    public float FireInterval => fireInterval;

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
        damagePerShot = 5;
        energyCapacity = 8 + (lvl - 1);
        fireInterval = Mathf.Max(0.08f, 0.12f / (1f + 0.08f * (lvl - 1)));
        if (lvl >= 2)
        {
            slowPercent = 0.40f;
            slowDuration = 3f;
        }
        else
        {
            slowPercent = 0.30f;
            slowDuration = 2f;
        }

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
        if (currentEnergy < energyPerShot)
        {
            _fireTimer = 0f;
            return;
        }

        _fireTimer += Time.deltaTime;
        while (_fireTimer >= fireInterval && currentEnergy >= energyPerShot)
        {
            _fireTimer -= fireInterval;
            Fire();
        }
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

    void Fire()
    {
        Enemy target = FindLeftmostEnemy();
        if (target == null)
        {
            currentEnergy = Mathf.Max(0, currentEnergy - energyPerShot);
            RefreshVisual();
            return;
        }

        currentEnergy -= energyPerShot;
        target.TakeDamage(damagePerShot);
        target.ApplySlow(slowPercent, slowDuration);
        ShowTrail(target.transform.position);
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
            _energyHudFill.color = new Color(0.55f, 0.9f, 1f, 1f);
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
        transform.localScale = Vector3.one * 0.6f;
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        float inv = 1f / 0.6f;
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

    void EnsureLine()
    {
        if (_line != null)
        {
            return;
        }

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.09f;
        _line.endWidth = 0.03f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = new Color(0.55f, 0.95f, 1f, 1f);
        _line.endColor = new Color(0.3f, 0.7f, 1f, 0.25f);
        _line.sortingOrder = 25;
        _line.enabled = false;
        _line.useWorldSpace = true;
    }
}
