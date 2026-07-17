using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 商店单个货架槽。
/// 点击后尝试免费购入到手牌；成功则清空本槽，等待下次刷新。
/// 暂无金币；预留扩展购买校验。
/// </summary>
public class ShopSlot : MonoBehaviour, IPointerClickHandler
{
    Image _background;
    Image _icon;
    Text _label;
    ShopController _shop;
    int _index;
    ModuleType _moduleType;
    bool _occupied;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _moduleType;

    /// <summary>
    /// 由 ShopController 初始化视觉引用。
    /// </summary>
    public void Setup(ShopController shop, int index, Image background, Image icon, Text label)
    {
        _shop = shop;
        _index = index;
        _background = background;
        _icon = icon;
        _label = label;
        Clear();
    }

    /// <summary>
    /// 上架一种模块（随机刷新时调用）。
    /// </summary>
    public void SetOffer(ModuleType type)
    {
        _occupied = true;
        _moduleType = type;
        if (_label != null)
        {
            _label.text = ModuleCatalog.GetDisplayName(type);
            _label.color = Color.white;
        }

        if (_icon != null)
        {
            _icon.color = ModuleCatalog.GetDisplayColor(type);
        }

        if (_background != null)
        {
            _background.color = new Color(0.18f, 0.2f, 0.24f, 0.95f);
        }
    }

    /// <summary>
    /// 清空货架（购入后或刷新前）。
    /// </summary>
    public void Clear()
    {
        _occupied = false;
        if (_label != null)
        {
            _label.text = "空";
            _label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        }

        if (_icon != null)
        {
            _icon.color = new Color(0.25f, 0.25f, 0.28f, 1f);
        }

        if (_background != null)
        {
            _background.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _shop == null)
        {
            return;
        }

        _shop.TryPurchaseSlot(_index);
    }
}
