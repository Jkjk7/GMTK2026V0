using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 火附魔：按卡牌实例种子对若干格写入 Flame；移动不改布局，升级确定性加 1 格。
/// </summary>
public class FireEnchantModule : ModuleBase
{
    readonly List<GridCoord> _owned = new List<GridCoord>(4);
    readonly List<CellEnchant> _kinds = new List<CellEnchant>(4);
    SpriteRenderer _body;
    TextMesh _levelLabel;
    int _appliedLevel;

    public override ModuleType ModuleType => global::ModuleType.FireEnchant;

    public override void BindToCell(GridBoard board, GridCoord cell)
    {
        base.BindToCell(board, cell);
        ReapplyEnchants(CardData.Level);
    }

    public override void ApplyCardData(ModuleCardData data)
    {
        int prev = CardData.Level;
        base.ApplyCardData(data);
        EnsureLevelLabel(data.Level);
        RefreshVisual();
        if (BoundBoard != null && data.Level != _appliedLevel)
        {
            ReapplyEnchants(data.Level);
        }
        else if (BoundBoard != null && data.Level > prev)
        {
            ReapplyEnchants(data.Level);
        }
    }

    void OnDestroy()
    {
        ClearOwnedEnchants();
    }

    public override void OnBallEnter(EnergyBall ball)
    {
    }

    void ReapplyEnchants(int level)
    {
        ClearOwnedEnchants();
        if (BoundBoard == null)
        {
            return;
        }

        int lvl = Mathf.Clamp(level, 1, ModulePricing.MaxAttackLevel);
        List<GridCoord> targets = EnchantSeedUtil.BuildTargets(
            BoundBoard,
            Cell,
            ModuleType,
            CardData.InstanceSeed,
            lvl);
        for (int i = 0; i < targets.Count; i++)
        {
            CellEnchant kind = GetKindForIndex(i);
            BoundBoard.SetEnchant(targets[i], kind);
            _owned.Add(targets[i]);
            _kinds.Add(kind);
        }

        _appliedLevel = lvl;
    }

    protected int InstanceSeed => CardData.InstanceSeed;

    protected virtual CellEnchant GetKindForIndex(int index) => CellEnchant.Flame;

    void ClearOwnedEnchants()
    {
        if (BoundBoard == null)
        {
            _owned.Clear();
            _kinds.Clear();
            return;
        }

        for (int i = 0; i < _owned.Count; i++)
        {
            GridCoord c = _owned[i];
            if (BoundBoard.GetEnchant(c) == _kinds[i])
            {
                BoundBoard.SetEnchant(c, CellEnchant.None);
            }
        }

        _owned.Clear();
        _kinds.Clear();
        _appliedLevel = 0;
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        if (_body != null)
        {
            _body.color = ModuleCatalog.GetDisplayColor(ModuleType);
        }
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = gameObject.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Square;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.55f;
    }

    void EnsureLevelLabel(int level)
    {
        if (level <= 1)
        {
            if (_levelLabel != null)
            {
                _levelLabel.gameObject.SetActive(false);
            }

            return;
        }

        if (_levelLabel == null)
        {
            var go = new GameObject("LevelLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            go.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            _levelLabel = go.AddComponent<TextMesh>();
            _levelLabel.anchor = TextAnchor.MiddleCenter;
            _levelLabel.fontSize = 40;
            _levelLabel.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 12;
            }
        }

        _levelLabel.gameObject.SetActive(true);
        _levelLabel.text = $"Lv{level}";
    }
}

/// <summary>附魔模块确定性选格工具。</summary>
public static class EnchantSeedUtil
{
    static readonly CellEnchant[] RandomKinds =
    {
        CellEnchant.Flame,
        CellEnchant.DamageUp,
        CellEnchant.Frost,
        CellEnchant.Shrink,
        CellEnchant.Cooldown
    };

    public static List<GridCoord> BuildTargets(
        GridBoard board,
        GridCoord origin,
        ModuleType type,
        int instanceSeed,
        int level)
    {
        // 保留 origin 参数兼容现有调用；附魔布局不再受模块放置格影响。
        var result = new List<GridCoord>(4);
        int lvl = Mathf.Clamp(level, 1, 4);
        for (int step = 1; step <= lvl; step++)
        {
            int seed = Hash((int)type, instanceSeed, step);
            var rng = new System.Random(seed);
            List<GridCoord> order = AllCellsShuffled(board, rng);
            // 找到第一个不在结果中的候选；不可用则跳过且不补抽
            for (int i = 0; i < order.Count; i++)
            {
                GridCoord c = order[i];
                if (Contains(result, c))
                {
                    continue;
                }

                if (IsEnchantable(board, c))
                {
                    result.Add(c);
                }

                break;
            }
        }

        return result;
    }

    public static CellEnchant RollKind(
        GridCoord origin,
        ModuleType type,
        int instanceSeed,
        int index)
    {
        // 与选格相同，origin 仅为兼容参数，不参与随机种子。
        int seed = Hash((int)type, instanceSeed, 100 + index);
        var rng = new System.Random(seed);
        return RandomKinds[rng.Next(0, RandomKinds.Length)];
    }

    static bool IsEnchantable(GridBoard board, GridCoord c)
    {
        return board.IsBuildableCell(c) && !board.IsCursed(c);
    }

    static bool Contains(List<GridCoord> list, GridCoord c)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(c))
            {
                return true;
            }
        }

        return false;
    }

    static List<GridCoord> AllCellsShuffled(GridBoard board, System.Random rng)
    {
        var list = new List<GridCoord>(GridBoard.Width * GridBoard.Height);
        for (int col = 0; col < GridBoard.Width; col++)
        {
            for (int row = 0; row < GridBoard.Height; row++)
            {
                list.Add(new GridCoord(col, row));
            }
        }

        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list;
    }

    static int Hash(int a, int b, int c)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + a;
            h = h * 31 + b;
            h = h * 31 + c;
            return h == 0 ? 1 : h;
        }
    }
}
