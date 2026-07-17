using UnityEngine;

/// <summary>
/// 商店控制器。
/// 职责：随机刷新货架、点击购入到手牌（当前免费）、按键刷新。
/// 不负责：金币扣费、稀有度卡池（预留 ModuleCatalog 扩展）。
/// </summary>
public class ShopController : MonoBehaviour
{
    public const int SlotCount = 6;

    [Header("Input")]
    [Tooltip("刷新商店的按键。")]
    [SerializeField] KeyCode refreshKey = KeyCode.F;

    HandController _hand;
    ShopSlot[] _slots;

    /// <summary>
    /// 注入手牌引用并绑定槽位，首次刷新货架。
    /// </summary>
    public void Initialize(HandController hand, ShopSlot[] slots)
    {
        _hand = hand;
        _slots = slots;
        RerollShop();
    }

    void Update()
    {
        if (Input.GetKeyDown(refreshKey))
        {
            RerollShop();
        }
    }

    /// <summary>
    /// 重新随机填充所有商店槽（当前池内均匀随机；仅两种时会铺满这两种）。
    /// </summary>
    public void RerollShop()
    {
        if (_slots == null)
        {
            return;
        }

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                continue;
            }

            _slots[i].SetOffer(ModuleCatalog.RollRandomType());
        }
    }

    /// <summary>
    /// 尝试购买指定槽：免费加入手牌；手满则失败保留货架。
    /// 返回是否购入成功。
    /// </summary>
    public bool TryPurchaseSlot(int index)
    {
        if (_hand == null || _slots == null || index < 0 || index >= _slots.Length)
        {
            return false;
        }

        ShopSlot slot = _slots[index];
        if (slot == null || !slot.IsOccupied)
        {
            return false;
        }

        // 预留：此处将来检查金币 / 稀有度限制
        if (!_hand.TryAddCard(slot.ModuleType))
        {
            Debug.Log("[Shop] 手牌已满（上限 " + HandController.SlotCount + "），无法购入。");
            return false;
        }

        slot.Clear();
        return true;
    }
}
