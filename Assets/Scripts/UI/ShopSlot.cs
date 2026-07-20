using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 商店货架槽：显示价格；买不起灰显；点击购入。
/// </summary>
public class ShopSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    ShopController _shop;
    ModuleSlotView _view;
    int _index;
    ModuleCardData _card;
    int _price;
    bool _occupied;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _card.Type;
    public ModuleCardData CardData => _card;
    public int Price => _price;

    public void Setup(ShopController shop, int index, ModuleSlotView view)
    {
        _shop = shop;
        _index = index;
        _view = view;
        Clear();
    }

    public void SetOffer(ModuleCardData card, int price)
    {
        _occupied = true;
        _card = card;
        _price = price;
        bool affordable = Economy.Instance == null || Economy.Instance.CanAfford(price);
        _view?.SetCard(card, price, affordable);
    }

    public void RefreshAffordability(int gold)
    {
        if (!_occupied)
        {
            return;
        }

        bool affordable = gold >= _price;
        _view?.SetCard(_card, _price, affordable);
    }

    public void Clear()
    {
        _occupied = false;
        _card = default;
        _price = 0;
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
            bool affordable = Economy.Instance == null || Economy.Instance.CanAfford(_price);
            if (affordable)
            {
                _view?.SetState(ModuleSlotView.SlotVisualState.Hover);
            }

            ModuleTooltipView.BeginHover(this, _card);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ModuleTooltipView.EndHover(this);
        if (_occupied)
        {
            int gold = Economy.Instance != null ? Economy.Instance.CurrentGold : 9999;
            bool affordable = gold >= _price;
            _view?.SetCard(_card, _price, affordable);
            if (affordable)
            {
                _view?.SetState(ModuleSlotView.SlotVisualState.Normal);
            }
        }
        else
        {
            _view?.SetEmpty();
        }
    }
}
