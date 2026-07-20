using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 单个手牌槽：点击选中；视觉交给 ModuleSlotView。
/// </summary>
public class HandSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    HandController _hand;
    ModuleSlotView _view;
    int _index;
    ModuleType _moduleType;
    bool _occupied;
    bool _selected;

    public bool IsOccupied => _occupied;
    public ModuleType ModuleType => _moduleType;

    public void Setup(HandController hand, int index, ModuleSlotView view)
    {
        _hand = hand;
        _index = index;
        _view = view;
        Clear();
    }

    public void SetCard(ModuleType type)
    {
        _occupied = true;
        _moduleType = type;
        _view?.SetModule(type);
        SetSelected(false);
    }

    public void Clear()
    {
        _occupied = false;
        _selected = false;
        _view?.SetEmpty();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        if (!_occupied)
        {
            _view?.SetEmpty();
            return;
        }

        _view?.SetState(selected
            ? ModuleSlotView.SlotVisualState.Selected
            : ModuleSlotView.SlotVisualState.Normal);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_occupied || _hand == null)
        {
            return;
        }

        _hand.SelectSlot(_index);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_occupied && !_selected)
        {
            _view?.SetState(ModuleSlotView.SlotVisualState.Hover);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_occupied)
        {
            SetSelected(_selected);
        }
    }
}
