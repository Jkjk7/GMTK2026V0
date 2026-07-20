using UnityEngine;

/// <summary>
/// 底部手牌区：最多 8 槽；支持选中、加入、合成、消耗。
/// </summary>
public class HandController : MonoBehaviour
{
    public const int SlotCount = 8;

    HandSlot[] _slots;
    int _selectedIndex = -1;

    public int SelectedIndex => _selectedIndex;

    public int OccupiedCount
    {
        get
        {
            if (_slots == null)
            {
                return 0;
            }

            int n = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].IsOccupied)
                {
                    n++;
                }
            }

            return n;
        }
    }

    public bool IsFull => OccupiedCount >= SlotCount;

    public bool HasSelection =>
        _selectedIndex >= 0 &&
        _selectedIndex < SlotCount &&
        _slots != null &&
        _slots[_selectedIndex] != null &&
        _slots[_selectedIndex].IsOccupied;

    public ModuleType SelectedModuleType => _slots[_selectedIndex].CardData.Type;

    public ModuleCardData SelectedCard => _slots[_selectedIndex].CardData;

    public HandSlot GetSlot(int index)
    {
        if (_slots == null || index < 0 || index >= _slots.Length)
        {
            return null;
        }

        return _slots[index];
    }

    public void BindSlots(HandSlot[] slots)
    {
        _slots = slots;
        _selectedIndex = -1;
    }

    public void ClearHand()
    {
        if (_slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null)
            {
                _slots[i].Clear();
            }
        }

        _selectedIndex = -1;
    }

    public bool TryAddCard(ModuleType type)
    {
        return TryAddCard(ModuleCardData.Create(type, 1, 0));
    }

    public bool TryAddCard(ModuleCardData card)
    {
        if (_slots == null)
        {
            return false;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && !_slots[i].IsOccupied)
            {
                _slots[i].SetCard(card);
                return true;
            }
        }

        return false;
    }

    public bool TryFuseIntoSlot(int targetIndex, ModuleCardData incoming)
    {
        HandSlot target = GetSlot(targetIndex);
        if (target == null || !target.IsOccupied)
        {
            return false;
        }

        if (!target.CardData.CanFuseWith(incoming))
        {
            return false;
        }

        target.SetCard(target.CardData.FusedWith(incoming));
        return true;
    }

    public void SelectSlot(int index)
    {
        if (_slots == null || index < 0 || index >= _slots.Length)
        {
            return;
        }

        if (!_slots[index].IsOccupied)
        {
            return;
        }

        if (_selectedIndex == index)
        {
            _slots[index].SetSelected(false);
            _selectedIndex = -1;
            return;
        }

        if (_selectedIndex >= 0 && _selectedIndex < _slots.Length)
        {
            _slots[_selectedIndex].SetSelected(false);
        }

        _selectedIndex = index;
        _slots[index].SetSelected(true);
    }

    public void ClearSelection()
    {
        if (_selectedIndex >= 0 && _slots != null && _selectedIndex < _slots.Length)
        {
            _slots[_selectedIndex].SetSelected(false);
        }

        _selectedIndex = -1;
    }

    public void ConsumeSelected()
    {
        if (!HasSelection)
        {
            return;
        }

        _slots[_selectedIndex].Clear();
        _selectedIndex = -1;
    }

    public bool TryConsumeSlot(int index, out ModuleCardData card)
    {
        card = default;
        HandSlot slot = GetSlot(index);
        if (slot == null || !slot.IsOccupied)
        {
            return false;
        }

        card = slot.CardData;
        if (_selectedIndex == index)
        {
            _selectedIndex = -1;
        }

        slot.Clear();
        return true;
    }
}
