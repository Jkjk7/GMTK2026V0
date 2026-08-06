using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店：按商店等级刷货与刷新价；可锁定阻止下一波自动刷新（进入该波准备后自动解锁）。
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
    Text _titleLabel;
    Text _lockLabel;
    Image _lockImage;
    bool _locked;
    int _lastWaveSeen = -1;

    public bool IsLocked => _locked;

    public int CurrentRefreshCost
    {
        get
        {
            if (RunModifiers.Instance != null && RunModifiers.Instance.FreeRefreshes > 0)
            {
                return 0;
            }

            int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
            int baseCost = ModulePricing.GetRefreshCost(wave);
            int mult = RunModifiers.Instance != null ? RunModifiers.Instance.RefreshCostMultiplier : 1;
            return Mathf.Max(0, baseCost * Mathf.Max(1, mult));
        }
    }

    public int CurrentShopLevel
    {
        get
        {
            int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
            return ModulePricing.GetShopLevel(wave);
        }
    }

    public void Initialize(
        HandController hand,
        ShopSlot[] slots,
        GameSession session,
        WaveManager waves = null,
        Text refreshLabel = null,
        Text titleLabel = null,
        Text lockLabel = null,
        Image lockImage = null)
    {
        _hand = hand;
        _slots = slots;
        _session = session;
        _waves = waves;
        _refreshLabel = refreshLabel;
        _titleLabel = titleLabel;
        _lockLabel = lockLabel;
        _lockImage = lockImage;
        _locked = false;
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged += OnGoldChanged;
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted += OnPrepStarted;
        }

        if (RunModifiers.Instance != null)
        {
            RunModifiers.Instance.Changed += UpdateRefreshLabel;
        }

        RerollShop();
        _lastWaveSeen = _waves != null ? _waves.CurrentWaveDisplay : 1;
        UpdateRefreshLabel();
        UpdateLockVisual();
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

        if (RunModifiers.Instance != null)
        {
            RunModifiers.Instance.Changed -= UpdateRefreshLabel;
        }
    }

    void OnGoldChanged(int _) => RefreshAffordability();

    void OnPrepStarted(int wave, float duration)
    {
        // 锁定只保护「进入本波准备」这一次不刷新；开局后自动解锁，避免玩家忘了解锁
        bool skipReroll = _locked;
        if (_locked)
        {
            _locked = false;
            UpdateLockVisual();
        }

        // 每一波结束后进入下一波准备时免费刷新（刚被锁定保护的那次除外）
        if (wave > 1 && !skipReroll)
        {
            RerollShop();
        }

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

        if (RunModifiers.Instance != null && RunModifiers.Instance.TryConsumeFreeRefresh())
        {
            RerollShop();
            UpdateRefreshLabel();
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
            if (ModulePricing.IsLevelableInShop(type))
            {
                level = ModulePricing.ClampOfferLevelForType(
                    type,
                    ModulePricing.RollOfferLevel(wave));
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

    public void ToggleLock()
    {
        _locked = !_locked;
        UpdateLockVisual();
        UpdateRefreshLabel();
    }

    void UpdateLockVisual()
    {
        if (_lockLabel != null)
        {
            _lockLabel.text = _locked
                ? GameLocalization.Text("Locked", "已锁定")
                : GameLocalization.Text("Lock", "锁定");
        }

        if (_lockImage != null)
        {
            _lockImage.color = _locked
                ? new Color(0.45f, 0.28f, 0.18f, 0.98f)
                : new Color(0.18f, 0.2f, 0.26f, 0.95f);
        }
    }

    void UpdateRefreshLabel()
    {
        int shopLv = CurrentShopLevel;
        if (_titleLabel != null)
        {
            string lockTag = _locked
                ? GameLocalization.Text(" [LOCK]", " [锁]")
                : string.Empty;
            _titleLabel.text = GameLocalization.Text(
                $"SHOP Lv{shopLv}{lockTag}",
                $"商店 Lv{shopLv}{lockTag}");
        }

        if (_refreshLabel == null)
        {
            return;
        }

        int cost = CurrentRefreshCost;
        bool can = Economy.Instance == null || Economy.Instance.CanAfford(cost);
        int free = RunModifiers.Instance != null ? RunModifiers.Instance.FreeRefreshes : 0;
        if (free > 0)
        {
            _refreshLabel.text = GameLocalization.Text(
                $"Refresh FREE x{free}",
                $"刷新 免费 x{free}");
        }
        else
        {
            _refreshLabel.text = GameLocalization.Text($"Refresh {cost}", $"刷新 {cost}");
        }

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
