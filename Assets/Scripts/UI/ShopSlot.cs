using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店货架槽：显示价格；买不起灰显；点击购入；拖到棋盘购买并放置。
/// </summary>
public class ShopSlot : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    ShopController _shop;
    ModuleSlotView _view;
    int _index;
    ModuleCardData _card;
    int _price;
    bool _occupied;
    bool _dragging;
    GameObject _dragIcon;
    Canvas _canvas;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _card.Type;
    public ModuleCardData CardData => _card;
    public int Price => _price;

    public void Setup(ShopController shop, int index, ModuleSlotView view)
    {
        _shop = shop;
        _index = index;
        _view = view;
        _canvas = GetComponentInParent<Canvas>();
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
        CleanupDragIcon();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _shop == null || _dragging)
        {
            return;
        }

        _shop.TryPurchaseSlot(_index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_occupied && !_dragging)
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
        if (_dragging)
        {
            return;
        }

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_occupied || _shop == null)
        {
            return;
        }

        _dragging = true;
        ModuleTooltipView.EndHover(this);
        CreateDragIcon(eventData);
        PlacementController.NotifyShopDragBegin(_shop, _index, _card, _price);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
        {
            return;
        }

        if (_dragIcon != null)
        {
            _dragIcon.transform.position = eventData.position;
        }

        PlacementController.NotifyShopDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        CleanupDragIcon();
        PlacementController.NotifyShopDrop(eventData.position);

        if (_occupied)
        {
            int gold = Economy.Instance != null ? Economy.Instance.CurrentGold : 9999;
            bool affordable = gold >= _price;
            _view?.SetCard(_card, _price, affordable);
        }
    }

    void CreateDragIcon(PointerEventData eventData)
    {
        CleanupDragIcon();
        _dragIcon = new GameObject("ShopDragIcon");
        Transform parent = _canvas != null ? _canvas.transform : transform;
        _dragIcon.transform.SetParent(parent, false);
        var rt = _dragIcon.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);
        var img = _dragIcon.AddComponent<Image>();
        img.sprite = PrototypeSprites.Square;
        img.color = ModuleCatalog.GetDisplayColor(_card.Type);
        img.raycastTarget = false;
        _dragIcon.transform.position = eventData.position;
    }

    void CleanupDragIcon()
    {
        if (_dragIcon != null)
        {
            Destroy(_dragIcon);
            _dragIcon = null;
        }
    }
}
