using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模块长悬停详情：棋盘 / 手牌 / 商店通用。
/// 布局侧重描述；留言次要；不显示金额。
/// </summary>
public class ModuleTooltipView : MonoBehaviour
{
    static ModuleTooltipView s_instance;

    [SerializeField] CanvasGroup group;
    [SerializeField] RectTransform root;
    [SerializeField] Image icon;
    [SerializeField] Text nameText;
    [SerializeField] Text descText;
    [SerializeField] Text statsText;
    [SerializeField] Text flavorText;

    GameSkin _skin;
    bool _visible;
    bool _hovering;
    object _hoverSource;
    ModuleCardData _hoverCard;
    ModuleBase _hoverLive;
    float _hoverTimer;
    const float HoverDelaySeconds = 0.7f;

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
        GameSkin skin)
    {
        group = canvasGroup;
        root = rect;
        icon = iconImage;
        nameText = name;
        descText = description;
        statsText = stats;
        flavorText = flavor;
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

        s_instance.InternalBegin(source, card, live);
    }

    public static void EndHover(object source)
    {
        s_instance?.InternalEnd(source);
    }

    public static void HideAll()
    {
        s_instance?.HideImmediate();
    }

    void InternalBegin(object source, ModuleCardData card, ModuleBase live)
    {
        bool same = _hovering &&
                    ReferenceEquals(_hoverSource, source) &&
                    CardsEqual(_hoverCard, card) &&
                    _hoverLive == live;
        _hoverSource = source;
        _hoverCard = card;
        _hoverLive = live;
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

        // 拖拽时不弹
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
        ModuleCardData card = _hoverLive != null ? _hoverLive.CardData : _hoverCard;
        if (nameText != null)
        {
            nameText.text = ModuleCatalog.GetDisplayName(card);
        }

        if (icon != null)
        {
            icon.sprite = _skin != null ? _skin.GetModuleIcon(card.Type) : PrototypeSprites.Square;
            icon.color = ModuleCatalog.GetDisplayColor(card.Type);
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

        if (card.Type == ModuleType.IceLaser)
        {
            int dmg = live is IceLaserModule ice
                ? ice.DamagePerShot
                : ModuleCatalog.GetDamagePerShot(card.Level);
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

        if (card.Type == ModuleType.Projectile || ModuleCatalog.IsAttackModule(card.Type))
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
        return a.Type == b.Type && a.Level == b.Level && a.InvestedGold == b.InvestedGold;
    }

    void HideImmediate()
    {
        _hovering = false;
        _hoverSource = null;
        _hoverLive = null;
        _hoverCard = default;
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
