using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模块/格子附魔长悬停详情：棋盘 / 手牌 / 商店通用。
/// 棋盘悬停时可同时展示模块与附魔。
/// </summary>
public class ModuleTooltipView : MonoBehaviour
{
    static ModuleTooltipView s_instance;

    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform root;
    [SerializeField] Image icon;
    [SerializeField] Text nameText;
    [SerializeField] Text rarityText;
    [SerializeField] Text descText;
    [SerializeField] Text statsText;
    [SerializeField] Text flavorText;
    [SerializeField] Text enchantTitleText;
    [SerializeField] Text enchantDescText;
    [SerializeField] GameObject moduleBlock;
    [SerializeField] GameObject enchantBlock;

    GameSkin _skin;
    bool _visible;
    bool _hovering;
    object _hoverSource;
    ModuleCardData _hoverCard;
    ModuleBase _hoverLive;
    CellEnchant _hoverEnchant;
    bool _hoverHasModule;
    float _hoverTimer;
    const float HoverDelaySeconds = 0.7f;
    static readonly Vector2 SizeModuleOnly = new Vector2(440f, 560f);
    static readonly Vector2 SizeEnchantOnly = new Vector2(320f, 180f);
    static readonly Vector2 SizeBoth = new Vector2(440f, 660f);
    const string HighlightOpen = "<color=#FFD060><b>";
    const string HighlightClose = "</b></color>";
    const string DimOpen = "<color=#8AA0B8>";
    const string DimClose = "</color>";

    void OnEnable()
    {
        s_instance = this;
    }

    void OnDisable()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    public void Bind(
        CanvasGroup canvasGroup,
        RectTransform rect,
        Image iconImage,
        Text name,
        Text description,
        Text stats,
        Text flavor,
        GameSkin skin,
        Text rarity = null,
        Text enchantTitle = null,
        Text enchantDesc = null,
        GameObject moduleSection = null,
        GameObject enchantSection = null)
    {
        group = canvasGroup;
        root = rect;
        icon = iconImage;
        nameText = name;
        rarityText = rarity;
        descText = description;
        statsText = stats;
        flavorText = flavor;
        enchantTitleText = enchantTitle;
        enchantDescText = enchantDesc;
        moduleBlock = moduleSection;
        enchantBlock = enchantSection;
        _skin = skin;
        if (statsText != null)
        {
            statsText.supportRichText = true;
            statsText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        HideImmediate();
    }

    /// <summary>开始悬停（棋盘传 live 模块；手牌/商店只传 card）。</summary>
    public static void BeginHover(object source, ModuleCardData card, ModuleBase live = null)
    {
        if (s_instance == null || card.Level < 1)
        {
            return;
        }

        s_instance.InternalBegin(source, true, card, live, CellEnchant.None);
    }

    /// <summary>棋盘格悬停：模块与/或附魔。</summary>
    public static void BeginBoardHover(object source, ModuleBase live, CellEnchant enchant)
    {
        if (s_instance == null)
        {
            return;
        }

        bool hasModule = live != null && live.CardData.Level >= 1;
        bool hasEnchant = enchant != CellEnchant.None;
        if (!hasModule && !hasEnchant)
        {
            s_instance.InternalEnd(source);
            return;
        }

        ModuleCardData card = hasModule ? live.CardData : default;
        s_instance.InternalBegin(source, hasModule, card, live, enchant);
    }

    public static void EndHover(object source)
    {
        s_instance?.InternalEnd(source);
    }

    public static void HideAll()
    {
        s_instance?.HideImmediate();
    }

    public static string GetEnchantDisplayName(CellEnchant enchant)
    {
        switch (CellEnchantRules.Normalize(enchant))
        {
            case CellEnchant.Flame: return GameLocalization.Text("Flame Enchant", "火焰附魔");
            case CellEnchant.DamageUp: return GameLocalization.Text("Damage Enchant", "伤害附魔");
            case CellEnchant.Frost: return GameLocalization.Text("Frost Enchant", "寒霜附魔");
            case CellEnchant.Shrink: return GameLocalization.Text("Rapid Enchant", "缩小附魔");
            case CellEnchant.Weak: return GameLocalization.Text("Weak Enchant", "虚弱附魔");
            default: return GameLocalization.Text("Enchant", "附魔");
        }
    }

    public static string GetEnchantDescription(CellEnchant enchant)
    {
        switch (CellEnchantRules.Normalize(enchant))
        {
            case CellEnchant.Flame:
                return GameLocalization.Text(
                    "Damage from this cell applies 3 seconds of burn.",
                    "此格模块造成伤害时，对目标施加 3 秒灼烧。");
            case CellEnchant.DamageUp:
                return GameLocalization.Text(
                    $"Final damage from this cell is multiplied by {CellEnchantRules.DamageUpMult:0.#}.",
                    $"此格模块最终伤害 ×{CellEnchantRules.DamageUpMult:0.#}。");
            case CellEnchant.Frost:
                return GameLocalization.Text(
                    "Damage from this cell applies 3 seconds of chill (30% slow).",
                    "此格模块造成伤害时，施加 3 秒寒冷（30% 减速）。");
            case CellEnchant.Shrink:
                return GameLocalization.Text(
                    $"Damage ×{CellEnchantRules.ShrinkDamageMult:0.#}; fire rate is doubled.",
                    $"此格模块伤害 ×{CellEnchantRules.ShrinkDamageMult:0.#}，射速翻倍。");
            case CellEnchant.Weak:
                return GameLocalization.Text(
                    $"Final damage ×{CellEnchantRules.WeakDamageMult:0.#}; fire interval ×{CellEnchantRules.WeakIntervalMult:0.#}.",
                    $"此格模块最终伤害 ×{CellEnchantRules.WeakDamageMult:0.#}，开火间隔 ×{CellEnchantRules.WeakIntervalMult:0.#}。");
            default:
                return string.Empty;
        }
    }

    void InternalBegin(
        object source,
        bool hasModule,
        ModuleCardData card,
        ModuleBase live,
        CellEnchant enchant)
    {
        bool same = _hovering &&
                    ReferenceEquals(_hoverSource, source) &&
                    _hoverHasModule == hasModule &&
                    CardsEqual(_hoverCard, card) &&
                    _hoverLive == live &&
                    _hoverEnchant == enchant;
        _hoverSource = source;
        _hoverHasModule = hasModule;
        _hoverCard = card;
        _hoverLive = live;
        _hoverEnchant = enchant;
        _hovering = true;
        if (!same)
        {
            _hoverTimer = 0f;
            if (_visible)
            {
                RefreshContent();
            }
            else
            {
                HideVisualOnly();
            }
        }
    }

    void InternalEnd(object source)
    {
        if (!_hovering || !ReferenceEquals(_hoverSource, source))
        {
            return;
        }

        HideImmediate();
    }

    void Update()
    {
        if (!_hovering)
        {
            return;
        }

        if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            HideVisualOnly();
            _hoverTimer = 0f;
            return;
        }

        if (_hoverLive != null)
        {
            _hoverCard = _hoverLive.CardData;
        }

        _hoverTimer += Time.unscaledDeltaTime;
        if (_hoverTimer < HoverDelaySeconds)
        {
            return;
        }

        RefreshContent();
        PositionNear(Input.mousePosition);
        SetVisible(true);
    }

    void RefreshContent()
    {
        bool hasModule = _hoverHasModule;
        bool hasEnchant = _hoverEnchant != CellEnchant.None;

        if (moduleBlock != null)
        {
            moduleBlock.SetActive(hasModule);
        }

        if (enchantBlock != null)
        {
            enchantBlock.SetActive(hasEnchant);
        }

        if (root != null)
        {
            if (hasModule && hasEnchant)
            {
                root.sizeDelta = SizeBoth;
            }
            else if (hasEnchant)
            {
                root.sizeDelta = SizeEnchantOnly;
            }
            else
            {
                root.sizeDelta = SizeModuleOnly;
            }
        }

        LayoutBlocks(hasModule, hasEnchant);

        if (hasModule)
        {
            ModuleCardData card = _hoverLive != null ? _hoverLive.CardData : _hoverCard;
            ModuleRarity rarity = ModuleCatalog.GetRarity(card.Type);
            if (nameText != null)
            {
                nameText.text = ModuleCatalog.GetDisplayName(card);
                nameText.color = ModuleCatalog.GetRarityColor(rarity);
            }

            if (rarityText != null)
            {
                rarityText.text = ModuleCatalog.GetRarityName(rarity);
                rarityText.color = ModuleCatalog.GetRarityColor(rarity);
            }

            if (icon != null)
            {
                ModuleIconVisuals.Apply(icon, card.Type);
                icon.enabled = true;
            }

            if (descText != null)
            {
                descText.text = ModuleCatalog.GetDescription(card.Type);
            }

            if (statsText != null)
            {
                statsText.text = BuildStats(card, _hoverLive);
            }

            if (flavorText != null)
            {
                flavorText.text = ModuleCatalog.GetFlavor(card.Type);
            }
        }

        if (hasEnchant)
        {
            Color tint = SolidEnchantColor(_hoverEnchant);
            string title = hasModule
                ? GameLocalization.Text(
                    $"Enchant · {GetEnchantDisplayName(_hoverEnchant)}",
                    $"附魔 · {GetEnchantDisplayName(_hoverEnchant)}")
                : GetEnchantDisplayName(_hoverEnchant);
            string body = hasModule
                ? GetEnchantDescription(_hoverEnchant)
                : GameLocalization.Text(
                    $"Cell Enchant\n{GetEnchantDescription(_hoverEnchant)}",
                    $"格子附魔\n{GetEnchantDescription(_hoverEnchant)}");

            if (enchantTitleText != null)
            {
                enchantTitleText.text = title;
                enchantTitleText.color = tint;
            }

            if (enchantDescText != null)
            {
                enchantDescText.text = body;
            }

            // 无独立附魔块时：并入 flavor / 仅附魔时填入主标题区
            if (enchantBlock == null)
            {
                if (hasModule)
                {
                    if (flavorText != null)
                    {
                        string existing = flavorText.text;
                        flavorText.text = string.IsNullOrEmpty(existing)
                            ? $"{title}\n{GetEnchantDescription(_hoverEnchant)}"
                            : $"{existing}\n\n{title}\n{GetEnchantDescription(_hoverEnchant)}";
                    }
                }
                else
                {
                    if (nameText != null)
                    {
                        nameText.text = GetEnchantDisplayName(_hoverEnchant);
                        nameText.color = tint;
                    }

                    if (rarityText != null)
                    {
                        rarityText.text = GameLocalization.Text("Cell Enchant", "格子附魔");
                        rarityText.color = new Color(0.75f, 0.78f, 0.85f, 1f);
                    }

                    if (icon != null)
                    {
                        icon.sprite = PrototypeSprites.Square;
                        icon.color = tint;
                        icon.enabled = true;
                    }

                    if (descText != null)
                    {
                        descText.text = GetEnchantDescription(_hoverEnchant);
                    }

                    if (statsText != null)
                    {
                        statsText.text = string.Empty;
                    }

                    if (flavorText != null)
                    {
                        flavorText.text = string.Empty;
                    }
                }
            }
        }
    }

    void LayoutBlocks(bool hasModule, bool hasEnchant)
    {
        var moduleRt = moduleBlock != null ? moduleBlock.transform as RectTransform : null;
        var enchantRt = enchantBlock != null ? enchantBlock.transform as RectTransform : null;

        if (hasModule && hasEnchant)
        {
            if (moduleRt != null)
            {
                moduleRt.anchorMin = new Vector2(0f, 0.28f);
                moduleRt.anchorMax = new Vector2(1f, 1f);
                moduleRt.offsetMin = Vector2.zero;
                moduleRt.offsetMax = Vector2.zero;
            }

            if (enchantRt != null)
            {
                enchantRt.anchorMin = new Vector2(0.04f, 0.03f);
                enchantRt.anchorMax = new Vector2(0.96f, 0.26f);
                enchantRt.offsetMin = Vector2.zero;
                enchantRt.offsetMax = Vector2.zero;
            }
        }
        else if (hasModule)
        {
            if (moduleRt != null)
            {
                moduleRt.anchorMin = Vector2.zero;
                moduleRt.anchorMax = Vector2.one;
                moduleRt.offsetMin = Vector2.zero;
                moduleRt.offsetMax = Vector2.zero;
            }
        }
        else if (hasEnchant && enchantRt != null)
        {
            enchantRt.anchorMin = new Vector2(0.04f, 0.08f);
            enchantRt.anchorMax = new Vector2(0.96f, 0.92f);
            enchantRt.offsetMin = Vector2.zero;
            enchantRt.offsetMax = Vector2.zero;
        }
    }

    static Color SolidEnchantColor(CellEnchant enchant)
    {
        Color c = GridBoard.GetEnchantColor(enchant);
        c.a = 1f;
        return Color.Lerp(c, Color.white, 0.25f);
    }

    struct StatSnap
    {
        public string Attack;
        public string Rate;
        public string Cost;
        public string Cap;
        public string Effect;
    }

    static string BuildStats(ModuleCardData card, ModuleBase live)
    {
        int maxLv = GetTooltipMaxLevel(card.Type);
        int curLv = Mathf.Clamp(card.Level, 1, maxLv);
        StatSnap current = BuildStatSnap(card, live, curLv);
        var sb = new System.Text.StringBuilder(256);
        AppendCurrentStatBlock(sb, current);

        string liveEnergy = LiveEnergyLine(live);
        if (!string.IsNullOrEmpty(liveEnergy))
        {
            sb.Append('\n').Append(liveEnergy);
        }

        if (maxLv > 1)
        {
            sb.Append("\n\n升级");
            for (int lv = 1; lv <= maxLv; lv++)
            {
                if (lv == curLv)
                {
                    continue;
                }

                string diff = FormatStatDiff(lv, BuildStatSnap(card, live, lv), current);
                if (string.IsNullOrEmpty(diff))
                {
                    continue;
                }

                sb.Append('\n');
                if (lv > curLv)
                {
                    sb.Append(HighlightOpen).Append(diff).Append(HighlightClose);
                }
                else
                {
                    sb.Append(DimOpen).Append(diff).Append(DimClose);
                }
            }
        }

        return sb.ToString();
    }

    static int GetTooltipMaxLevel(ModuleType type)
    {
        if (type == ModuleType.Miner)
        {
            return 3;
        }

        if (type == ModuleType.FireEnchant || type == ModuleType.Surprise)
        {
            return 4;
        }

        if (ModuleCatalog.IsItemModule(type))
        {
            return 1;
        }

        if (ModuleCatalog.IsAttackModule(type)
            || type == ModuleType.FlameAmp
            || type == ModuleType.IceAmp
            || type == ModuleType.Heatwave
            || type == ModuleType.FrostFreeze)
        {
            return ModulePricing.MaxAttackLevel;
        }

        return 1;
    }

    static StatSnap BuildStatSnap(ModuleCardData card, ModuleBase live, int level)
    {
        int lv = Mathf.Max(1, level);
        CellEnchant enchant = ResolveLiveEnchant(live);
        float chillSlow = ResolveChillSlowPercent();

        switch (card.Type)
        {
            case ModuleType.Projectile:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetDamagePerShot(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetFireInterval(lv), enchant)),
                    Cost: "1",
                    Cap: Int(ModuleCatalog.GetEnergyCapacity(lv)));

            case ModuleType.Bomb:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetBombDamage(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(1f / 1.5f, enchant)),
                    Cost: "5",
                    Cap: Int(ModuleCatalog.GetBombEnergyCapacity(lv)),
                    Effect: $"AOE半径{ModuleCatalog.GetBombRadius(lv):0.#}");

            case ModuleType.IceLaser:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetIceDamage(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetIceFireInterval(lv), enchant)),
                    Cost: Int(ModuleCatalog.GetIceEnergyPerShot(lv)),
                    Cap: Int(ModuleCatalog.GetIceEnergyCapacity(lv)),
                    Effect: $"[寒冷]{ModuleCatalog.GetIceSlowDuration(lv):0.#}秒（减速{chillSlow * 100f:0}%）");

            case ModuleType.Spark:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetSparkDamage(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetSparkFireInterval(lv), enchant)),
                    Cost: Int(ModuleCatalog.GetSparkEnergyPerShot(lv)),
                    Cap: Int(ModuleCatalog.GetSparkEnergyCapacity(lv)),
                    Effect: $"[灼烧]{ModuleCatalog.GetSparkBurnDuration(lv):0.#}秒");

            case ModuleType.LaserCannon:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetLaserCannonDamage(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(1f, enchant)),
                    Cost: "5",
                    Cap: "5");

            case ModuleType.ArcaneMissile:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetArcaneMissileDamage(lv), enchant),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetArcaneMissileFireInterval(lv), enchant)),
                    Cost: Int(ModuleCatalog.GetArcaneMissileEnergyPerShot(lv)),
                    Cap: Int(ModuleCatalog.GetArcaneMissileEnergyCapacity(lv)),
                    Effect: "索敌最右");

            case ModuleType.BlackHole:
                return Snap(
                    Cost: Int(ModuleCatalog.GetBlackHoleEnergyPerShot(lv)),
                    Cap: Int(ModuleCatalog.GetBlackHoleEnergyCapacity(lv)),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetBlackHoleFireInterval(lv), enchant)),
                    Effect: $"半径{ModuleCatalog.GetBlackHoleRadius(lv):0.#} 持续{ModuleCatalog.GetBlackHoleDuration(lv):0.#}秒 吸力{ModuleCatalog.GetBlackHolePullStrength(lv):0.#}");

            case ModuleType.Heatwave:
                return Snap(
                    Cost: Int(HeatwaveModule.EnergyCost),
                    Cap: Int(HeatwaveModule.EnergyCap),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetHeatwaveFireInterval(lv), enchant)),
                    Effect: $"全屏[灼烧]{ModuleCatalog.GetHeatwaveBurnDuration(lv):0.#}秒");

            case ModuleType.FlameWall:
                return Snap(
                    Attack: Attack(ModuleCatalog.GetFlameWallDamage(lv), enchant),
                    Cost: $"{ModuleCatalog.GetFlameWallEnergyDrainPerSecond(lv):0.#}/秒",
                    Cap: Int(ModuleCatalog.GetFlameWallEnergyCapacity(lv)),
                    Effect: $"中线火墙；穿过[灼烧]{ModuleCatalog.GetFlameWallBurnDuration(lv):0.#}秒");

            case ModuleType.FlameBlessing:
                return Snap(Effect: "一次性：目标格变为火焰附魔");

            case ModuleType.Purify:
                return Snap(Effect: "一次性：清除目标格诅咒/锁定/附魔");

            case ModuleType.FrostMushroom:
                return Snap(Effect: $"一次性：全体冻结1秒并[寒冷]{2f:0.#}秒（减速{chillSlow * 100f:0}%）");

            case ModuleType.FrostFreeze:
                return Snap(
                    Cost: Int(FrostFreezeModule.EnergyCost),
                    Cap: Int(FrostFreezeModule.EnergyCap),
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(
                        ModuleCatalog.GetFrostFreezeFireInterval(lv), enchant)),
                    Effect: $"全屏[寒冷]{ModuleCatalog.GetFrostFreezeChillDuration(lv):0.#}秒（减速{chillSlow * 100f:0}%）");

            case ModuleType.Miner:
                return Snap(
                    Attack: $"{ModuleCatalog.GetMinerGoldAmount(lv)}金",
                    Rate: FormatFireRate(CellEnchantRules.ScaleFireInterval(3f, enchant)),
                    Cost: Int(MinerModule.FixedEnergyCost),
                    Cap: Int(MinerModule.FixedEnergyCost),
                    Effect: "每次开火产出金币");

            case ModuleType.FlameAmp:
                return Snap(
                    Effect: $"灼烧+{ModuleCatalog.GetFlameAmpBonus(lv)}/{RunModifiers.BurnTickInterval:0.#}秒（被动可叠加）");

            case ModuleType.IceAmp:
            {
                float bonus = ModuleCatalog.GetIceAmpSlowBonus(lv);
                return Snap(
                    Effect: $"寒冷减速+{bonus * 100f:0}%（总上限{ModuleCatalog.MaxChillSlowPercent * 100f:0}%，被动可叠加）");
            }

            case ModuleType.FireEnchant:
                return Snap(Effect: $"灼烧附魔{lv}格（种子固定）");

            case ModuleType.Surprise:
                return Snap(Effect: $"随机附魔{lv}格（种子固定）");

            case ModuleType.Splitter:
                return Snap(Effect: "T 形一分二，寿命减半");

            case ModuleType.Portal:
                return Snap(Effect: PathShapeLabel(card, live) + "；成对传送保方向，最多2座");

            case ModuleType.Relay:
                return Snap(
                    Cap: Int(RelayModule.EnergyCap),
                    Effect: PathShapeLabel(card, live) + "；穿过汲能，满后刷新寿命");

            case ModuleType.Accelerator:
                return Snap(Effect: PathShapeLabel(card, live) + "；速度×1.5（每球一次）");

            case ModuleType.Fusion:
            {
                string progress = live is FusionModule f
                    ? $"；进度{f.AbsorbedCount}/{FusionModule.BallsNeeded}"
                    : string.Empty;
                return Snap(Effect: PathShapeLabel(card, live) + "；5球合成1球" + progress);
            }

            case ModuleType.Fission:
                return Snap(
                    Cap: Int(FissionModule.EnergyThreshold),
                    Effect: PathShapeLabel(card, live) + "；≥5能→0.5秒射5颗默认球（裂变球不再被核裂变吸收）");

            case ModuleType.Redirector:
            {
                string facing = live is RedirectorModule red
                    ? OrientationLabel(red.Orientation)
                    : string.Empty;
                string effect = string.IsNullOrEmpty(facing)
                    ? "直角改向"
                    : $"直角改向（{facing}）";
                return Snap(Effect: effect);
            }

            default:
                if (live is PathEffectModule path)
                {
                    string shape = path.Shape == PathShape.Bent ? "L 拐弯"
                        : path.Shape == PathShape.Tee ? "T 形" : "直通";
                    return Snap(Effect: $"路径模块 · {shape}");
                }

                return card.Bent ? Snap(Effect: "路径：拐弯版") : Snap(Effect: "直角改向");
        }
    }

    static CellEnchant ResolveLiveEnchant(ModuleBase live)
    {
        if (live == null || live.BoundBoard == null)
        {
            return CellEnchant.None;
        }

        return live.CellEnchant;
    }

    static float ResolveChillSlowPercent()
    {
        return RunModifiers.Instance != null
            ? RunModifiers.Instance.GetEffectiveChillSlowPercent()
            : ModuleCatalog.IceSlowPercent;
    }

    static string Attack(int rawDamage, CellEnchant enchant) =>
        Int(CellEnchantRules.ScaleDamage(rawDamage, enchant));

    static StatSnap Snap(
        string Attack = null,
        string Rate = null,
        string Cost = null,
        string Cap = null,
        string Effect = null) =>
        new StatSnap
        {
            Attack = Attack ?? string.Empty,
            Rate = Rate ?? string.Empty,
            Cost = Cost ?? string.Empty,
            Cap = Cap ?? string.Empty,
            Effect = Effect ?? string.Empty
        };

    static string Int(int v) => v.ToString();

    static string Sec(float seconds) => $"{seconds:0.#}秒";

    /// <summary>射速 ≥0.5 次/秒显示 X/秒，否则显示间隔秒数。</summary>
    static string FormatFireRate(float intervalSeconds)
    {
        if (intervalSeconds <= 0.0001f)
        {
            return string.Empty;
        }

        float rate = 1f / intervalSeconds;
        if (rate < 0.5f)
        {
            return Sec(intervalSeconds);
        }

        return $"{rate:0.#}/秒";
    }

    static string PathShapeLabel(ModuleCardData card, ModuleBase live)
    {
        if (live is PathEffectModule path)
        {
            return path.Shape == PathShape.Bent ? "L拐弯"
                : path.Shape == PathShape.Tee ? "T形" : "直通";
        }

        return card.Bent ? "L拐弯" : "直通";
    }

    static void AppendCurrentStatBlock(System.Text.StringBuilder sb, StatSnap s)
    {
        void Line(string label, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            if (sb.Length > 0)
            {
                sb.Append('\n');
            }

            sb.Append(label).Append(value);
        }

        Line("攻击力：", s.Attack);
        Line("射速：", s.Rate);
        Line("能耗：", s.Cost);
        Line("储能：", s.Cap);
        Line("效果：", s.Effect);
    }

    static string FormatStatDiff(int level, StatSnap target, StatSnap current)
    {
        var parts = new System.Collections.Generic.List<string>(4);
        AddDiff(parts, "攻击力", target.Attack, current.Attack);
        AddDiff(parts, "射速", target.Rate, current.Rate);
        AddDiff(parts, "能耗", target.Cost, current.Cost);
        AddDiff(parts, "储能", target.Cap, current.Cap);
        AddDiff(parts, "效果", target.Effect, current.Effect);
        if (parts.Count == 0)
        {
            return null;
        }

        return $"Lv{level}：" + string.Join(" · ", parts);
    }

    static void AddDiff(
        System.Collections.Generic.List<string> parts,
        string label,
        string target,
        string current)
    {
        if (string.IsNullOrEmpty(target) && string.IsNullOrEmpty(current))
        {
            return;
        }

        if (target == current)
        {
            return;
        }

        parts.Add(string.IsNullOrEmpty(target) ? $"{label} —" : $"{label} {target}");
    }

    static string LiveEnergyLine(ModuleBase live)
    {
        if (live == null)
        {
            return null;
        }

        if (live is LaserCannonModule cannon)
        {
            return $"当前储能：{cannon.CurrentEnergy}/{cannon.EnergyCapacity}";
        }

        if (live is ProjectileModule p)
        {
            return $"当前储能：{p.CurrentEnergy}/{p.EnergyCapacity}";
        }

        if (live is BombModule b)
        {
            return $"当前储能：{b.CurrentEnergy}/{b.EnergyCapacity}";
        }

        if (live is IceLaserModule ice)
        {
            return $"当前储能：{ice.CurrentEnergy}/{ice.EnergyCapacity}";
        }

        if (live is MinerModule m)
        {
            return $"当前储能：{m.CurrentEnergy}/{m.EnergyCapacity}";
        }

        if (live is SparkModule s)
        {
            return $"当前储能：{s.CurrentEnergy}/{s.EnergyCapacity}";
        }

        if (live is BlackHoleModule bh)
        {
            return $"当前储能：{bh.CurrentEnergy}/{bh.EnergyCapacity}";
        }

        if (live is HeatwaveModule hw)
        {
            return $"当前储能：{hw.CurrentEnergy}/{hw.EnergyCapacity}";
        }

        if (live is FlameWallModule fw)
        {
            string wall = fw.IsWallActive ? "·火墙开启" : string.Empty;
            return $"当前储能：{fw.CurrentEnergy}/{fw.EnergyCapacity}{wall}";
        }

        if (live is FrostFreezeModule ff)
        {
            return $"当前储能：{ff.CurrentEnergy}/{ff.EnergyCapacity}";
        }

        if (live is ArcaneMissileModule am)
        {
            return $"当前储能：{am.CurrentEnergy}/{am.EnergyCapacity}";
        }

        if (live is RelayModule relay)
        {
            return $"当前储能：{relay.StoredEnergy}/{RelayModule.EnergyCap}";
        }

        if (live is FissionModule fission)
        {
            return $"当前储能：{fission.StoredEnergy}/{FissionModule.EnergyThreshold}";
        }

        return null;
    }

    static string OrientationLabel(int orientation)
    {
        switch (((orientation % 4) + 4) % 4)
        {
            case 0: return "左 ↔ 上";
            case 1: return "上 ↔ 右";
            case 2: return "右 ↔ 下";
            default: return "下 ↔ 左";
        }
    }

    static bool CardsEqual(ModuleCardData a, ModuleCardData b)
    {
        return a.Type == b.Type
               && a.Level == b.Level
               && a.InvestedGold == b.InvestedGold
               && a.Bent == b.Bent;
    }

    void HideImmediate()
    {
        _hovering = false;
        _hoverSource = null;
        _hoverLive = null;
        _hoverCard = default;
        _hoverEnchant = CellEnchant.None;
        _hoverHasModule = false;
        _hoverTimer = 0f;
        SetVisible(false);
    }

    void HideVisualOnly()
    {
        SetVisible(false);
    }

    void PositionNear(Vector3 screenPos)
    {
        if (root == null)
        {
            return;
        }

        RectTransform canvasRt = root.parent as RectTransform;
        if (canvasRt == null)
        {
            root.position = screenPos + new Vector3(24f, -24f, 0f);
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screenPos, null, out Vector2 local);
        local += new Vector2(28f, -20f);

        Vector2 size = root.rect.size;
        Vector2 canvasSize = canvasRt.rect.size;
        float halfW = canvasSize.x * 0.5f;
        float halfH = canvasSize.y * 0.5f;
        local.x = Mathf.Clamp(local.x, -halfW + 12f, halfW - size.x - 12f);
        local.y = Mathf.Clamp(local.y, -halfH + size.y + 12f, halfH - 12f);
        root.anchoredPosition = local;
    }

    void SetVisible(bool visible)
    {
        _visible = visible;
        gameObject.SetActive(true);
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        if (visible)
        {
            transform.SetAsLastSibling();
        }
    }
}
