using UnityEngine;

/// <summary>
/// 底部手牌区控制器。
/// 职责：维护约 5 个槽、选中状态、开局发牌、放置后消耗。
/// 不负责：实际往棋盘实例化模块（交给 PlacementController）。
/// </summary>
public class HandController : MonoBehaviour
{
    public const int SlotCount = 5;

    HandSlot[] _slots;
    int _selectedIndex = -1;

    /// <summary>当前选中槽下标；无选中为 -1。</summary>
    public int SelectedIndex => _selectedIndex;

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
    /// 开局手牌：3 收束器 + 2 射弹塔。
    /// </summary>
    public void DealStartingHand()
    {
        if (_slots == null || _slots.Length < SlotCount)
        {
            return;
        }

        _slots[0].SetCard(global::ModuleType.Redirector);
        _slots[1].SetCard(global::ModuleType.Redirector);
        _slots[2].SetCard(global::ModuleType.Redirector);
        _slots[3].SetCard(global::ModuleType.Projectile);
        _slots[4].SetCard(global::ModuleType.Projectile);
        _selectedIndex = -1;
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
