using UnityEngine;

/// <summary>
/// 底部手牌区控制器。
/// 职责：维护最多 5 个槽、选中状态、从商店加入、放置后消耗。
/// 不负责：商店刷新、棋盘实例化（交给 ShopController / PlacementController）。
/// </summary>
public class HandController : MonoBehaviour
{
    public const int SlotCount = 5;

    HandSlot[] _slots;
    int _selectedIndex = -1;

    /// <summary>当前选中槽下标；无选中为 -1。</summary>
    public int SelectedIndex => _selectedIndex;

    /// <summary>已占用槽数量。</summary>
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

    /// <summary>手牌是否已满。</summary>
    public bool IsFull => OccupiedCount >= SlotCount;

    /// <summary>
    /// 是否有选中且非空的手牌。
    /// </summary>
    public bool HasSelection =>
        _selectedIndex >= 0 &&
        _selectedIndex < SlotCount &&
        _slots != null &&
        _slots[_selectedIndex] != null &&
        _slots[_selectedIndex].IsOccupied;

    /// <summary>
    /// 当前选中模块类型。调用前请确认 HasSelection。
    /// </summary>
    public ModuleType SelectedModuleType => _slots[_selectedIndex].ModuleType;

    /// <summary>
    /// 绑定由 GameBootstrap 创建的槽位组件。
    /// </summary>
    public void BindSlots(HandSlot[] slots)
    {
        _slots = slots;
        _selectedIndex = -1;
    }

    /// <summary>
    /// 开局清空手牌（模块从商店免费购入）。
    /// </summary>
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

    /// <summary>
    /// 尝试将一张模块加入第一个空槽。手满返回 false。
    /// </summary>
    public bool TryAddCard(ModuleType type)
    {
        if (_slots == null)
        {
            return false;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] != null && !_slots[i].IsOccupied)
            {
                _slots[i].SetCard(type);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 选中某槽；再次点击同一槽可取消选中。
    /// </summary>
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

    /// <summary>
    /// 放置成功后消耗当前选中手牌。
    /// </summary>
    public void ConsumeSelected()
    {
        if (!HasSelection)
        {
            return;
        }

        _slots[_selectedIndex].Clear();
        _selectedIndex = -1;
    }
}
