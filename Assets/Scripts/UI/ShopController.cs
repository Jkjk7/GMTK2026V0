using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店：扣费购买、恒定刷新价、阶段货架、买不起灰显；波末免费自动刷新。
/// </summary>
public class ShopController : MonoBehaviour
{
    public const int SlotCount = 6;

    [SerializeField] KeyCode refreshKey = KeyCode.F;

    HandController _hand;
    ShopSlot[] _slots;
    GameSession _session;
    WaveManager _waves;
    Text _refreshLabel;
    int _lastWaveSeen = -1;

    public int CurrentRefreshCost
    {
        get
        {
            int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
            return ModulePricing.GetRefreshCost(wave);
        }
    }

    public void Initialize(
        HandController hand,
        ShopSlot[] slots,
        GameSession session,
        WaveManager waves = null,
        Text refreshLabel = null)
    {
        _hand = hand;
        _slots = slots;
        _session = session;
        _waves = waves;
        _refreshLabel = refreshLabel;
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged += OnGoldChanged;
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted += OnPrepStarted;
        }

        RerollShop();
        _lastWaveSeen = _waves != null ? _waves.CurrentWaveDisplay : 1;
        UpdateRefreshLabel();
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged -= OnGoldChanged;
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted -= OnPrepStarted;
        }
    }

    void OnGoldChanged(int _) => RefreshAffordability();

    void OnPrepStarted(int wave, float duration)
    {
        // 每一波结束后进入下一波准备时免费刷新；开局第 1 波已在 Initialize 刷过
        if (wave <= 1)
        {
            return;
        }

        RerollShop();
        UpdateRefreshLabel();
    }

    void Update()
    {
        if (_session != null && !_session.IsRunActive)
        {
            return;
        }

        if (_waves != null && (_waves.IsCountdownPhase || _waves.IsAwaitingDraft))
        {
            return;
        }

        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        if (wave != _lastWaveSeen)
        {
            _lastWaveSeen = wave;
            UpdateRefreshLabel();
        }

        if (Input.GetKeyDown(refreshKey))
        {
            TryRefreshPaid();
        }
    }

    public void TryRefreshPaid()
    {
        if (_session != null && !_session.IsRunActive)
        {
            return;
        }

        if (_waves != null && (_waves.IsCountdownPhase || _waves.IsAwaitingDraft))
        {
            return;
        }

        int cost = CurrentRefreshCost;
        if (Economy.Instance != null && !Economy.Instance.TrySpend(cost))
        {
            return;
        }

        RerollShop();
        UpdateRefreshLabel();
    }

    public void RerollShop()
    {
        if (_slots == null)
        {
            return;
        }

        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == null)
            {
                continue;
            }

            ModuleType type = ModuleCatalog.RollShopSlotType(i, wave);
            int level = 1;
            if (ModuleCatalog.IsAttackModule(type) || type == ModuleType.FlameAmp)
            {
                level = ModulePricing.RollAttackLevel(wave);
            }
            else if (type == ModuleType.Miner)
            {
                int rolled = ModulePricing.RollAttackLevel(wave);
                level = Mathf.Clamp(rolled, 1, 3);
            }

            int price = ModulePricing.GetShopPrice(type, level, wave);
            var offer = ModuleCardData.Create(type, level, 0);
            _slots[i].SetOffer(offer, price);
        }

        RefreshAffordability();
    }

    public void RefreshAffordability()
    {
        if (_slots == null)
        {
            return;
        }

        int gold = Economy.Instance != null ? Economy.Instance.CurrentGold : 9999;
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i]?.RefreshAffordability(gold);
        }

        UpdateRefreshLabel();
    }

    void UpdateRefreshLabel()
    {
        if (_refreshLabel == null)
        {
            return;
        }

        int cost = CurrentRefreshCost;
        bool can = Economy.Instance == null || Economy.Instance.CanAfford(cost);
        _refreshLabel.text = $"刷新 {cost}";
        _refreshLabel.color = can
            ? new Color(0.95f, 0.85f, 0.4f, 1f)
            : new Color(0.5f, 0.35f, 0.3f, 1f);
    }

    public bool TryPurchaseSlot(int index)
    {
        if (!CanInteractShop())
        {
            return false;
        }

        if (_hand == null || !TryGetOccupiedSlot(index, out ShopSlot slot))
        {
            return false;
        }

        int price = slot.Price;
        if (_hand.IsFull)
        {
            Debug.Log("[Shop] 手牌已满（上限 " + HandController.SlotCount + "），无法购入。");
            return false;
        }

        if (Economy.Instance != null && !Economy.Instance.TrySpend(price))
        {
            return false;
        }

        ModuleCardData purchased = ModuleCardData.FromShopPurchase(
            slot.CardData.Type,
            slot.CardData.Level,
            price);
        if (!_hand.TryAddCard(purchased))
        {
            Economy.Instance?.AddGold(price, silent: true);
            return false;
        }

        slot.Clear();
        RefreshAffordability();
        return true;
    }

    /// <summary>拖到棋盘直接购买：不经手牌。</summary>
    public bool TryPurchaseForBoard(int index, out ModuleCardData purchased, out int pricePaid)
    {
        purchased = default;
        pricePaid = 0;
        if (!CanInteractShop() || !TryGetOccupiedSlot(index, out ShopSlot slot))
        {
            return false;
        }

        pricePaid = slot.Price;
        if (Economy.Instance != null && !Economy.Instance.TrySpend(pricePaid))
        {
            pricePaid = 0;
            return false;
        }

        purchased = ModuleCardData.FromShopPurchase(
            slot.CardData.Type,
            slot.CardData.Level,
            pricePaid);
        slot.Clear();
        RefreshAffordability();
        return true;
    }

    public void RestoreOffer(int index, ModuleCardData card, int price)
    {
        if (_slots == null || index < 0 || index >= _slots.Length || _slots[index] == null)
        {
            return;
        }

        _slots[index].SetOffer(card, price);
        RefreshAffordability();
    }

    bool CanInteractShop()
    {
        if (_session != null && !_session.IsRunActive)
        {
            return false;
        }

        if (_waves != null && (_waves.IsCountdownPhase || _waves.IsAwaitingDraft))
        {
            return false;
        }

        return true;
    }

    bool TryGetOccupiedSlot(int index, out ShopSlot slot)
    {
        slot = null;
        if (_slots == null || index < 0 || index >= _slots.Length)
        {
            return false;
        }

        slot = _slots[index];
        return slot != null && slot.IsOccupied;
    }
}
