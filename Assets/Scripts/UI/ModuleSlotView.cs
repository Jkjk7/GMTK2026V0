using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 手牌/商店槽位的纯视觉层。逻辑槽只调用这些方法，不直接改色。
/// </summary>
public sealed class ModuleSlotView : MonoBehaviour
{
    public enum SlotVisualState
    {
        Empty,
        Normal,
        Hover,
        Selected,
        Disabled
    }

    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] Image selectionFrame;
    [SerializeField] Image rarityBar;
    [SerializeField] Text nameText;
    [SerializeField] Text priceText;

    GameSkin _skin;
    SlotVisualState _state = SlotVisualState.Empty;
    ModuleType? _moduleType;

    static readonly Color EmptyBg = new Color(0.65f, 0.7f, 0.8f, 0.65f);
    static readonly Color NormalBg = Color.white;
    static readonly Color HoverBg = new Color(1.15f, 1.15f, 1.15f, 1f);
    static readonly Color SelectedBg = new Color(0.9f, 1.2f, 1.05f, 1f);
    static readonly Color DisabledBg = new Color(0.65f, 0.65f, 0.75f, 0.8f);

    public void Bind(
        Image bg,
        Image iconImage,
        Text label,
        Image frame = null,
        GameSkin skin = null,
        Text priceLabel = null,
        Image rarity = null)
    {
        background = bg;
        icon = iconImage;
        nameText = label;
        selectionFrame = frame;
        priceText = priceLabel;
        rarityBar = rarity;
        _skin = skin;
        ApplyState();
    }

    public void SetSkin(GameSkin skin)
    {
        _skin = skin;
        ApplyState();
    }

    public void SetEmpty()
    {
        _moduleType = null;
        _state = SlotVisualState.Empty;
        if (nameText != null)
        {
            nameText.text = GameLocalization.Text("Empty", "空");
            nameText.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        }

        if (priceText != null)
        {
            priceText.text = string.Empty;
        }

        if (rarityBar != null)
        {
            rarityBar.enabled = false;
        }

        if (icon != null)
        {
            icon.sprite = PixelUiSprites.EmptySlot;
            icon.color = Color.white;
            icon.preserveAspect = true;
        }

        ApplyState();
    }

    public void SetModule(ModuleType type)
    {
        SetCard(ModuleCardData.Create(type, 1, 0), -1, true);
    }

    public void SetCard(ModuleCardData card, int price = -1, bool affordable = true)
    {
        _moduleType = card.Type;
        // 刷新时清掉 Hover，避免离开后仍高亮
        if (_state == SlotVisualState.Empty ||
            _state == SlotVisualState.Disabled ||
            _state == SlotVisualState.Hover)
        {
            _state = SlotVisualState.Normal;
        }

        if (nameText != null)
        {
            ModuleRarity rarity = ModuleCatalog.GetRarity(card.Type);
            nameText.text = ModuleCatalog.GetDisplayName(card);
            nameText.color = ModuleCatalog.GetRarityColor(rarity);
        }

        if (icon != null)
        {
            ModuleIconVisuals.Apply(icon, card.Type, !affordable && price >= 0);
        }

        if (rarityBar != null)
        {
            rarityBar.enabled = true;
            Color rc = ModuleCatalog.GetRarityColor(ModuleCatalog.GetRarity(card.Type));
            rc.a = 0.95f;
            rarityBar.color = rc;
        }

        if (priceText != null)
        {
            if (price >= 0)
            {
                priceText.text = price.ToString();
                priceText.color = affordable
                    ? new Color(0.95f, 0.82f, 0.25f, 1f)
                    : new Color(0.55f, 0.35f, 0.3f, 1f);
            }
            else
            {
                priceText.text = string.Empty;
            }
        }

        if (!affordable && price >= 0)
        {
            _state = SlotVisualState.Disabled;
        }
        else if (affordable && _state == SlotVisualState.Disabled)
        {
            _state = SlotVisualState.Normal;
        }

        ApplyState();
    }

    public void SetAffordable(bool affordable)
    {
        if (_moduleType == null)
        {
            return;
        }

        if (!affordable)
        {
            SetState(SlotVisualState.Disabled);
            if (priceText != null)
            {
                priceText.color = new Color(0.55f, 0.35f, 0.3f, 1f);
            }
        }
        else if (_state == SlotVisualState.Disabled)
        {
            SetState(SlotVisualState.Normal);
            if (priceText != null)
            {
                priceText.color = new Color(0.95f, 0.82f, 0.25f, 1f);
            }
        }
    }

    public void SetState(SlotVisualState state)
    {
        if (_moduleType == null && state != SlotVisualState.Empty && state != SlotVisualState.Disabled)
        {
            state = SlotVisualState.Empty;
        }

        _state = state;
        ApplyState();
    }

    void ApplyState()
    {
        if (background != null)
        {
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 2f;
            switch (_state)
            {
                case SlotVisualState.Empty:
                    background.color = EmptyBg;
                    background.sprite = _skin != null ? _skin.GetSlotBackground(false, true) : background.sprite;
                    break;
                case SlotVisualState.Hover:
                    background.color = HoverBg;
                    break;
                case SlotVisualState.Selected:
                    background.color = SelectedBg;
                    background.sprite = _skin != null ? _skin.GetSlotBackground(true, false) : background.sprite;
                    break;
                case SlotVisualState.Disabled:
                    background.color = DisabledBg;
                    break;
                default:
                    background.color = NormalBg;
                    background.sprite = _skin != null ? _skin.GetSlotBackground(false, false) : background.sprite;
                    break;
            }
        }

        if (selectionFrame != null)
        {
            selectionFrame.sprite = PixelUiSprites.Frame;
            selectionFrame.type = Image.Type.Sliced;
            selectionFrame.pixelsPerUnitMultiplier = 2f;
            selectionFrame.enabled = _state == SlotVisualState.Selected || _state == SlotVisualState.Hover;
            selectionFrame.color = _state == SlotVisualState.Selected
                ? new Color(0.55f, 0.95f, 0.45f, 0.9f)
                : new Color(0.55f, 0.75f, 1f, 0.55f);
        }

        if (icon != null && _moduleType != null)
        {
            ModuleIconVisuals.Apply(
                icon,
                _moduleType.Value,
                _state == SlotVisualState.Disabled);
        }
    }
}
