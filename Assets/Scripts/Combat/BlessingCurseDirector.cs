using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每 5 波：祝福+束缚组合三选一（一张卡 = 一个祝福 + 一个束缚）。
/// </summary>
public class BlessingCurseDirector : MonoBehaviour
{
    public enum BlessingId
    {
        GoldBurst,
        RareWeapon,
        BombRadius,
        BurnDamageUp,
        EnchantRandomCells,
        FlameBlessingItems,
        FreeRefreshes,
        UpgradeModules
    }

    public enum CurseId
    {
        CurseCells,
        LockModules,
        TimeTax,
        PoolPurge,
        EnemyHaste,
        RefreshCostDouble,
        WeakCells
    }

    struct Combo
    {
        public BlessingId Blessing;
        public CurseId Curse;
        /// <summary>PoolPurge 预选中的移除目标（选卡时展示，选中后按此移除）。</summary>
        public ModuleType[] PurgeTargets;
    }

    DraftChoiceView _draftUi;
    GridBoard _board;
    HandController _hand;
    BoardExpandService _expand;
    RunModulePool _pool;
    PlacementController _placement;
    Action _onFinished;
    readonly List<Combo> _choices = new List<Combo>(3);

    public void Initialize(
        DraftChoiceView draftUi,
        GridBoard board,
        HandController hand,
        BoardExpandService expand,
        RunModulePool pool)
    {
        _draftUi = draftUi;
        _board = board;
        _hand = hand;
        _expand = expand;
        _pool = pool;
    }

    public void BindPlacement(PlacementController placement)
    {
        _placement = placement;
    }

    public bool ShouldOfferAfterWave(int waveDisplay)
    {
        return waveDisplay > 0 && waveDisplay % 5 == 0 && waveDisplay < WaveSpawnBudget.WaveCount;
    }

    public void BeginDraft(int waveDisplay, Action onFinished)
    {
        _onFinished = onFinished;
        if (_draftUi == null)
        {
            Finish();
            return;
        }

        if (RunModifiers.Instance != null)
        {
            RunModifiers.Instance.NotifyBlessingOffered();
        }

        BuildChoices();
        if (_choices.Count == 0)
        {
            Finish();
            return;
        }

        int tier = RunModifiers.Instance != null ? RunModifiers.Instance.CurrentTier : 1;
        var labels = new List<string>(_choices.Count);
        for (int i = 0; i < _choices.Count; i++)
        {
            labels.Add(
                GameLocalization.Text(
                    $"Blessing: {FormatBlessing(_choices[i].Blessing, tier)}\nBinding: {FormatCurse(_choices[i], tier)}",
                    $"祝福：{FormatBlessing(_choices[i].Blessing, tier)}\n束缚：{FormatCurse(_choices[i], tier)}"));
        }

        _draftUi.ShowCustom(
            GameLocalization.Text(
                $"Blessing & Binding (after wave {waveDisplay} · tier {tier})",
                $"祝福与束缚（第{waveDisplay}波后 · 第{tier}次）"),
            labels,
            OnPickedIndex,
            Finish);
    }

    void BuildChoices()
    {
        _choices.Clear();
        var blessings = new List<BlessingId>((BlessingId[])Enum.GetValues(typeof(BlessingId)));
        var curses = new List<CurseId>((CurseId[])Enum.GetValues(typeof(CurseId)));
        Shuffle(blessings);
        Shuffle(curses);

        int take = Mathf.Min(3, blessings.Count);
        for (int i = 0; i < take; i++)
        {
            CurseId curse = curses[i % curses.Count];
            var combo = new Combo
            {
                Blessing = blessings[i],
                Curse = curse,
                PurgeTargets = null
            };
            if (curse == CurseId.PoolPurge)
            {
                combo.PurgeTargets = RollPoolPurgeTargets();
            }

            _choices.Add(combo);
        }
    }

    ModuleType[] RollPoolPurgeTargets()
    {
        if (_pool == null)
        {
            return Array.Empty<ModuleType>();
        }

        int wave = WaveManager.FindDisplayWave();
        int stage = ModulePricing.GetStage(wave);
        ModuleRarity target = ModuleRarity.Common;
        if (stage >= 3)
        {
            target = ModuleRarity.Epic;
        }
        else if (stage >= 1)
        {
            target = ModuleRarity.Rare;
        }

        var picks = new List<ModuleType>(_pool.PickPurgeByRarity(target, 2));
        if (picks.Count < 2 && target != ModuleRarity.Common)
        {
            List<ModuleType> fill = _pool.PickPurgeByRarity(ModuleRarity.Common, 2 - picks.Count);
            for (int i = 0; i < fill.Count; i++)
            {
                if (!picks.Contains(fill[i]))
                {
                    picks.Add(fill[i]);
                }
            }
        }

        return picks.ToArray();
    }

    void OnPickedIndex(int index)
    {
        if (index >= 0 && index < _choices.Count)
        {
            int tier = RunModifiers.Instance != null ? RunModifiers.Instance.CurrentTier : 1;
            Combo combo = _choices[index];
            ApplyBlessing(combo.Blessing, tier);
            ApplyCurse(combo, tier);
        }

        Finish();
    }

    void ApplyBlessing(BlessingId id, int tier)
    {
        switch (id)
        {
            case BlessingId.GoldBurst:
                Economy.Instance?.AddGold(GoldBurstAmount(tier));
                break;
            case BlessingId.RareWeapon:
                GrantRareWeapon();
                break;
            case BlessingId.BombRadius:
                RunModifiers.Instance?.ApplyBombRadiusBoost();
                break;
            case BlessingId.BurnDamageUp:
                RunModifiers.Instance?.AddBurnDamageBonus(5);
                break;
            case BlessingId.EnchantRandomCells:
                _board?.EnchantRandomBuildableCells(Mathf.Clamp(tier + 1, 2, 5));
                RunModifiers.Instance?.NotifyUiChanged();
                break;
            case BlessingId.FlameBlessingItems:
                GrantItemCards(ModuleType.FlameBlessing, FlameBlessingItemCount(tier));
                break;
            case BlessingId.FreeRefreshes:
                RunModifiers.Instance?.AddFreeRefreshes(FreeRefreshCount(tier));
                RunModifiers.Instance?.NotifyUiChanged();
                break;
            case BlessingId.UpgradeModules:
                ApplyUpgradeModules(tier);
                RunModifiers.Instance?.NotifyUiChanged();
                break;
        }
    }

    void ApplyCurse(Combo combo, int tier)
    {
        switch (combo.Curse)
        {
            case CurseId.CurseCells:
                ApplyCurseCells(tier);
                break;
            case CurseId.LockModules:
                ApplyLockModules(tier);
                break;
            case CurseId.TimeTax:
                SandClock.Instance?.RemoveSand(TimeTaxMs(tier), 1000);
                break;
            case CurseId.PoolPurge:
                ApplyPoolPurge(combo.PurgeTargets);
                break;
            case CurseId.EnemyHaste:
                RunModifiers.Instance?.ApplyEnemyHaste();
                break;
            case CurseId.RefreshCostDouble:
                RunModifiers.Instance?.ApplyRefreshCostDouble();
                break;
            case CurseId.WeakCells:
                _board?.WeakenRandomBuildableCells(WeakCellCount(tier));
                RunModifiers.Instance?.NotifyUiChanged();
                break;
        }
    }

    void GrantRareWeapon()
    {
        int wave = WaveManager.FindDisplayWave();
        ModuleRarity target = HighestWeaponRarity(ModulePricing.GetStage(wave));
        ModuleType type = RollWeaponOfRarity(target);
        var card = ModuleCardData.Create(type, 1, 0);
        if (_hand != null && _hand.TryAddCard(card))
        {
            return;
        }

        // 手满：分解为废料金
        Economy.Instance?.AddGold(Mathf.Max(5, card.ScrapRefund));
    }

    void GrantItemCards(ModuleType type, int count)
    {
        int give = Mathf.Max(0, count);
        for (int i = 0; i < give; i++)
        {
            var card = ModuleCardData.Create(type, 1, 0);
            if (_hand != null && _hand.TryAddCard(card))
            {
                continue;
            }

            Economy.Instance?.AddGold(Mathf.Max(5, card.ScrapRefund));
        }
    }

    void ApplyUpgradeModules(int tier)
    {
        int count = Mathf.Clamp(tier, 1, 4);
        var targets = new List<OwnedModuleRef>();
        CollectBoardUpgradeTargets(targets);
        CollectHandUpgradeTargets(targets);
        if (targets.Count == 0)
        {
            return;
        }

        Shuffle(targets);
        int upgraded = 0;
        for (int i = 0; i < targets.Count && upgraded < count; i++)
        {
            OwnedModuleRef target = targets[i];
            ModuleRarity next = GetNextRarity(ModuleCatalog.GetRarity(target.Type));
            if (next == ModuleRarity.Legendary)
            {
                continue;
            }

            if (!TryRollOwnedReplacement(next, out ModuleType to) || to == target.Type)
            {
                continue;
            }

            if (TryApplyOwnedUpgrade(target, to))
            {
                upgraded++;
            }
        }
    }

    struct OwnedModuleRef
    {
        public bool IsHand;
        public int HandIndex;
        public ModuleBase BoardModule;
        public ModuleType Type;
        public ModuleCardData Card;
    }

    void CollectBoardUpgradeTargets(List<OwnedModuleRef> into)
    {
        if (_board == null)
        {
            return;
        }

        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                ModuleBase mod = _board.GetModule(new GridCoord(col, row));
                if (mod == null || ModuleCatalog.IsItemModule(mod.ModuleType))
                {
                    continue;
                }

                if (!HasUpgradeCandidate(mod.ModuleType))
                {
                    continue;
                }

                into.Add(new OwnedModuleRef
                {
                    IsHand = false,
                    BoardModule = mod,
                    Type = mod.ModuleType,
                    Card = mod.CardData
                });
            }
        }
    }

    void CollectHandUpgradeTargets(List<OwnedModuleRef> into)
    {
        if (_hand == null)
        {
            return;
        }

        for (int i = 0; i < HandController.SlotCount; i++)
        {
            HandSlot slot = _hand.GetSlot(i);
            if (slot == null || !slot.IsOccupied)
            {
                continue;
            }

            ModuleCardData card = slot.CardData;
            if (ModuleCatalog.IsItemModule(card.Type) || !HasUpgradeCandidate(card.Type))
            {
                continue;
            }

            into.Add(new OwnedModuleRef
            {
                IsHand = true,
                HandIndex = i,
                Type = card.Type,
                Card = card
            });
        }
    }

    bool TryApplyOwnedUpgrade(OwnedModuleRef target, ModuleType to)
    {
        ModuleCardData morph = ModuleCardData.Create(
            to,
            target.Card.Level,
            target.Card.InvestedGold,
            target.Card.Bent,
            target.Card.InstanceSeed);

        if (target.IsHand)
        {
            return _hand != null && _hand.TryReplaceCard(target.HandIndex, morph);
        }

        if (_placement == null || target.BoardModule == null)
        {
            return false;
        }

        return _placement.TryMorphPlacedModule(target.BoardModule, to);
    }

    static ModuleRarity HighestWeaponRarity(int stage)
    {
        // stage 升高：稀有 → 史诗；传奇暂无实体则降级
        if (stage >= 2)
        {
            return ModuleRarity.Epic;
        }

        return ModuleRarity.Rare;
    }

    static ModuleType RollWeaponOfRarity(ModuleRarity rarity)
    {
        var bag = new List<ModuleType>();
        CollectAttackOfRarity(bag, rarity);
        if (bag.Count == 0 && rarity == ModuleRarity.Legendary)
        {
            CollectAttackOfRarity(bag, ModuleRarity.Epic);
        }

        if (bag.Count == 0 && rarity == ModuleRarity.Epic)
        {
            CollectAttackOfRarity(bag, ModuleRarity.Rare);
        }

        if (bag.Count == 0)
        {
            CollectAttackOfRarity(bag, ModuleRarity.Common);
        }

        if (bag.Count == 0)
        {
            return ModuleType.Projectile;
        }

        return bag[UnityEngine.Random.Range(0, bag.Count)];
    }

    static void CollectAttackOfRarity(List<ModuleType> bag, ModuleRarity rarity)
    {
        ModuleType[] all = ModuleCatalog.GetSellableTypes();
        for (int i = 0; i < all.Length; i++)
        {
            if (ModuleCatalog.IsAttackModule(all[i]) && ModuleCatalog.GetRarity(all[i]) == rarity)
            {
                bag.Add(all[i]);
            }
        }
    }

    bool HasUpgradeCandidate(ModuleType type)
    {
        if (ModuleCatalog.IsItemModule(type))
        {
            return false;
        }

        ModuleRarity next = GetNextRarity(ModuleCatalog.GetRarity(type));
        if (next == ModuleRarity.Legendary)
        {
            return false;
        }

        ModuleType[] all = ModuleCatalog.GetSellableTypes();
        for (int i = 0; i < all.Length; i++)
        {
            if (IsValidUpgradeResult(all[i]) && ModuleCatalog.GetRarity(all[i]) == next)
            {
                return true;
            }
        }

        return false;
    }

    bool TryRollOwnedReplacement(ModuleRarity rarity, out ModuleType result)
    {
        var bag = new List<ModuleType>();
        ModuleType[] all = ModuleCatalog.GetSellableTypes();
        for (int i = 0; i < all.Length; i++)
        {
            if (IsValidUpgradeResult(all[i]) && ModuleCatalog.GetRarity(all[i]) == rarity)
            {
                bag.Add(all[i]);
            }
        }

        if (bag.Count == 0)
        {
            result = default;
            return false;
        }

        result = bag[UnityEngine.Random.Range(0, bag.Count)];
        return true;
    }

    static bool IsValidUpgradeResult(ModuleType type) => !ModuleCatalog.IsItemModule(type);

    static ModuleRarity GetNextRarity(ModuleRarity rarity)
    {
        switch (rarity)
        {
            case ModuleRarity.Common: return ModuleRarity.Rare;
            case ModuleRarity.Rare: return ModuleRarity.Epic;
            default: return ModuleRarity.Legendary;
        }
    }

    void ApplyCurseCells(int tier)
    {
        if (_board == null)
        {
            return;
        }

        int count = Mathf.Clamp(tier, 1, 4);
        var unloaded = _board.CurseRandomBuildableCells(count);
        for (int i = 0; i < unloaded.Count; i++)
        {
            ModuleBase mod = unloaded[i];
            if (mod == null)
            {
                continue;
            }

            ModuleCardData card = mod.CardData;
            UnityEngine.Object.Destroy(mod.gameObject);
            if (_hand == null || !_hand.TryAddCard(card))
            {
                Economy.Instance?.AddGold(Mathf.Max(5, card.ScrapRefund));
            }
        }
    }

    void ApplyLockModules(int tier)
    {
        if (_board == null)
        {
            return;
        }

        int count = Mathf.Clamp(tier, 1, 4);
        var placed = new List<ModuleBase>();
        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                ModuleBase m = _board.GetModule(new GridCoord(col, row));
                if (m != null && !m.IsPermanentlyLocked)
                {
                    placed.Add(m);
                }
            }
        }

        Shuffle(placed);
        int take = Mathf.Min(count, placed.Count);
        for (int i = 0; i < take; i++)
        {
            placed[i].SetPermanentlyLocked(true);
        }
    }

    void ApplyPoolPurge(ModuleType[] targets)
    {
        if (_pool == null)
        {
            return;
        }

        if (targets != null && targets.Length > 0)
        {
            _pool.PurgeSpecific(targets);
            return;
        }

        // 兜底：无预选时按旧逻辑随机
        int wave = WaveManager.FindDisplayWave();
        int stage = ModulePricing.GetStage(wave);
        ModuleRarity target = ModuleRarity.Common;
        if (stage >= 3)
        {
            target = ModuleRarity.Epic;
        }
        else if (stage >= 1)
        {
            target = ModuleRarity.Rare;
        }

        int removed = _pool.PurgeByRarity(target, 2);
        if (removed < 2 && target != ModuleRarity.Common)
        {
            _pool.PurgeByRarity(ModuleRarity.Common, 2 - removed);
        }
    }

    static int GoldBurstAmount(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 100;
            case 2: return 200;
            case 3: return 300;
            default: return 400;
        }
    }

    static int TimeTaxMs(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 30_000;
            case 2: return 45_000;
            case 3: return 60_000;
            default: return 90_000;
        }
    }

    static int FlameBlessingItemCount(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1:
            case 2: return 1;
            default: return 2;
        }
    }

    static int FreeRefreshCount(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 5;
            case 2: return 10;
            case 3: return 15;
            default: return 20;
        }
    }

    static int WeakCellCount(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 2;
            case 2: return 3;
            case 3: return 4;
            default: return 5;
        }
    }

    static string FormatBlessing(BlessingId id, int tier)
    {
        switch (id)
        {
            case BlessingId.GoldBurst: return GameLocalization.Text($"+{GoldBurstAmount(tier)} gold", $"+{GoldBurstAmount(tier)} 金");
            case BlessingId.RareWeapon: return GameLocalization.Text("Add a random high-rarity weapon to hand", "随机高稀有武器入手牌");
            case BlessingId.BombRadius: return GameLocalization.Text("Bomb / black-hole radius ×1.2", "炸弹/黑洞范围 ×1.2");
            case BlessingId.BurnDamageUp: return GameLocalization.Text($"Burn damage +5 (every {RunModifiers.BurnTickInterval:0.#}s)", $"灼烧伤害 +5（每{RunModifiers.BurnTickInterval:0.#}秒）");
            case BlessingId.EnchantRandomCells: return GameLocalization.Text($"Enchant {Mathf.Clamp(tier + 1, 2, 5)} random cells", $"随机 {Mathf.Clamp(tier + 1, 2, 5)} 格附魔");
            case BlessingId.FlameBlessingItems: return GameLocalization.Text($"Gain {FlameBlessingItemCount(tier)} Flame Blessing item(s)", $"获得 {FlameBlessingItemCount(tier)} 个火焰祝福道具");
            case BlessingId.FreeRefreshes: return GameLocalization.Text($"Gain {FreeRefreshCount(tier)} free refreshes", $"获得 {FreeRefreshCount(tier)} 次免费刷新");
            case BlessingId.UpgradeModules: return GameLocalization.Text(
                $"Transform {Mathf.Clamp(tier, 1, 4)} board/hand module(s) into a random higher-rarity module",
                $"将战场或手牌中 {Mathf.Clamp(tier, 1, 4)} 个模块变为高一级稀有度的随机模块");
            default: return id.ToString();
        }
    }

    static string FormatCurse(Combo combo, int tier)
    {
        int n = Mathf.Clamp(tier, 1, 4);
        switch (combo.Curse)
        {
            case CurseId.CurseCells: return GameLocalization.Text($"Curse {n} cells (cannot build)", $"诅咒 {n} 格（不可放置）");
            case CurseId.LockModules: return GameLocalization.Text($"Lock {n} placed modules", $"锁定 {n} 个已放置模块");
            case CurseId.TimeTax: return GameLocalization.Text($"-{TimeTaxMs(tier) / 1000}s sand", $"-{TimeTaxMs(tier) / 1000}s 沙");
            case CurseId.PoolPurge: return FormatPoolPurge(combo.PurgeTargets);
            case CurseId.EnemyHaste: return GameLocalization.Text("Enemy speed +8%", "敌人移速 +8%");
            case CurseId.RefreshCostDouble: return GameLocalization.Text("Refresh price ×2", "刷新价格翻倍");
            case CurseId.WeakCells: return GameLocalization.Text($"Apply Weak enchant to {WeakCellCount(tier)} cells", $"使 {WeakCellCount(tier)} 格获得虚弱附魔");
            default: return combo.Curse.ToString();
        }
    }

    static string FormatPoolPurge(ModuleType[] targets)
    {
        if (targets == null || targets.Length == 0)
        {
            return GameLocalization.Text("Remove 2 modules from the shop pool", "商店池移除 2 个模块");
        }

        if (targets.Length == 1)
        {
            string name = ModuleCatalog.GetDisplayName(targets[0]);
            return GameLocalization.Text(
                $"Shop pool removes: {name}",
                $"商店池移除：{name}");
        }

        string a = ModuleCatalog.GetDisplayName(targets[0]);
        string b = ModuleCatalog.GetDisplayName(targets[1]);
        return GameLocalization.Text(
            $"Shop pool removes: {a} & {b}",
            $"商店池移除：{a}、{b}");
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void Finish()
    {
        _draftUi?.Hide();
        Action cb = _onFinished;
        _onFinished = null;
        cb?.Invoke();
    }
}
