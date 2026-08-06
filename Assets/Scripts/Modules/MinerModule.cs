using UnityEngine;

/// <summary>
/// 比特币采矿机：固定能耗换金币；升级提高产金量；全图最多 3 台同时产出。
/// </summary>
public class MinerModule : ModuleBase
{
    public const int MaxActiveProducers = 3;
    public const int FixedEnergyCost = 10;

    [SerializeField] int energyCapacity = 10;
    [SerializeField] int currentEnergy;
    [SerializeField] int energyCost = FixedEnergyCost;
    [SerializeField] int goldPerCycle = 1;
    [SerializeField] float cooldownSeconds = 3f;

    float _cooldown;
    SpriteRenderer _body;
    Transform _energyHud;
    SpriteRenderer _energyHudFill;
    TextMesh _levelLabel;
    ModuleCooldownHud _cooldownHud;

    public override ModuleType ModuleType => global::ModuleType.Miner;
    public int CurrentEnergy => currentEnergy;
    public int EnergyCapacity => energyCapacity;
    public int EnergyCost => energyCost;
    public int GoldPerCycle => goldPerCycle;

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
        int lvl = Mathf.Clamp(level, 1, 3);
        energyCost = FixedEnergyCost;
        goldPerCycle = ModuleCatalog.GetMinerGoldAmount(lvl);
        energyCapacity = FixedEnergyCost;
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
        float cdTotal = cooldownSeconds * EnchantFireIntervalMultiplier;
        if (_cooldown > 0f)
        {
            _cooldown -= Time.deltaTime;
        }

        if (_cooldownHud == null)
        {
            _cooldownHud = ModuleCooldownHud.Ensure(transform, new Vector3(0f, 1.15f, 0f));
        }

        _cooldownHud.SetCooldown(_cooldown > 0f ? _cooldown : 0f, cdTotal);

        if (currentEnergy < energyCost || _cooldown > 0f)
        {
            return;
        }

        if (!IsAmongActiveProducers())
        {
            return;
        }

        currentEnergy -= energyCost;
        _cooldown = cdTotal;
        int gold = Mathf.Max(1, goldPerCycle);
        if (GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(gold, transform.position);
        }
        else if (Economy.Instance != null)
        {
            Economy.Instance.AddGold(gold);
        }

        RefreshVisual();
    }

    bool IsAmongActiveProducers()
    {
        MinerModule[] miners = FindObjectsOfType<MinerModule>();
        int active = 0;
        for (int i = 0; i < miners.Length; i++)
        {
            MinerModule m = miners[i];
            if (m == null || !m.isActiveAndEnabled)
            {
                continue;
            }

            // 稳定排序：实例 ID 较小的优先产出
            if (m.GetEntityId() < GetEntityId())
            {
                active++;
                if (active >= MaxActiveProducers)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        currentEnergy = AbsorbBallEnergy(ball, currentEnergy, energyCapacity);
        RefreshVisual();
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
            _energyHudFill.color = new Color(0.95f, 0.85f, 0.25f, 1f);
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
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.85f);
        bg.sortingOrder = 18;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hudGo.transform, false);
        _energyHudFill = fillGo.AddComponent<SpriteRenderer>();
        _energyHudFill.sprite = PrototypeSprites.Square;
        _energyHudFill.sortingOrder = 19;
    }
}
