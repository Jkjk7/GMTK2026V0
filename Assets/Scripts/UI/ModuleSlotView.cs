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
    [SerializeField] Text nameText;

    GameSkin _skin;
    SlotVisualState _state = SlotVisualState.Empty;
    ModuleType? _moduleType;

    static readonly Color EmptyBg = new Color(0.12f, 0.12f, 0.15f, 0.85f);
    static readonly Color NormalBg = new Color(0.16f, 0.17f, 0.22f, 0.95f);
    static readonly Color HoverBg = new Color(0.22f, 0.28f, 0.38f, 0.98f);
    static readonly Color SelectedBg = new Color(0.28f, 0.48f, 0.28f, 0.98f);
    static readonly Color DisabledBg = new Color(0.1f, 0.1f, 0.1f, 0.7f);

    public void Bind(Image bg, Image iconImage, Text label, Image frame = null, GameSkin skin = null)
    {
        background = bg;
        icon = iconImage;
        nameText = label;
        selectionFrame = frame;
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
            nameText.text = "空";
            nameText.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        }

        if (icon != null)
        {
            icon.sprite = _skin != null ? _skin.ResolveSquare(null) : PrototypeSprites.Square;
            icon.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        }

        ApplyState();
    }

    public void SetModule(ModuleType type)
    {
        _moduleType = type;
        if (_state == SlotVisualState.Empty || _state == SlotVisualState.Disabled)
        {
            _state = SlotVisualState.Normal;
        }

        if (nameText != null)
        {
            nameText.text = ModuleCatalog.GetDisplayName(type);
            nameText.color = Color.white;
        }

        if (icon != null)
        {
            icon.sprite = _skin != null ? _skin.GetModuleIcon(type) : PrototypeSprites.Square;
            icon.color = ModuleCatalog.GetDisplayColor(type);
        }

        ApplyState();
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
            selectionFrame.enabled = _state == SlotVisualState.Selected || _state == SlotVisualState.Hover;
            selectionFrame.color = _state == SlotVisualState.Selected
                ? new Color(0.55f, 0.95f, 0.45f, 0.9f)
                : new Color(0.55f, 0.75f, 1f, 0.55f);
        }
    }
}
