using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 单个手牌槽：点击选中；拖拽放置/合成/分解。
/// </summary>
public class HandSlot : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    HandController _hand;
    ModuleSlotView _view;
    int _index;
    ModuleCardData _card;
    bool _occupied;
    bool _selected;
    bool _dragging;
    GameObject _dragIcon;
    Canvas _canvas;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _card.Type;
    public ModuleCardData CardData => _card;
    public int Index => _index;

    public void Setup(HandController hand, int index, ModuleSlotView view)
    {
        _hand = hand;
        _index = index;
        _view = view;
        _canvas = GetComponentInParent<Canvas>();
        Clear();
    }

    public void SetCard(ModuleCardData card)
    {
        _occupied = true;
        _card = card;
        _view?.SetCard(card);
        SetSelected(false);
    }

    public void Clear()
    {
        _occupied = false;
        _selected = false;
        _card = default;
        _view?.SetEmpty();
        CleanupDragIcon();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (!_occupied)
        {
            _view?.SetEmpty();
            return;
        }

        _view?.SetCard(_card);
        _view?.SetState(selected
            ? ModuleSlotView.SlotVisualState.Selected
            : ModuleSlotView.SlotVisualState.Normal);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _hand == null || _dragging)
        {
            return;
        }

        _hand.SelectSlot(_index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_occupied && !_selected && !_dragging)
        {
            _view?.SetState(ModuleSlotView.SlotVisualState.Hover);
        }

        if (_occupied && !_dragging)
        {
            ModuleTooltipView.BeginHover(this, _card);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_occupied && !_dragging)
        {
            SetSelected(_selected);
        }

        ModuleTooltipView.EndHover(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_occupied || _hand == null)
        {
            return;
        }

        _dragging = true;
        _hand.SelectSlot(_index);
        ModuleTooltipView.EndHover(this);
        CreateDragIcon(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging || _dragIcon == null)
        {
            return;
        }

        _dragIcon.transform.position = eventData.position;
        PlacementController.NotifyHandDrag(_card, eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        CleanupDragIcon();
        PlacementController.NotifyHandDrop(_hand, _index, eventData.position);
    }

    void CreateDragIcon(PointerEventData eventData)
    {
        CleanupDragIcon();
        _dragIcon = new GameObject("HandDragIcon");
        Transform parent = _canvas != null ? _canvas.transform : transform;
        _dragIcon.transform.SetParent(parent, false);
        var rt = _dragIcon.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(48f, 48f);
        var img = _dragIcon.AddComponent<Image>();
        ModuleIconVisuals.Apply(img, _card.Type);
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
