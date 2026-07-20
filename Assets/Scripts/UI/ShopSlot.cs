using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 商店货架槽：点击购入；视觉交给 ModuleSlotView。
/// </summary>
public class ShopSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    ShopController _shop;
    ModuleSlotView _view;
    int _index;
    ModuleType _moduleType;
    bool _occupied;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _moduleType;

    public void Setup(ShopController shop, int index, ModuleSlotView view)
    {
        _shop = shop;
        _index = index;
        _view = view;
        Clear();
    }

    public void SetOffer(ModuleType type)
    {
        _occupied = true;
        _moduleType = type;
        _view?.SetModule(type);
        _view?.SetState(ModuleSlotView.SlotVisualState.Normal);
    }

    public void Clear()
    {
        _occupied = false;
        _view?.SetEmpty();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _shop == null)
        {
            return;
        }

        _shop.TryPurchaseSlot(_index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_occupied)
        {
            _view?.SetState(ModuleSlotView.SlotVisualState.Hover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_occupied)
        {
            _view?.SetState(ModuleSlotView.SlotVisualState.Normal);
        }
        else
        {
            _view?.SetEmpty();
        }
    }
}
