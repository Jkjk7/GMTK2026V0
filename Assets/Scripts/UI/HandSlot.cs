using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 单个手牌槽 UI。
/// 点击后通知 HandController 选中本槽；显示模块类型色块与名称。
/// </summary>
public class HandSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] Image background;
    [SerializeField] Image icon;
    [SerializeField] Text label;

    HandController _hand;
    int _index;
    ModuleType _moduleType;
    bool _occupied;
    bool _selected;

    /// <summary>槽位是否有牌。</summary>
    public bool IsOccupied => _occupied;

    /// <summary>本槽模块类型（仅 occupied 时有效）。</summary>
    public ModuleType ModuleType => _moduleType;

    /// <summary>
    /// 由 HandController 初始化。
    /// </summary>
    public void Setup(HandController hand, int index, Image bg, Image iconImage, Text nameLabel)
    {
        _hand = hand;
        _index = index;
        background = bg;
        icon = iconImage;
        label = nameLabel;
        Clear();
    }

    /// <summary>
    /// 放入一张指定类型的手牌。
    /// </summary>
    public void SetCard(ModuleType type)
    {
        _occupied = true;
        _moduleType = type;
        if (label != null)
        {
            label.text = type == global::ModuleType.Redirector ? "收束器" : "射弹塔";
        }

        if (icon != null)
        {
            icon.color = type == global::ModuleType.Redirector
                ? new Color(0.4f, 0.75f, 0.95f, 1f)
                : new Color(0.9f, 0.35f, 0.25f, 1f);
        }

        SetSelected(false);
    }

    /// <summary>
    /// 清空槽位（放置消耗后）。
    /// </summary>
    public void Clear()
    {
        _occupied = false;
        _selected = false;
        if (label != null)
        {
            label.text = "空";
        }

        if (icon != null)
        {
            icon.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        }

        if (background != null)
        {
            background.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);
        }
    }

    /// <summary>
    /// 更新选中高亮。
    /// </summary>
    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (background != null)
        {
            background.color = selected
                ? new Color(0.35f, 0.55f, 0.25f, 0.95f)
                : new Color(0.15f, 0.15f, 0.18f, 0.9f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _hand == null)
        {
            return;
        }

        _hand.SelectSlot(_index);
    }
}
