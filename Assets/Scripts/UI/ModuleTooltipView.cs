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
    static readonly Vector2 SizeModuleOnly = new Vector2(340f, 300f);
    static readonly Vector2 SizeEnchantOnly = new Vector2(300f, 170f);
    static readonly Vector2 SizeBoth = new Vector2(340f, 390f);

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
        switch (enchant)
        {
            case CellEnchant.Flame: return GameLocalization.Text("Flame Enchant", "火焰附魔");
            case CellEnchant.DamageUp: return GameLocalization.Text("Damage Enchant", "伤害附魔");
            case CellEnchant.Frost: return GameLocalization.Text("Frost Enchant", "寒霜附魔");
            case CellEnchant.Shrink: return GameLocalization.Text("Rapid Enchant", "缩小附魔");
            case CellEnchant.Cooldown: return GameLocalization.Text("Cooldown Enchant", "冷却附魔");
            default: return GameLocalization.Text("Enchant", "附魔");
        }
    }

    public static string GetEnchantDescription(CellEnchant enchant)
    {
        switch (enchant)
        {
            case CellEnchant.Flame:
                return GameLocalization.Text(
                    "Damage from this cell applies 3 seconds of burn.",
                    "此格模块造成伤害时，对目标施加 3 秒灼烧。");
            case CellEnchant.DamageUp:
                return GameLocalization.Text(
                    "Final damage from this cell is multiplied by 1.2.",
                    "此格模块最终伤害 ×1.2。");
            case CellEnchant.Frost:
                return GameLocalization.Text(
                    "Damage from this cell applies 3 seconds of chill (30% slow).",
                    "此格模块造成伤害时，施加 3 秒寒冷（30% 减速）。");
            case CellEnchant.Shrink:
                return GameLocalization.Text(
                    "Damage ×0.5; attack speed is doubled.",
                    "此格模块伤害 ×0.5，射速翻倍。");
            case CellEnchant.Cooldown:
                return GameLocalization.Text(
                    "Cooldown-based modules on this cell recover twice as fast.",
                    "此格有冷却的模块冷却时间减半（如采矿机）。");
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

    static string BuildStats(ModuleCardData card, ModuleBase live)
    {
        if (card.Type == ModuleType.Bomb)
        {
            int dmg = live is BombModule b ? b.DamagePerShot : ModuleCatalog.GetBombDamage(card.Level);
            float radius = live is BombModule b2 ? b2.AoeRadius : ModuleCatalog.GetBombRadius(card.Level);
            string energy = live is BombModule liveB
                ? $"储能：{liveB.CurrentEnergy}/{liveB.EnergyCapacity}"
                : "储能上限：20";
            return $"伤害：{dmg}\n射速：1.5/秒\n范围：{radius:0.#}\n{energy}";
        }

        if (card.Type == ModuleType.BlackHole)
        {
            float radius = live is BlackHoleModule bh
                ? bh.PullRadius
                : ModuleCatalog.GetBlackHoleRadius(card.Level);
            float dur = live is BlackHoleModule bh2
                ? bh2.PullDuration
                : ModuleCatalog.GetBlackHoleDuration(card.Level);
            string energy = live is BlackHoleModule liveH
                ? $"储能：{liveH.CurrentEnergy}/{liveH.EnergyCapacity}"
                : "储能上限：5";
            return $"吸引半径：{radius:0.#}\n持续：{dur:0.#}秒\n射速：3秒/发\n能耗：5\n{energy}";
        }

        if (card.Type == ModuleType.IceLaser)
        {
            int dmg = live is IceLaserModule ice
                ? ice.DamagePerShot
                : ModuleCatalog.GetIceDamage(card.Level);
            float interval = live is IceLaserModule ice2
                ? ice2.FireInterval
                : ModuleCatalog.GetFireInterval(card.Level);
            float rps = interval > 0.0001f ? 1f / interval : 0f;
            float slowDur = live is IceLaserModule ice3
                ? ice3.SlowDuration
                : ModuleCatalog.GetIceSlowDuration(card.Level);
            int cap = live is IceLaserModule ice4
                ? ice4.EnergyCapacity
                : ModuleCatalog.GetEnergyCapacity(card.Level);
            string energy = live is IceLaserModule liveI
                ? $"储能：{liveI.CurrentEnergy}/{liveI.EnergyCapacity}"
                : $"储能上限：{cap}";
            return $"伤害：{dmg}\n射速：{rps:0.#}/秒\n寒冷：{ModuleCatalog.IceSlowPercent * 100f:0}% / {slowDur:0.#}秒\n{energy}";
        }

        if (card.Type == ModuleType.Miner)
        {
            int cost = MinerModule.FixedEnergyCost;
            int gold = live is MinerModule m
                ? m.GoldPerCycle
                : ModuleCatalog.GetMinerGoldAmount(card.Level);
            string energy = live is MinerModule liveM
                ? $"储能：{liveM.CurrentEnergy}/{liveM.EnergyCapacity}"
                : $"储能上限：{cost}";
            return $"产出：{cost} 能 → {gold} 金\n冷却：3 秒\n{energy}";
        }

        if (card.Type == ModuleType.FlameAmp)
        {
            int bonus = live is FlameAmpModule amp
                ? amp.BurnBonus
                : ModuleCatalog.GetFlameAmpBonus(card.Level);
            return $"灼烧增幅：+{bonus}/{RunModifiers.BurnTickInterval:0.#}秒\n被动：场上生效，可叠加";
        }

        if (card.Type == ModuleType.Spark)
        {
            int dmg = live is SparkModule s
                ? s.DamagePerShot
                : ModuleCatalog.GetSparkDamage(card.Level);
            float interval = live is SparkModule s2
                ? s2.FireInterval
                : ModuleCatalog.GetSparkFireInterval(card.Level);
            float burn = live is SparkModule s3
                ? s3.BurnDuration
                : ModuleCatalog.GetSparkBurnDuration(card.Level);
            int cap = live is SparkModule s4
                ? s4.EnergyCapacity
                : ModuleCatalog.GetSparkEnergyCapacity(card.Level);
            int cost = live is SparkModule s5
                ? s5.EnergyPerShot
                : ModuleCatalog.GetSparkEnergyPerShot(card.Level);
            float rps = interval > 0.0001f ? 1f / interval : 0f;
            string energy = live is SparkModule liveS
                ? $"储能：{liveS.CurrentEnergy}/{liveS.EnergyCapacity}"
                : $"储能上限：{cap}";
            return $"伤害：{dmg}\n射速：{rps:0.#}/秒\n灼烧：{burn:0.#}秒\n能耗：{cost}\n{energy}";
        }

        if (card.Type == ModuleType.Heatwave)
        {
            float burn = ModuleCatalog.GetHeatwaveBurnDuration(card.Level);
            string energy = live is HeatwaveModule hw
                ? $"储能：{hw.CurrentEnergy}/{hw.EnergyCapacity}"
                : "储能：0/20";
            return $"全屏灼烧：{burn:0.#}秒\n能耗：20\n冷却：5秒\n{energy}";
        }

        if (card.Type == ModuleType.FireEnchant || card.Type == ModuleType.Surprise)
        {
            int cells = Mathf.Clamp(card.Level, 1, 4);
            string kind = card.Type == ModuleType.FireEnchant ? "灼烧附魔" : "随机附魔";
            return $"附魔格数：{cells}\n种类：{kind}\n同位置种子固定；诅咒格跳过不补抽";
        }

        if (card.Type == ModuleType.Splitter)
        {
            return "形状：T 形\n效果：一分二，寿命减半\n不可与收束器合成拐弯";
        }

        if (card.Type == ModuleType.Portal)
        {
            string shape = card.Bent ? "L 拐弯" : "直通";
            return $"形状：{shape}\n场上最多 2 座\n成对传送，保持飞行方向";
        }

        if (card.Type == ModuleType.Relay)
        {
            string shape = card.Bent ? "L 拐弯" : "直通";
            string energy = live is RelayModule r
                ? $"储能：{r.StoredEnergy}/{RelayModule.EnergyCap}"
                : $"储能上限：{RelayModule.EnergyCap}";
            return $"形状：{shape}\n吸收能量；下一球刷新寿命\n{energy}";
        }

        if (card.Type == ModuleType.Accelerator)
        {
            string shape = card.Bent ? "L 拐弯" : "直通";
            return $"形状：{shape}\n速度 ×1.5（每球一次）";
        }

        if (card.Type == ModuleType.Fusion)
        {
            string shape = card.Bent ? "L 拐弯" : "直通";
            string prog = live is FusionModule f
                ? $"进度：{f.AbsorbedCount}/{FusionModule.BallsNeeded}"
                : $"需吸收：{FusionModule.BallsNeeded} 球";
            return $"形状：{shape}\n5 球合成 1 球\n{prog}";
        }

        if (card.Type == ModuleType.Fission)
        {
            string shape = card.Bent ? "L 拐弯" : "直通";
            string energy = live is FissionModule fi
                ? $"储能：{fi.StoredEnergy}/{FissionModule.EnergyThreshold}"
                : $"阈值：{FissionModule.EnergyThreshold}";
            return $"形状：{shape}\n≥5 能 → 0.5s 射 5 颗默认球\n{energy}";
        }

        if (card.Type == ModuleType.Projectile)
        {
            int dmg = live is ProjectileModule p
                ? p.DamagePerShot
                : ModuleCatalog.GetDamagePerShot(card.Level);
            float interval = live is ProjectileModule p2
                ? p2.FireInterval
                : ModuleCatalog.GetFireInterval(card.Level);
            int cap = live is ProjectileModule p3
                ? p3.EnergyCapacity
                : ModuleCatalog.GetEnergyCapacity(card.Level);
            float rps = interval > 0.0001f ? 1f / interval : 0f;

            string energyLine = live is ProjectileModule liveProj
                ? $"储能：{liveProj.CurrentEnergy}/{cap}"
                : $"储能上限：{cap}";

            return $"伤害：{dmg}\n射速：{rps:0.#}/秒\n{energyLine}";
        }

        if (live is RedirectorModule red)
        {
            return $"功能：直角改向\n朝向：{OrientationLabel(red.Orientation)}";
        }

        if (live is PathEffectModule path)
        {
            string shape = path.Shape == PathShape.Bent ? "L 拐弯"
                : path.Shape == PathShape.Tee ? "T 形" : "直通";
            return $"路径模块\n形状：{shape}\n朝向：{path.OrientationIndex}";
        }

        if (card.Bent)
        {
            return "路径：拐弯版";
        }

        return "功能：直角改向";
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
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        gameObject.SetActive(true);
        if (!visible && group != null)
        {
            group.alpha = 0f;
        }
    }
}
