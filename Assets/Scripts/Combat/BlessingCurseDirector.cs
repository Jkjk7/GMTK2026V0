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
        TimeGift,
        RareWeapon,
        BombRadius,
        BoardExpandDiscount,
        BurnDamageUp,
        EnchantRandomCells
    }

    public enum CurseId
    {
        CurseCells,
        LockModules,
        TimeTax,
        PoolPurge,
        EnemyHaste
    }

    struct Combo
    {
        public BlessingId Blessing;
        public CurseId Curse;
    }

    DraftChoiceView _draftUi;
    GridBoard _board;
    HandController _hand;
    BoardExpandService _expand;
    RunModulePool _pool;
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

    public bool ShouldOfferAfterWave(int waveDisplay)
    {
        return waveDisplay > 0 && waveDisplay % 5 == 0 && waveDisplay < 25;
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
                    $"Blessing: {FormatBlessing(_choices[i].Blessing, tier)}\nBinding: {FormatCurse(_choices[i].Curse, tier)}",
                    $"祝福：{FormatBlessing(_choices[i].Blessing, tier)}\n束缚：{FormatCurse(_choices[i].Curse, tier)}"));
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
            _choices.Add(new Combo
            {
                Blessing = blessings[i],
                Curse = curses[i % curses.Count]
            });
        }
    }

    void OnPickedIndex(int index)
    {
        if (index >= 0 && index < _choices.Count)
        {
            int tier = RunModifiers.Instance != null ? RunModifiers.Instance.CurrentTier : 1;
            ApplyBlessing(_choices[index].Blessing, tier);
            ApplyCurse(_choices[index].Curse, tier);
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
            case BlessingId.TimeGift:
                SandClock.Instance?.AddSand(TimeGiftMs(tier));
                break;
            case BlessingId.RareWeapon:
                GrantRareWeapon();
                break;
            case BlessingId.BombRadius:
                RunModifiers.Instance?.ApplyBombRadiusBoost();
                break;
            case BlessingId.BoardExpandDiscount:
                if (_expand != null && _expand.GetNextExpandCost() > 0)
                {
                    RunModifiers.Instance?.GrantExpandHalfPrice();
                }
                else
                {
                    Economy.Instance?.AddGold(25);
                }

                break;
            case BlessingId.BurnDamageUp:
                RunModifiers.Instance?.AddBurnDamageBonus(5);
                break;
            case BlessingId.EnchantRandomCells:
                _board?.EnchantRandomBuildableCells(Mathf.Clamp(tier, 1, 4));
                RunModifiers.Instance?.NotifyUiChanged();
                break;
        }
    }

    void ApplyCurse(CurseId id, int tier)
    {
        switch (id)
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
                ApplyPoolPurge();
                break;
            case CurseId.EnemyHaste:
                RunModifiers.Instance?.ApplyEnemyHaste();
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

    void ApplyPoolPurge()
    {
        if (_pool == null)
        {
            return;
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

    static int TimeGiftMs(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 20_000;
            case 2: return 30_000;
            case 3: return 40_000;
            default: return 50_000;
        }
    }

    static int TimeTaxMs(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 4))
        {
            case 1: return 25_000;
            case 2: return 37_500;
            case 3: return 50_000;
            default: return 62_500;
        }
    }

    static string FormatBlessing(BlessingId id, int tier)
    {
        switch (id)
        {
            case BlessingId.GoldBurst: return GameLocalization.Text($"+{GoldBurstAmount(tier)} gold", $"+{GoldBurstAmount(tier)} 金");
            case BlessingId.TimeGift: return GameLocalization.Text($"+{TimeGiftMs(tier) / 1000}s sand", $"+{TimeGiftMs(tier) / 1000}s 沙");
            case BlessingId.RareWeapon: return GameLocalization.Text("Add a random high-rarity weapon to hand", "随机高稀有武器入手牌");
            case BlessingId.BombRadius: return GameLocalization.Text("Bomb / black-hole radius ×1.2", "炸弹/黑洞范围 ×1.2");
            case BlessingId.BoardExpandDiscount: return GameLocalization.Text("Next board expansion is half price", "下次棋盘扩展半价");
            case BlessingId.BurnDamageUp: return GameLocalization.Text($"Burn damage +5 (every {RunModifiers.BurnTickInterval:0.#}s)", $"灼烧伤害 +5（每{RunModifiers.BurnTickInterval:0.#}秒）");
            case BlessingId.EnchantRandomCells: return GameLocalization.Text($"Enchant {Mathf.Clamp(tier, 1, 4)} random cells", $"随机 {Mathf.Clamp(tier, 1, 4)} 格附魔");
            default: return id.ToString();
        }
    }

    static string FormatCurse(CurseId id, int tier)
    {
        int n = Mathf.Clamp(tier, 1, 4);
        switch (id)
        {
            case CurseId.CurseCells: return GameLocalization.Text($"Curse {n} cells (cannot build)", $"诅咒 {n} 格（不可放置）");
            case CurseId.LockModules: return GameLocalization.Text($"Lock {n} placed modules", $"锁定 {n} 个已放置模块");
            case CurseId.TimeTax: return GameLocalization.Text($"-{TimeTaxMs(tier) / 1000}s sand", $"-{TimeTaxMs(tier) / 1000}s 沙");
            case CurseId.PoolPurge: return GameLocalization.Text("Remove 2 modules from the shop pool", "商店池移除 2 个模块");
            case CurseId.EnemyHaste: return GameLocalization.Text("Enemy speed +8%", "敌人移速 +8%");
            default: return id.ToString();
        }
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
