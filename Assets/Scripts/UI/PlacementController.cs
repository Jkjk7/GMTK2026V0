using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 放置 / 库存拖拽 / 拆除二次确认 / 分解确认（手牌或战场）。
/// 准备/战斗均可合成；长悬停显示模块详情（棋盘/手牌/商店）。
/// </summary>
public class PlacementController : MonoBehaviour
{
    static PlacementController s_instance;

    GridBoard _board;
    HandController _hand;
    Transform _moduleRoot;
    GameSession _session;
    WaveManager _waves;
    ScrapZone _scrapZone;
    ConfirmPromptView _confirm;
    BoardExpandService _boardExpand;
    Camera _gameplayCamera;
    GameSkin _skin;
    GridCoord? _hoveredCell;
    GridCellView _hoveredCellView;

    int _previewOrientation;

    RedirectorModule _ghostRedirector;
    ProjectileModule _ghostProjectile;
    ModuleBase _ghostOther;
    ModuleType? _ghostType;

    // 拆除确认
    GridCoord? _pendingDismantleCell;
    ModuleBase _pendingDismantleModule;
    float _pendingTimeout;
    SpriteRenderer _pendingOutline;

    // 棋盘拖移（确认后移动 / 互换）
    bool _boardDragging;
    ModuleBase _boardDragModule;
    GridCoord _boardDragFrom;
    GridCoord? _pendingMoveTo;
    ModuleBase _pendingMoveModule;
    ModuleBase _pendingSwapPartner;

    // 商店拖到棋盘：购买并放置
    bool _shopDragging;
    ShopController _shopDragShop;
    int _shopDragIndex = -1;
    ModuleCardData _shopDragCard;
    int _shopDragPrice;

    // 分解确认（来自手牌）
    int _pendingScrapHandIndex = -1;
    ModuleCardData _pendingScrapCard;

    // 分解确认（来自战场拖入）
    ModuleBase _pendingBoardScrapModule;
    GridCoord _pendingBoardScrapFrom;

    const float PendingTimeoutSeconds = 5f;

    public void Initialize(
        GridBoard board,
        HandController hand,
        Transform moduleRoot,
        GameSession session,
        Camera gameplayCamera = null,
        GameSkin skin = null,
        WaveManager waves = null,
        ScrapZone scrapZone = null,
        ConfirmPromptView confirm = null,
        ModuleTooltipView tooltip = null,
        BoardExpandService boardExpand = null)
    {
        s_instance = this;
        _board = board;
        _hand = hand;
        _moduleRoot = moduleRoot;
        _session = session;
        _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        _skin = skin;
        _waves = waves;
        _scrapZone = scrapZone;
        _confirm = confirm;
        _boardExpand = boardExpand;
        _previewOrientation = 0;
    }

    void OnDestroy()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }
    }

    public static void NotifyHandDrag(ModuleCardData card, Vector2 screenPos)
    {
        if (s_instance == null)
        {
            return;
        }

        Vector3 world = s_instance.ScreenToWorld(screenPos);
        if (s_instance._scrapZone != null && s_instance._scrapZone.ContainsWorldPoint(world))
        {
            s_instance._scrapZone.ShowHandPreview(card);
        }
        else if (s_instance._hand != null && s_instance._hand.HasSelection)
        {
            s_instance._scrapZone?.ShowHandPreview(card);
        }
        else
        {
            s_instance._scrapZone?.SetIdle();
        }
    }

    public static void NotifyHandDrop(HandController hand, int handIndex, Vector2 screenPos)
    {
        s_instance?.HandleHandDrop(hand, handIndex, screenPos);
    }

    public static void NotifyShopDragBegin(ShopController shop, int slotIndex, ModuleCardData card, int price)
    {
        if (s_instance == null)
        {
            return;
        }

        s_instance._shopDragging = true;
        s_instance._shopDragShop = shop;
        s_instance._shopDragIndex = slotIndex;
        s_instance._shopDragCard = card;
        s_instance._shopDragPrice = price;
        s_instance._previewOrientation = 0;
        ModuleTooltipView.HideAll();
    }

    public static void NotifyShopDrag(Vector2 screenPos)
    {
        s_instance?.UpdateShopDrag(screenPos);
    }

    public static void NotifyShopDrop(Vector2 screenPos)
    {
        s_instance?.FinishShopDrag(screenPos);
    }

    /// <summary>
    /// ESC 由局内设置面板统一开关，不再在此取消进行中操作。
    /// </summary>
    void Update()
    {
        if (_board == null || _hand == null)
        {
            return;
        }

        if (InGameSettingsHud.IsOpen)
        {
            return;
        }

        if (_session != null && !_session.IsRunActive)
        {
            ClearGhost();
            CancelShopDrag();
            CancelPendingDismantle();
            CancelPendingMove(restore: true);
            CancelPendingBoardScrap(restore: true);
            HideModuleTooltip();
            return;
        }

        if (_waves != null && (_waves.IsCountdownPhase || _waves.IsAwaitingDraft))
        {
            ClearGhost();
            CancelShopDrag();
            CancelPendingDismantle();
            CancelPendingMove(restore: true);
            CancelPendingBoardScrap(restore: true);
            HideModuleTooltip();
            if (_waves.IsCountdownPhase)
            {
                _confirm?.Close();
            }

            return;
        }

        HandleRotationInput();
        TickPendingTimeout();
        UpdateScrapHighlightFromSelection();
        UpdateModuleHoverTip();

        if (_boardDragging)
        {
            HideModuleTooltip();
            UpdateBoardDrag();
            return;
        }

        if (_shopDragging)
        {
            HideModuleTooltip();
            UpdateShopDrag(Input.mousePosition);
            return;
        }

        if (Input.GetMouseButtonDown(0) && !IsPointerOverUi())
        {
            HideModuleTooltip();
            TryClickScrapZone();
            if (!_hand.HasSelection)
            {
                if (!TryClickLockedCellForExpand())
                {
                    TryBeginBoardDrag();
                }
            }
        }

        if (IsPointerOverUi())
        {
            ClearGhost();
            ClearCellHover();
            HideModuleTooltip();
            return;
        }

        UpdateGhost();

        if (Input.GetMouseButtonDown(0) && _hand.HasSelection)
        {
            if (_pendingDismantleCell.HasValue)
            {
                Vector3 mw = GetMouseWorld();
                if (!_board.TryWorldToCell(mw, out GridCoord c) ||
                    !_pendingDismantleCell.Value.Equals(c))
                {
                    CancelPendingDismantle();
                }
            }

            TryPlaceAtMouse();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X))
        {
            TryDismantleGesture();
        }
    }

    bool TryClickLockedCellForExpand()
    {
        if (_boardExpand == null || _board == null)
        {
            return false;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            return false;
        }

        if (_board.IsBuildableCell(cell))
        {
            return false;
        }

        int cost = _boardExpand.GetNextExpandCost();
        if (cost <= 0)
        {
            return true;
        }

        int next = _boardExpand.GetNextSize();
        bool can = Economy.Instance == null || Economy.Instance.CanAfford(cost);
        if (!can)
        {
            Economy.Instance?.NotifyInsufficient();
        }

        _confirm?.Show(
            GameLocalization.Text($"Expand board to {next}×{next}?", $"扩展棋盘到 {next}×{next}？"),
            GameLocalization.Text(
                $"Unlock the outer cells\nCost: {cost} gold",
                $"解锁外围格子\n消耗 {cost} 金币"),
            can
                ? GameLocalization.Text($"Confirm -{cost}", $"确认 -{cost}")
                : GameLocalization.Text("Insufficient Gold", "金币不足"),
            can,
            can ? string.Empty : GameLocalization.Text("Insufficient Gold", "金币不足"),
            () => _boardExpand.TryExpand(),
            null);
        return true;
    }

    void TryBeginBoardDrag()
    {
        if (_pendingMoveModule != null)
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            return;
        }

        ModuleBase existing = _board.GetModule(cell);
        if (existing == null || existing.IsPermanentlyLocked)
        {
            return;
        }

        if (!_board.TryExtractModule(cell, out _boardDragModule) || _boardDragModule == null)
        {
            return;
        }

        CancelPendingDismantle();
        _boardDragFrom = cell;
        _boardDragging = true;
        if (_boardDragModule.CanRotate)
        {
            _previewOrientation = _boardDragModule.OrientationIndex;
        }

        ClearGhost();
    }

    void UpdateBoardDrag()
    {
        if (_boardDragModule == null)
        {
            _boardDragging = false;
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        _boardDragModule.transform.position = mouseWorld;

        bool overScrap = _scrapZone != null && _scrapZone.ContainsWorldPoint(mouseWorld);
        if (overScrap)
        {
            ClearCellHover();
            _scrapZone.ShowHandPreview(_boardDragModule.CardData);
        }
        else
        {
            _scrapZone?.SetIdle();
            if (_board.TryWorldToCell(mouseWorld, out GridCoord cell))
            {
                ModuleBase occupant = _board.GetModule(cell);
                bool canFuse = occupant != null &&
                               occupant.CardData.CanFuseWith(_boardDragModule.CardData);
                bool canSwap = occupant != null &&
                               !occupant.IsPermanentlyLocked &&
                               CanPlaceTypeAt(_boardDragModule.ModuleType, cell) &&
                               CanPlaceTypeAt(occupant.ModuleType, _boardDragFrom);
                bool canDrop = (_board.CanPlace(cell) && CanPlaceTypeAt(_boardDragModule.ModuleType, cell)) ||
                               canFuse ||
                               canSwap;
                UpdateCellHover(cell, canDrop);
            }
            else
            {
                ClearCellHover();
            }
        }

        if (!Input.GetMouseButton(0))
        {
            FinishBoardDrag(mouseWorld);
        }
    }

    void FinishBoardDrag(Vector3 mouseWorld)
    {
        _boardDragging = false;
        ModuleBase mod = _boardDragModule;
        _boardDragModule = null;
        ClearCellHover();
        if (mod == null)
        {
            return;
        }

        // 拖入分解区 → 确认分解
        if (_scrapZone != null && _scrapZone.ContainsWorldPoint(mouseWorld))
        {
            BeginBoardScrapConfirm(mod);
            return;
        }

        _scrapZone?.SetIdle();

        if (!_board.TryWorldToCell(mouseWorld, out GridCoord to) || to.Equals(_boardDragFrom))
        {
            _board.TryPlaceModule(_boardDragFrom, mod);
            return;
        }

        ModuleBase occupant = _board.GetModule(to);
        if (occupant != null)
        {
            if (occupant.CardData.CanFuseWith(mod.CardData))
            {
                if (TryApplyBoardFuse(occupant, to, mod.CardData))
                {
                    Destroy(mod.gameObject);
                }
                else
                {
                    _board.TryPlaceModule(_boardDragFrom, mod);
                }

                return;
            }

            // 诅咒锁定：不可互换
            if (occupant.IsPermanentlyLocked)
            {
                _board.TryPlaceModule(_boardDragFrom, mod);
                return;
            }

            // 不可合成 → 互换位置
            if (!CanPlaceTypeAt(mod.ModuleType, to) ||
                !CanPlaceTypeAt(occupant.ModuleType, _boardDragFrom))
            {
                _board.TryPlaceModule(_boardDragFrom, mod);
                return;
            }

            if (_session == null || _session.IsPreparing)
            {
                if (!TryExecuteBoardSwap(_boardDragFrom, to, mod, occupant))
                {
                    _board.TryPlaceModule(_boardDragFrom, mod);
                }

                return;
            }

            mod.transform.position = _board.CellToWorld(to);
            BeginSwapConfirm(_boardDragFrom, to, mod, occupant);
            return;
        }

        if (!_board.CanPlace(to) || !CanPlaceTypeAt(mod.ModuleType, to))
        {
            _board.TryPlaceModule(_boardDragFrom, mod);
            return;
        }

        // 准备阶段：直接移动，不弹确认
        if (_session == null || _session.IsPreparing)
        {
            if (!_board.TryPlaceModule(to, mod))
            {
                _board.TryPlaceModule(_boardDragFrom, mod);
            }

            return;
        }

        // 战斗阶段：预览目标格并确认（按拆除费率扣费）
        mod.transform.position = _board.CellToWorld(to);
        BeginMoveConfirm(_boardDragFrom, to, mod);
    }

    void BeginMoveConfirm(GridCoord from, GridCoord to, ModuleBase mod)
    {
        _pendingMoveTo = to;
        _pendingMoveModule = mod;
        _pendingSwapPartner = null;
        _boardDragFrom = from;
        _pendingTimeout = PendingTimeoutSeconds;
        ShowPendingOutline(mod);

        ModuleCardData card = mod.CardData;
        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        int cost = ModulePricing.GetDismantleCost(card, wave, inCombat);
        bool canAfford = cost <= 0 || Economy.Instance == null || Economy.Instance.CanAfford(cost);

        string name = ModuleCatalog.GetDisplayName(card);
        string feeLine = inCombat
            ? GameLocalization.Text(
                $"Combat move cost: {cost} gold (same as dismantle)",
                $"战斗中移动费用：{cost} 金币（同拆除）")
            : GameLocalization.Text("Preparation move: free", "准备阶段移动：免费");
        string warn = canAfford
            ? string.Empty
            : GameLocalization.Text("Insufficient Gold", "金币不足");
        if (!canAfford)
        {
            Economy.Instance?.NotifyInsufficient();
        }

        string confirmText = cost > 0
            ? GameLocalization.Text($"Confirm Move -{cost}", $"确认移动 -{cost}")
            : GameLocalization.Text("Confirm Move", "确认移动");
        _confirm?.Show(
            GameLocalization.Text($"Dismantle and move \"{name}\"?", $"拆除并移动「{name}」？"),
            GameLocalization.Text(
                $"{feeLine}\nFrom ({from.Col},{from.Row}) → ({to.Col},{to.Row})",
                $"{feeLine}\n从 ({from.Col},{from.Row}) → ({to.Col},{to.Row})"),
            confirmText,
            canAfford,
            warn,
            ExecutePendingMove,
            () => CancelPendingMove(restore: true));
    }

    void BeginSwapConfirm(GridCoord from, GridCoord to, ModuleBase mod, ModuleBase partner)
    {
        _pendingMoveTo = to;
        _pendingMoveModule = mod;
        _pendingSwapPartner = partner;
        _boardDragFrom = from;
        _pendingTimeout = PendingTimeoutSeconds;
        ShowPendingOutline(mod);

        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        int costA = ModulePricing.GetDismantleCost(mod.CardData, wave, inCombat);
        int costB = ModulePricing.GetDismantleCost(partner.CardData, wave, inCombat);
        int cost = costA + costB;
        bool canAfford = cost <= 0 || Economy.Instance == null || Economy.Instance.CanAfford(cost);

        string nameA = ModuleCatalog.GetDisplayName(mod.CardData);
        string nameB = ModuleCatalog.GetDisplayName(partner.CardData);
        string feeLine = GameLocalization.Text(
            $"Combat swap cost: {cost} gold ({costA}+{costB}, both dismantle fees)",
            $"战斗中互换费用：{cost} 金币（{costA}+{costB}，双方拆除费）");
        string warn = canAfford
            ? string.Empty
            : GameLocalization.Text("Insufficient Gold", "金币不足");
        if (!canAfford)
        {
            Economy.Instance?.NotifyInsufficient();
        }

        string confirmText = cost > 0
            ? GameLocalization.Text($"Confirm Swap -{cost}", $"确认互换 -{cost}")
            : GameLocalization.Text("Confirm Swap", "确认互换");
        _confirm?.Show(
            GameLocalization.Text(
                $"Swap \"{nameA}\" ↔ \"{nameB}\"?",
                $"互换「{nameA}」↔「{nameB}」？"),
            GameLocalization.Text(
                $"{feeLine}\n({from.Col},{from.Row}) ↔ ({to.Col},{to.Row})",
                $"{feeLine}\n({from.Col},{from.Row}) ↔ ({to.Col},{to.Row})"),
            confirmText,
            canAfford,
            warn,
            ExecutePendingMove,
            () => CancelPendingMove(restore: true));
    }

    bool TryExecuteBoardSwap(GridCoord from, GridCoord to, ModuleBase mod, ModuleBase partner)
    {
        if (mod == null || partner == null || _board == null)
        {
            return false;
        }

        if (!_board.TryExtractModule(to, out ModuleBase extracted) || extracted != partner)
        {
            return false;
        }

        if (!_board.TryPlaceModule(to, mod))
        {
            _board.TryPlaceModule(to, partner);
            return false;
        }

        if (!_board.TryPlaceModule(from, partner))
        {
            _board.TryExtractModule(to, out _);
            _board.TryPlaceModule(to, partner);
            _board.TryPlaceModule(from, mod);
            return false;
        }

        return true;
    }

    void ExecutePendingMove()
    {
        if (_pendingMoveModule == null || !_pendingMoveTo.HasValue)
        {
            CancelPendingMove(restore: true);
            return;
        }

        ModuleBase mod = _pendingMoveModule;
        GridCoord to = _pendingMoveTo.Value;
        ModuleBase partner = _pendingSwapPartner;
        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;

        int cost;
        if (partner != null)
        {
            cost = ModulePricing.GetDismantleCost(mod.CardData, wave, inCombat) +
                   ModulePricing.GetDismantleCost(partner.CardData, wave, inCombat);
        }
        else
        {
            cost = ModulePricing.GetDismantleCost(mod.CardData, wave, inCombat);
        }

        if (cost > 0)
        {
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost))
            {
                CancelPendingMove(restore: true);
                return;
            }
        }

        bool ok;
        if (partner != null)
        {
            ok = TryExecuteBoardSwap(_boardDragFrom, to, mod, partner);
        }
        else
        {
            ok = _board.CanPlace(to) && _board.TryPlaceModule(to, mod);
        }

        if (!ok)
        {
            if (cost > 0)
            {
                Economy.Instance?.AddGold(cost, silent: true);
            }

            CancelPendingMove(restore: true);
            return;
        }

        ClearPendingMoveState();
        _confirm?.Close();
    }

    void CancelPendingMove(bool restore)
    {
        if (restore && _pendingMoveModule != null)
        {
            _board.TryPlaceModule(_boardDragFrom, _pendingMoveModule);
        }

        ClearPendingMoveState();
        _confirm?.Close();
    }

    void ClearPendingMoveState()
    {
        if (_pendingOutline != null)
        {
            Destroy(_pendingOutline.gameObject);
            _pendingOutline = null;
        }

        _pendingMoveTo = null;
        _pendingMoveModule = null;
        _pendingSwapPartner = null;
    }

    void TickPendingTimeout()
    {
        if (_pendingDismantleCell.HasValue)
        {
            _pendingTimeout -= Time.deltaTime;
            if (_pendingTimeout <= 0f)
            {
                CancelPendingDismantle();
            }
        }
        else if (_pendingMoveModule != null)
        {
            _pendingTimeout -= Time.deltaTime;
            if (_pendingTimeout <= 0f)
            {
                CancelPendingMove(restore: true);
            }
        }
        else if (_pendingBoardScrapModule != null)
        {
            _pendingTimeout -= Time.deltaTime;
            if (_pendingTimeout <= 0f)
            {
                CancelPendingBoardScrap(restore: true);
            }
        }
    }

    void TryDismantleGesture()
    {
        if (_pendingMoveModule != null)
        {
            CancelPendingMove(restore: true);
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            CancelPendingDismantle();
            return;
        }

        ModuleBase mod = _board.GetModule(cell);
        if (mod == null)
        {
            CancelPendingDismantle();
            return;
        }

        // 准备阶段：直接拆除回库存，不弹确认
        if (_session == null || _session.IsPreparing)
        {
            CancelPendingDismantle();
            ExecuteDismantleAt(cell);
            return;
        }

        if (_pendingDismantleCell.HasValue && _pendingDismantleCell.Value.Equals(cell))
        {
            ExecutePendingDismantle();
            return;
        }

        BeginDismantleConfirm(cell, mod);
    }

    /// <summary>立刻拆除指定格（准备阶段免费直拆）。</summary>
    void ExecuteDismantleAt(GridCoord cell)
    {
        if (_board == null || _hand == null)
        {
            return;
        }

        ModuleBase existing = _board.GetModule(cell);
        if (existing == null || existing.IsPermanentlyLocked)
        {
            return;
        }

        if (_hand.IsFull)
        {
            Debug.Log("[Placement] 库存已满，无法拆除。");
            return;
        }

        ModuleCardData card = existing.CardData;
        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        int cost = ModulePricing.GetDismantleCost(card, wave, inCombat);
        if (cost > 0)
        {
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost))
            {
                return;
            }
        }

        if (!_board.TryRemoveModule(cell, out ModuleCardData removed))
        {
            if (cost > 0)
            {
                Economy.Instance?.AddGold(cost, silent: true);
            }

            return;
        }

        if (!_hand.TryAddCard(removed))
        {
            if (cost > 0)
            {
                Economy.Instance?.AddGold(cost, silent: true);
            }

            Debug.LogWarning("[Placement] 拆除后无法回手牌。");
        }
    }

    void UpdateScrapHighlightFromSelection()
    {
        if (_scrapZone == null || _hand == null)
        {
            return;
        }

        if (_boardDragging || _pendingBoardScrapModule != null)
        {
            return;
        }

        if (_hand.HasSelection)
        {
            _scrapZone.ShowHandPreview(_hand.SelectedCard);
        }
        else if (_pendingScrapHandIndex < 0)
        {
            _scrapZone.SetIdle();
        }
    }

    void TryClickScrapZone()
    {
        if (_scrapZone == null || _hand == null || !_hand.HasSelection)
        {
            return;
        }

        Vector3 world = GetMouseWorld();
        if (!_scrapZone.ContainsWorldPoint(world))
        {
            return;
        }

        BeginScrapConfirm(_hand.SelectedIndex, _hand.SelectedCard);
    }

    void BeginScrapConfirm(int handIndex, ModuleCardData card)
    {
        CancelPendingBoardScrap(restore: true);
        _pendingScrapHandIndex = handIndex;
        _pendingScrapCard = card;
        int refund = card.ScrapRefund;
        string name = ModuleCatalog.GetDisplayName(card);
        _confirm?.Show(
            GameLocalization.Text($"Permanently scrap \"{name}\"?", $"确定永久分解「{name}」？"),
            GameLocalization.Text(
                $"Refund {refund} gold\nThis cannot be undone",
                $"将返还 {refund} 金币\n此操作无法撤销"),
            GameLocalization.Text("Confirm Scrap", "确认分解"),
            true,
            string.Empty,
            ExecutePendingScrap,
            CancelPendingScrap);
    }

    void BeginBoardScrapConfirm(ModuleBase mod)
    {
        if (mod == null || mod.IsPermanentlyLocked)
        {
            if (mod != null && mod.IsPermanentlyLocked && _board != null)
            {
                _board.TryPlaceModule(_boardDragFrom, mod);
            }

            return;
        }

        CancelPendingScrap();
        CancelPendingDismantle();
        CancelPendingMove(restore: true);

        _pendingBoardScrapModule = mod;
        _pendingBoardScrapFrom = _boardDragFrom;
        _pendingTimeout = PendingTimeoutSeconds;

        if (_scrapZone != null)
        {
            mod.transform.position = _scrapZone.transform.position;
            _scrapZone.ShowHandPreview(mod.CardData);
        }

        ShowPendingOutline(mod);

        ModuleCardData card = mod.CardData;
        int refund = card.ScrapRefund;
        string name = ModuleCatalog.GetDisplayName(card);
        _confirm?.Show(
            GameLocalization.Text($"Permanently scrap \"{name}\"?", $"确定永久分解「{name}」？"),
            GameLocalization.Text(
                $"Remove from the board and refund {refund} gold\nThis cannot be undone",
                $"将从战场移除并返还 {refund} 金币\n此操作无法撤销"),
            GameLocalization.Text("Confirm Scrap", "确认分解"),
            true,
            string.Empty,
            ExecutePendingBoardScrap,
            () => CancelPendingBoardScrap(restore: true));
    }

    void ExecutePendingScrap()
    {
        if (_pendingScrapHandIndex < 0 || _hand == null)
        {
            CancelPendingScrap();
            return;
        }

        if (!_hand.TryConsumeSlot(_pendingScrapHandIndex, out ModuleCardData card))
        {
            CancelPendingScrap();
            return;
        }

        Vector3 from = _scrapZone != null ? _scrapZone.transform.position : Vector3.zero;
        _scrapZone?.TryScrap(card, from);
        CancelPendingScrap();
        _confirm?.Close();
    }

    void ExecutePendingBoardScrap()
    {
        ModuleBase mod = _pendingBoardScrapModule;
        if (mod == null)
        {
            CancelPendingBoardScrap(restore: false);
            return;
        }

        ModuleCardData card = mod.CardData;
        Vector3 from = mod.transform.position;
        ClearPendingBoardScrapState();
        Destroy(mod.gameObject);
        _scrapZone?.TryScrap(card, from);
        _confirm?.Close();
    }

    void CancelPendingScrap()
    {
        _pendingScrapHandIndex = -1;
        _pendingScrapCard = default;
        if (_hand == null || !_hand.HasSelection)
        {
            if (_pendingBoardScrapModule == null)
            {
                _scrapZone?.SetIdle();
            }
        }

        _confirm?.Close();
    }

    void CancelPendingBoardScrap(bool restore)
    {
        if (restore && _pendingBoardScrapModule != null && _board != null)
        {
            _board.TryPlaceModule(_pendingBoardScrapFrom, _pendingBoardScrapModule);
        }

        ClearPendingBoardScrapState();
        if (_hand == null || !_hand.HasSelection)
        {
            _scrapZone?.SetIdle();
        }

        _confirm?.Close();
    }

    void ClearPendingBoardScrapState()
    {
        if (_pendingOutline != null)
        {
            Destroy(_pendingOutline.gameObject);
            _pendingOutline = null;
        }

        _pendingBoardScrapModule = null;
    }

    void UpdateModuleHoverTip()
    {
        if (_boardDragging ||
            _pendingMoveModule != null ||
            _pendingBoardScrapModule != null ||
            (_confirm != null && _confirm.IsOpen))
        {
            ModuleTooltipView.EndHover(this);
            return;
        }

        // UI（手牌/商店）自行报悬停；此处只处理棋盘
        if (IsPointerOverUi())
        {
            ModuleTooltipView.EndHover(this);
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            ModuleTooltipView.EndHover(this);
            return;
        }

        ModuleBase mod = _board.GetModule(cell);
        CellEnchant enchant = _board.GetEnchant(cell);
        if (mod == null && enchant == CellEnchant.None)
        {
            ModuleTooltipView.EndHover(this);
            return;
        }

        ModuleTooltipView.BeginBoardHover(this, mod, enchant);
    }

    void HideModuleTooltip()
    {
        ModuleTooltipView.EndHover(this);
    }

    void BeginDismantleConfirm(GridCoord cell, ModuleBase mod)
    {
        if (mod != null && mod.IsPermanentlyLocked)
        {
            return;
        }

        CancelPendingDismantle();
        _pendingDismantleCell = cell;
        _pendingDismantleModule = mod;
        _pendingTimeout = PendingTimeoutSeconds;
        ShowPendingOutline(mod);

        ModuleCardData card = mod.CardData;
        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        int cost = ModulePricing.GetDismantleCost(card, wave, inCombat);
        bool handFull = _hand != null && _hand.IsFull;
        bool canAfford = cost <= 0 || Economy.Instance == null || Economy.Instance.CanAfford(cost);
        bool canConfirm = !handFull && canAfford;

        string name = ModuleCatalog.GetDisplayName(card);
        string feeLine = inCombat
            ? GameLocalization.Text($"Combat dismantle cost: {cost} gold", $"战斗中拆除费用：{cost} 金币")
            : GameLocalization.Text("Preparation dismantle: free", "准备阶段拆除：免费");
        string stockLine = handFull
            ? GameLocalization.Text("Storage full; cannot dismantle", "库存已满，无法拆除")
            : GameLocalization.Text("Returns to storage after dismantling", "拆除后返回库存");
        string warn = string.Empty;
        if (handFull)
        {
            warn = GameLocalization.Text("Storage full; cannot dismantle", "库存已满，无法拆除");
        }
        else if (!canAfford)
        {
            warn = GameLocalization.Text("Insufficient Gold", "金币不足");
            Economy.Instance?.NotifyInsufficient();
        }

        string confirmText = cost > 0
            ? GameLocalization.Text($"Confirm Dismantle -{cost}", $"确认拆除 -{cost}")
            : GameLocalization.Text("Confirm Dismantle", "确认拆除");
        _confirm?.Show(
            GameLocalization.Text($"Dismantle \"{name}\"?", $"拆除「{name}」？"),
            $"{feeLine}\n{stockLine}",
            confirmText,
            canConfirm,
            warn,
            ExecutePendingDismantle,
            CancelPendingDismantle);
    }

    void ExecutePendingDismantle()
    {
        // 二次右键/X 确认时也要关掉弹窗
        _confirm?.Close();

        if (!_pendingDismantleCell.HasValue || _board == null || _hand == null)
        {
            CancelPendingDismantle();
            return;
        }

        GridCoord cell = _pendingDismantleCell.Value;
        ModuleBase existing = _board.GetModule(cell);
        if (existing == null)
        {
            CancelPendingDismantle();
            return;
        }

        if (_hand.IsFull)
        {
            CancelPendingDismantle();
            return;
        }

        ModuleCardData card = existing.CardData;
        bool inCombat = _session != null && _session.IsCombatActive;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 1;
        int cost = ModulePricing.GetDismantleCost(card, wave, inCombat);
        if (cost > 0)
        {
            if (Economy.Instance == null || !Economy.Instance.TrySpend(cost))
            {
                CancelPendingDismantle();
                return;
            }
        }

        if (!_board.TryRemoveModule(cell, out ModuleCardData removed))
        {
            if (cost > 0)
            {
                Economy.Instance?.AddGold(cost, silent: true);
            }

            CancelPendingDismantle();
            return;
        }

        if (!_hand.TryAddCard(removed))
        {
            Economy.Instance?.AddGold(cost, silent: true);
            Debug.LogWarning("[Placement] 拆除后无法回手牌。");
        }

        CancelPendingDismantle();
    }

    void CancelPendingDismantle()
    {
        _pendingDismantleCell = null;
        _pendingDismantleModule = null;
        if (_pendingOutline != null)
        {
            Destroy(_pendingOutline.gameObject);
            _pendingOutline = null;
        }

        // 仅在无其他确认时关弹窗，避免误关移动/分解框
        if (_pendingMoveModule == null && _pendingBoardScrapModule == null)
        {
            _confirm?.Close();
        }
    }

    void ShowPendingOutline(ModuleBase mod)
    {
        if (mod == null)
        {
            return;
        }

        var go = new GameObject("DismantleOutline");
        go.transform.SetParent(mod.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * 1.25f;
        _pendingOutline = go.AddComponent<SpriteRenderer>();
        _pendingOutline.sprite = PrototypeSprites.Square;
        _pendingOutline.color = new Color(1f, 0.35f, 0.15f, 0.45f);
        _pendingOutline.sortingOrder = 15;
    }

    void HandleHandDrop(HandController hand, int handIndex, Vector2 screenPos)
    {
        HandSlot source = hand != null ? hand.GetSlot(handIndex) : null;
        if (source == null || !source.IsOccupied)
        {
            return;
        }

        ModuleCardData card = source.CardData;
        Vector3 world = ScreenToWorld(screenPos);

        if (EventSystem.current != null)
        {
            var results = new System.Collections.Generic.List<RaycastResult>();
            var ped = new PointerEventData(EventSystem.current) { position = screenPos };
            EventSystem.current.RaycastAll(ped, results);
            for (int i = 0; i < results.Count; i++)
            {
                HandSlot target = results[i].gameObject.GetComponentInParent<HandSlot>();
                if (target == null || target.Index == handIndex)
                {
                    continue;
                }

                if (target.IsOccupied)
                {
                    if (target.CardData.CanFuseWith(card) && hand.TryConsumeSlot(handIndex, out card))
                    {
                        hand.TryFuseIntoSlot(target.Index, card);
                        _scrapZone?.SetIdle();
                        return;
                    }
                }
                else if (hand.TryConsumeSlot(handIndex, out card))
                {
                    target.SetCard(card);
                    _scrapZone?.SetIdle();
                    return;
                }
            }
        }

        // 拖到分解区 → 确认（不立即销毁）
        if (_scrapZone != null && _scrapZone.ContainsWorldPoint(world))
        {
            BeginScrapConfirm(handIndex, card);
            return;
        }

        if (_board != null && _board.TryWorldToCell(world, out GridCoord cell))
        {
            if (ModuleCatalog.IsItemModule(card.Type))
            {
                if (hand.TryConsumeSlot(handIndex, out card))
                {
                    if (!TryUseConsumableCard(card, cell))
                    {
                        hand.TryAddCard(card);
                    }
                }

                _scrapZone?.SetIdle();
                return;
            }

            ModuleBase occupant = _board.GetModule(cell);
            if (occupant != null &&
                occupant.CardData.CanFuseWith(card))
            {
                if (hand.TryConsumeSlot(handIndex, out card))
                {
                    if (!TryApplyBoardFuse(occupant, cell, card))
                    {
                        hand.TryAddCard(card);
                    }
                }

                _scrapZone?.SetIdle();
                return;
            }

            if (_board.CanPlace(cell)
                && CanPlaceTypeAt(card.Type, cell)
                && hand.TryConsumeSlot(handIndex, out card))
            {
                ModuleBase module = CreateModule(card);
                if (module != null)
                {
                    ApplyPreviewOrientation(module);
                    module.transform.SetParent(_moduleRoot, true);
                    if (_board.TryPlaceModule(cell, module))
                    {
                        _scrapZone?.SetIdle();
                        return;
                    }

                    Destroy(module.gameObject);
                    hand.TryAddCard(card);
                }
            }
        }

        _scrapZone?.SetIdle();
    }

    void HandleRotationInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
        {
            return;
        }

        // 棋盘拿起的可旋转模块：直接转实体
        if (_boardDragging && _boardDragModule != null && _boardDragModule.CanRotate)
        {
            _boardDragModule.RotateClockwise();
            _previewOrientation = _boardDragModule.OrientationIndex;
            return;
        }

        // 手牌选中 / 商店拖拽：转预览朝向
        bool previewRotate = _shopDragging;
        if (!previewRotate && _hand != null && _hand.HasSelection)
        {
            previewRotate = ModuleCatalog.IsRotatable(_hand.SelectedModuleType);
        }

        if (!previewRotate)
        {
            return;
        }

        _previewOrientation = (_previewOrientation + 1) % 4;
        if (_ghostRedirector != null && _ghostType == ModuleType.Redirector)
        {
            _ghostRedirector.SetOrientation(_previewOrientation);
        }
    }

    void UpdateShopDrag(Vector2 screenPos)
    {
        if (!_shopDragging)
        {
            return;
        }

        EnsureGhost(_shopDragCard.Type);
        Vector3 mouseWorld = ScreenToWorld(screenPos);
        if (_board != null && _board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            ModuleBase occupant = _board.GetModule(cell);
            bool canFuse = occupant != null &&
                           occupant.CardData.CanFuseWith(_shopDragCard);
            bool canUseItem = ModuleCatalog.IsItemModule(_shopDragCard.Type)
                && CanUseConsumableAt(_shopDragCard.Type, cell);
            bool canPlace = canUseItem
                || (_board.CanPlace(cell) && CanPlaceTypeAt(_shopDragCard.Type, cell))
                || canFuse;
            bool canAfford = Economy.Instance == null || Economy.Instance.CanAfford(_shopDragPrice);
            ShowGhostAt(_board.CellToWorld(cell), canPlace && canAfford);
            UpdateCellHover(cell, canPlace && canAfford);
        }
        else
        {
            HideGhost();
            ClearCellHover();
        }
    }

    void FinishShopDrag(Vector2 screenPos)
    {
        if (!_shopDragging)
        {
            return;
        }

        ShopController shop = _shopDragShop;
        int index = _shopDragIndex;
        ModuleCardData card = _shopDragCard;
        int orient = _previewOrientation;
        CancelShopDrag();

        if (shop == null || _board == null)
        {
            return;
        }

        Vector3 world = ScreenToWorld(screenPos);
        if (!_board.TryWorldToCell(world, out GridCoord cell))
        {
            return;
        }

        ModuleBase occupant = _board.GetModule(cell);
        bool canFuse = occupant != null &&
                       occupant.CardData.CanFuseWith(card);
        bool canUseItem = ModuleCatalog.IsItemModule(card.Type) && CanUseConsumableAt(card.Type, cell);
        if (!canFuse && !canUseItem && (!_board.CanPlace(cell) || !CanPlaceTypeAt(card.Type, cell)))
        {
            return;
        }

        if (!shop.TryPurchaseForBoard(index, out ModuleCardData purchased, out int pricePaid))
        {
            return;
        }

        if (canUseItem)
        {
            if (!TryUseConsumableCard(purchased, cell))
            {
                Economy.Instance?.AddGold(pricePaid, silent: true);
                shop.RestoreOffer(index, purchased, pricePaid);
            }

            return;
        }

        if (canFuse && occupant != null)
        {
            if (!TryApplyBoardFuse(occupant, cell, purchased))
            {
                Economy.Instance?.AddGold(pricePaid, silent: true);
                shop.RestoreOffer(index, purchased, pricePaid);
            }

            return;
        }

        ModuleBase module = CreateModule(purchased);
        if (module == null)
        {
            Economy.Instance?.AddGold(pricePaid, silent: true);
            shop.RestoreOffer(index, purchased, pricePaid);
            return;
        }

        if (module.CanRotate)
        {
            module.SetOrientationIndex(orient);
        }

        module.transform.SetParent(_moduleRoot, true);
        if (!_board.TryPlaceModule(cell, module))
        {
            Destroy(module.gameObject);
            Economy.Instance?.AddGold(pricePaid, silent: true);
            shop.RestoreOffer(index, purchased, pricePaid);
        }
    }

    void CancelShopDrag()
    {
        _shopDragging = false;
        _shopDragShop = null;
        _shopDragIndex = -1;
        _shopDragCard = default;
        _shopDragPrice = 0;
        ClearGhost();
        ClearCellHover();
    }

    void UpdateGhost()
    {
        if (!_hand.HasSelection)
        {
            ClearGhost();
            ClearCellHover();
            return;
        }

        ModuleType type = _hand.SelectedModuleType;
        EnsureGhost(type);

        Vector3 mouseWorld = GetMouseWorld();
        if (_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            ModuleBase occupant = _board.GetModule(cell);
            bool canFuse = occupant != null &&
                           occupant.CardData.CanFuseWith(_hand.SelectedCard);
            bool canUseItem = ModuleCatalog.IsItemModule(type) && CanUseConsumableAt(type, cell);
            bool canPlace = canUseItem
                || (_board.CanPlace(cell) && CanPlaceTypeAt(type, cell))
                || canFuse;
            ShowGhostAt(_board.CellToWorld(cell), canPlace);
            UpdateCellHover(cell, canPlace);
        }
        else
        {
            HideGhost();
            ClearCellHover();
        }
    }

    void ShowGhostAt(Vector3 pos, bool valid)
    {
        Color tint = valid
            ? new Color(0.35f, 0.95f, 0.55f, 0.55f)
            : new Color(0.95f, 0.3f, 0.3f, 0.55f);

        if (_ghostType == ModuleType.Redirector && _ghostRedirector != null)
        {
            _ghostRedirector.gameObject.SetActive(true);
            _ghostRedirector.transform.position = pos;
            _ghostRedirector.SetOrientation(_previewOrientation);
            SetGhostTint(_ghostRedirector.gameObject, tint);
        }
        else if (_ghostType == ModuleType.Projectile && _ghostProjectile != null)
        {
            _ghostProjectile.gameObject.SetActive(true);
            _ghostProjectile.transform.position = pos;
            SetGhostTint(_ghostProjectile.gameObject, tint);
        }
        else if (_ghostOther != null)
        {
            _ghostOther.gameObject.SetActive(true);
            _ghostOther.transform.position = pos;
            if (_shopDragging)
            {
                _ghostOther.ApplyCardData(_shopDragCard);
            }
            else if (_hand != null && _hand.HasSelection)
            {
                _ghostOther.ApplyCardData(_hand.SelectedCard);
            }

            if (_ghostOther.CanRotate)
            {
                _ghostOther.SetOrientationIndex(_previewOrientation);
            }

            SetGhostTint(_ghostOther.gameObject, tint);
        }
    }

    void HideGhost()
    {
        if (_ghostRedirector != null)
        {
            _ghostRedirector.gameObject.SetActive(false);
        }

        if (_ghostProjectile != null)
        {
            _ghostProjectile.gameObject.SetActive(false);
        }

        if (_ghostOther != null)
        {
            _ghostOther.gameObject.SetActive(false);
        }
    }

    void UpdateCellHover(GridCoord cell, bool valid)
    {
        if (_hoveredCell.HasValue && _hoveredCell.Value.Equals(cell) && _hoveredCellView != null)
        {
            if (valid)
            {
                _hoveredCellView.SetValid();
            }
            else
            {
                _hoveredCellView.SetInvalid();
            }

            return;
        }

        ClearCellHover();
        Transform cells = _board.transform.Find("Cells");
        if (cells == null)
        {
            return;
        }

        Transform cellTf = cells.Find($"Cell_{cell.Col}_{cell.Row}");
        if (cellTf == null)
        {
            return;
        }

        _hoveredCellView = cellTf.GetComponent<GridCellView>();
        _hoveredCell = cell;
        if (_hoveredCellView == null)
        {
            return;
        }

        if (valid)
        {
            _hoveredCellView.SetValid();
        }
        else
        {
            _hoveredCellView.SetInvalid();
        }
    }

    void ClearCellHover()
    {
        if (_hoveredCellView != null)
        {
            _hoveredCellView.SetNormal();
            _hoveredCellView = null;
        }

        _hoveredCell = null;
    }

    void TryPlaceAtMouse()
    {
        if (!_hand.HasSelection)
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            return;
        }

        ModuleCardData card = _hand.SelectedCard;
        if (ModuleCatalog.IsItemModule(card.Type))
        {
            if (TryUseConsumableCard(card, cell))
            {
                _hand.ConsumeSelected();
                ClearGhost();
            }

            return;
        }

        ModuleBase occupant = _board.GetModule(cell);
        if (occupant != null &&
            occupant.CardData.CanFuseWith(card))
        {
            if (TryApplyBoardFuse(occupant, cell, card))
            {
                _hand.ConsumeSelected();
                ClearGhost();
            }

            return;
        }

        if (!_board.CanPlace(cell) || !CanPlaceTypeAt(card.Type, cell))
        {
            return;
        }

        ModuleBase module = CreateModule(card);
        if (module == null)
        {
            return;
        }

        ApplyPreviewOrientation(module);

        module.transform.SetParent(_moduleRoot, true);
        if (!_board.TryPlaceModule(cell, module))
        {
            Destroy(module.gameObject);
            return;
        }

        _hand.ConsumeSelected();
        ClearGhost();
    }

    Vector3 GetMouseWorld() => ScreenToWorld(Input.mousePosition);

    Vector3 ScreenToWorld(Vector2 screen)
    {
        Camera cam = _gameplayCamera != null ? _gameplayCamera : Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 s = new Vector3(screen.x, screen.y, -cam.transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(s);
        world.z = 0f;
        return world;
    }

    static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    ModuleBase CreateModule(ModuleCardData card)
    {
        ModuleBase module = CreateModule(card.Type);
        if (module != null)
        {
            module.ApplyCardData(card);
            ModuleSkinApplicator.Apply(module);
        }

        return module;
    }

    ModuleBase CreateModule(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
                return new GameObject("Redirector").AddComponent<RedirectorModule>();
            case ModuleType.Projectile:
                return new GameObject("ProjectileTurret").AddComponent<ProjectileModule>();
            case ModuleType.Bomb:
                return new GameObject("BombTurret").AddComponent<BombModule>();
            case ModuleType.IceLaser:
                return new GameObject("IceLaserTurret").AddComponent<IceLaserModule>();
            case ModuleType.Miner:
                return new GameObject("Miner").AddComponent<MinerModule>();
            case ModuleType.BlackHole:
                return new GameObject("BlackHoleTurret").AddComponent<BlackHoleModule>();
            case ModuleType.FlameAmp:
                return new GameObject("FlameAmp").AddComponent<FlameAmpModule>();
            case ModuleType.IceAmp:
                return new GameObject("IceAmp").AddComponent<IceAmpModule>();
            case ModuleType.Spark:
                return new GameObject("SparkTurret").AddComponent<SparkModule>();
            case ModuleType.Splitter:
                return new GameObject("Splitter").AddComponent<SplitterModule>();
            case ModuleType.Portal:
                return new GameObject("Portal").AddComponent<PortalModule>();
            case ModuleType.Relay:
                return new GameObject("Relay").AddComponent<RelayModule>();
            case ModuleType.Accelerator:
                return new GameObject("Accelerator").AddComponent<AcceleratorModule>();
            case ModuleType.Fusion:
                return new GameObject("Fusion").AddComponent<FusionModule>();
            case ModuleType.Fission:
                return new GameObject("Fission").AddComponent<FissionModule>();
            case ModuleType.FireEnchant:
                return new GameObject("FireEnchant").AddComponent<FireEnchantModule>();
            case ModuleType.Surprise:
                return new GameObject("Surprise").AddComponent<SurpriseModule>();
            case ModuleType.Heatwave:
                return new GameObject("Heatwave").AddComponent<HeatwaveModule>();
            case ModuleType.FlameWall:
                return new GameObject("FlameWall").AddComponent<FlameWallModule>();
            case ModuleType.FlameBlessing:
                return new GameObject("FlameBlessing").AddComponent<FlameBlessingItemModule>();
            case ModuleType.Purify:
                return new GameObject("Purify").AddComponent<PurifyItemModule>();
            case ModuleType.FrostMushroom:
                return new GameObject("FrostMushroom").AddComponent<FrostMushroomItemModule>();
            case ModuleType.FrostBomb:
                return new GameObject("FrostBomb").AddComponent<FrostBombModule>();
            case ModuleType.FrostCannon:
                return new GameObject("FrostCannon").AddComponent<FrostCannonModule>();
            case ModuleType.LaserCannon:
                return new GameObject("LaserCannon").AddComponent<LaserCannonModule>();
            case ModuleType.FrostFreeze:
                return new GameObject("FrostFreeze").AddComponent<FrostFreezeModule>();
            case ModuleType.ArcaneMissile:
                return new GameObject("ArcaneMissile").AddComponent<ArcaneMissileModule>();
            default:
                return null;
        }
    }

    void ApplyPreviewOrientation(ModuleBase module)
    {
        if (module == null || !module.CanRotate)
        {
            return;
        }

        if (module is RedirectorModule redirector)
        {
            redirector.SetOrientation(_previewOrientation);
        }
        else
        {
            module.SetOrientationIndex(_previewOrientation);
        }
    }

    bool CanPlaceTypeAt(ModuleType type, GridCoord cell)
    {
        if (type == ModuleType.Portal && PortalModule.CountOnBoard() >= 2)
        {
            return false;
        }

        return true;
    }

    bool CanUseConsumableAt(ModuleType type, GridCoord cell)
    {
        if (_board == null)
        {
            return false;
        }

        ConsumableModule item = CreateModule(type) as ConsumableModule;
        if (item == null)
        {
            return false;
        }

        bool canUse = item.CanConsumeAt(_board, cell, _session);
        Destroy(item.gameObject);
        return canUse;
    }

    bool TryUseConsumableCard(ModuleCardData card, GridCoord cell)
    {
        if (_board == null)
        {
            return false;
        }

        ConsumableModule item = CreateModule(card.Type) as ConsumableModule;
        if (item == null)
        {
            return false;
        }

        item.ApplyCardData(card);
        bool used = item.ConsumeAt(_board, cell, _session);
        Destroy(item.gameObject);
        return used;
    }

    /// <summary>
    /// 棋盘合成：同型升级，或收束器×功能模块变拐弯（类型变化时替换实例）。
    /// </summary>
    bool TryApplyBoardFuse(ModuleBase occupant, GridCoord cell, ModuleCardData incoming)
    {
        if (occupant == null || _board == null)
        {
            return false;
        }

        ModuleCardData fused = occupant.CardData.FusedWith(incoming);
        if (fused.Type == occupant.ModuleType)
        {
            occupant.ApplyCardData(fused);
            return true;
        }

        bool wasLocked = occupant.IsPermanentlyLocked;
        int orient = occupant.OrientationIndex;
        if (!_board.TryExtractModule(cell, out ModuleBase extracted) || extracted != occupant)
        {
            return false;
        }

        Destroy(extracted.gameObject);
        ModuleBase neu = CreateModule(fused);
        if (neu == null)
        {
            return false;
        }

        if (neu.CanRotate)
        {
            neu.SetOrientationIndex(orient);
        }

        if (wasLocked)
        {
            neu.SetPermanentlyLocked(true);
        }

        neu.transform.SetParent(_moduleRoot, true);
        if (!_board.TryPlaceModule(cell, neu))
        {
            Destroy(neu.gameObject);
            return false;
        }

        return true;
    }

    /// <summary>祝福等：将棋盘上的模块替换为另一类型，保留等级/金币/锁定/朝向。</summary>
    public bool TryMorphPlacedModule(ModuleBase occupant, ModuleType newType)
    {
        if (occupant == null || _board == null || ModuleCatalog.IsItemModule(newType))
        {
            return false;
        }

        if (occupant.ModuleType == newType)
        {
            return false;
        }

        GridCoord cell = occupant.Cell;
        ModuleCardData source = occupant.CardData;
        ModuleCardData morph = ModuleCardData.Create(
            newType,
            source.Level,
            source.InvestedGold,
            source.Bent,
            source.InstanceSeed);

        bool wasLocked = occupant.IsPermanentlyLocked;
        int orient = occupant.OrientationIndex;
        if (!_board.TryExtractModule(cell, out ModuleBase extracted) || extracted != occupant)
        {
            return false;
        }

        Destroy(extracted.gameObject);
        ModuleBase neu = CreateModule(morph);
        if (neu == null)
        {
            return false;
        }

        if (neu.CanRotate)
        {
            neu.SetOrientationIndex(orient);
        }

        if (wasLocked)
        {
            neu.SetPermanentlyLocked(true);
        }

        neu.transform.SetParent(_moduleRoot, true);
        if (!_board.TryPlaceModule(cell, neu))
        {
            Destroy(neu.gameObject);
            return false;
        }

        return true;
    }

    void EnsureGhost(ModuleType type)
    {
        if (_ghostType == type)
        {
            return;
        }

        ClearGhost();
        _ghostType = type;
        if (type == ModuleType.Redirector)
        {
            var go = new GameObject("GhostRedirector");
            go.transform.SetParent(transform, false);
            _ghostRedirector = go.AddComponent<RedirectorModule>();
            go.SetActive(false);
        }
        else if (type == ModuleType.Projectile)
        {
            var go = new GameObject("GhostProjectile");
            go.transform.SetParent(transform, false);
            _ghostProjectile = go.AddComponent<ProjectileModule>();
            _ghostProjectile.enabled = false;
            go.SetActive(false);
        }
        else
        {
            var go = new GameObject("GhostOther");
            go.transform.SetParent(transform, false);
            switch (type)
            {
                case ModuleType.Bomb:
                    _ghostOther = go.AddComponent<BombModule>();
                    break;
                case ModuleType.IceLaser:
                    _ghostOther = go.AddComponent<IceLaserModule>();
                    break;
                case ModuleType.Miner:
                    _ghostOther = go.AddComponent<MinerModule>();
                    break;
                case ModuleType.BlackHole:
                    _ghostOther = go.AddComponent<BlackHoleModule>();
                    break;
                case ModuleType.FlameAmp:
                    _ghostOther = go.AddComponent<FlameAmpModule>();
                    break;
                case ModuleType.IceAmp:
                    _ghostOther = go.AddComponent<IceAmpModule>();
                    break;
                case ModuleType.Spark:
                    _ghostOther = go.AddComponent<SparkModule>();
                    break;
                case ModuleType.Splitter:
                    _ghostOther = go.AddComponent<SplitterModule>();
                    break;
                case ModuleType.Portal:
                    _ghostOther = go.AddComponent<PortalModule>();
                    break;
                case ModuleType.Relay:
                    _ghostOther = go.AddComponent<RelayModule>();
                    break;
                case ModuleType.Accelerator:
                    _ghostOther = go.AddComponent<AcceleratorModule>();
                    break;
                case ModuleType.Fusion:
                    _ghostOther = go.AddComponent<FusionModule>();
                    break;
                case ModuleType.Fission:
                    _ghostOther = go.AddComponent<FissionModule>();
                    break;
                case ModuleType.FireEnchant:
                    _ghostOther = go.AddComponent<FireEnchantModule>();
                    break;
                case ModuleType.Surprise:
                    _ghostOther = go.AddComponent<SurpriseModule>();
                    break;
                case ModuleType.Heatwave:
                    _ghostOther = go.AddComponent<HeatwaveModule>();
                    break;
                case ModuleType.FlameWall:
                    _ghostOther = go.AddComponent<FlameWallModule>();
                    break;
                case ModuleType.FlameBlessing:
                    _ghostOther = go.AddComponent<FlameBlessingItemModule>();
                    break;
                case ModuleType.Purify:
                    _ghostOther = go.AddComponent<PurifyItemModule>();
                    break;
                case ModuleType.FrostMushroom:
                    _ghostOther = go.AddComponent<FrostMushroomItemModule>();
                    break;
                case ModuleType.FrostBomb:
                    _ghostOther = go.AddComponent<FrostBombModule>();
                    break;
                case ModuleType.FrostCannon:
                    _ghostOther = go.AddComponent<FrostCannonModule>();
                    break;
                case ModuleType.LaserCannon:
                    _ghostOther = go.AddComponent<LaserCannonModule>();
                    break;
                case ModuleType.FrostFreeze:
                    _ghostOther = go.AddComponent<FrostFreezeModule>();
                    break;
                case ModuleType.ArcaneMissile:
                    _ghostOther = go.AddComponent<ArcaneMissileModule>();
                    break;
            }

            if (_ghostOther != null)
            {
                _ghostOther.enabled = false;
                ModuleSkinApplicator.Apply(_ghostOther);
            }

            go.SetActive(false);
        }

        if (_ghostRedirector != null)
        {
            ModuleSkinApplicator.Apply(_ghostRedirector);
        }

        if (_ghostProjectile != null)
        {
            ModuleSkinApplicator.Apply(_ghostProjectile);
        }
    }

    void ClearGhost()
    {
        if (_ghostRedirector != null)
        {
            Destroy(_ghostRedirector.gameObject);
            _ghostRedirector = null;
        }

        if (_ghostProjectile != null)
        {
            Destroy(_ghostProjectile.gameObject);
            _ghostProjectile = null;
        }

        if (_ghostOther != null)
        {
            Destroy(_ghostOther.gameObject);
            _ghostOther = null;
        }

        _ghostType = null;
        ClearCellHover();
    }

    static void SetGhostTint(GameObject root, Color tint)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = tint;
        }
    }
}
