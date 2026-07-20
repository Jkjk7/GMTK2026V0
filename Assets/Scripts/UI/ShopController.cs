using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店：扣费购买、刷新价、阶段货架、买不起灰显。
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
    int _refreshIndexInWave;
    int _lastWaveSeen = -1;

    public int CurrentRefreshCost
    {
        get
        {
            int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
            return ModulePricing.GetRefreshCost(wave, _refreshIndexInWave);
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
        _refreshIndexInWave = 0;
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged += OnGoldChanged;
        }

        RerollShop();
        UpdateRefreshLabel();
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged -= OnGoldChanged;
        }
    }

    void OnGoldChanged(int _) => RefreshAffordability();

    void Update()
    {
        if (_session != null && !_session.IsRunActive)
        {
            return;
        }

        if (_waves != null && _waves.IsCountdownPhase)
        {
            return;
        }

        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        if (wave != _lastWaveSeen)
        {
            _lastWaveSeen = wave;
            _refreshIndexInWave = 0;
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

        if (_waves != null && _waves.IsCountdownPhase)
        {
            return;
        }

        int cost = CurrentRefreshCost;
        if (Economy.Instance != null && !Economy.Instance.TrySpend(cost))
        {
            return;
        }

        _refreshIndexInWave++;
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

            ModuleType type = ModuleCatalog.RollShopSlotType(i);
            int level = ModuleCatalog.IsAttackModule(type)
                ? ModulePricing.RollAttackLevel(wave)
                : 1;
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
        if (_session != null && !_session.IsRunActive)
        {
            return false;
        }

        if (_waves != null && _waves.IsCountdownPhase)
        {
            return false;
        }

        if (_hand == null || _slots == null || index < 0 || index >= _slots.Length)
        {
            return false;
        }

        ShopSlot slot = _slots[index];
        if (slot == null || !slot.IsOccupied)
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
}
