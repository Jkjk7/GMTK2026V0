using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局商店解锁池。开局：激光+收束器+寒冰+火花；按稀有度权重抽货。
/// </summary>
public class RunModulePool : MonoBehaviour
{
    public const int MaxSize = 12;

    public static RunModulePool Instance { get; private set; }

    readonly List<ModuleType> _unlocked = new List<ModuleType>();

    public IReadOnlyList<ModuleType> Unlocked => _unlocked;
    public int Count => _unlocked.Count;
    public bool IsFull => _unlocked.Count >= MaxSize;

    void Awake()
    {
        Instance = this;
        ResetToStarter();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ResetToStarter()
    {
        _unlocked.Clear();
        _unlocked.Add(ModuleType.Projectile);
        _unlocked.Add(ModuleType.Redirector);
        _unlocked.Add(ModuleType.IceLaser);
        _unlocked.Add(ModuleType.Spark);
    }

    public bool Contains(ModuleType type) => _unlocked.Contains(type);

    public bool TryAdd(ModuleType type)
    {
        if (_unlocked.Contains(type) || IsFull)
        {
            return false;
        }

        _unlocked.Add(type);
        return true;
    }

    public bool TryReplace(ModuleType remove, ModuleType add)
    {
        if (!_unlocked.Contains(remove) || _unlocked.Contains(add))
        {
            return false;
        }

        int idx = _unlocked.IndexOf(remove);
        if (idx < 0)
        {
            return false;
        }

        _unlocked[idx] = add;
        return true;
    }

    public bool TryRemove(ModuleType type)
    {
        if (!CanRemove(type))
        {
            return false;
        }

        return _unlocked.Remove(type);
    }

    /// <summary>按稀有度权重从列表抽一个。</summary>
    public static ModuleType RollWeighted(IList<ModuleType> list, int waveNumber)
    {
        if (list == null || list.Count == 0)
        {
            return ModuleType.Redirector;
        }

        int stage = ModulePricing.GetStage(waveNumber);
        float total = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            total += ModuleCatalog.GetShopWeight(list[i], stage);
        }

        float roll = Random.value * Mathf.Max(0.001f, total);
        float acc = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            acc += ModuleCatalog.GetShopWeight(list[i], stage);
            if (roll <= acc)
            {
                return list[i];
            }
        }

        return list[list.Count - 1];
    }

    public ModuleType RollShopSlotType(int slotIndex)
    {
        return RollShopSlotType(slotIndex, WaveManagerDisplayWave());
    }

    public ModuleType RollShopSlotType(int slotIndex, int waveNumber)
    {
        if (_unlocked.Count == 0)
        {
            return ModuleType.Redirector;
        }

        // 六格公平：全部已解锁按稀有度权重抽取（可重复类型）
        return RollWeighted(_unlocked, waveNumber);
    }

    static int WaveManagerDisplayWave()
    {
        return WaveManager.FindDisplayWave();
    }

    public List<ModuleType> PickDistinctReplaceTargets(int count)
    {
        var result = new List<ModuleType>();
        if (_unlocked.Count == 0 || count <= 0)
        {
            return result;
        }

        var bag = new List<ModuleType>(_unlocked);
        for (int i = 0; i < bag.Count; i++)
        {
            int j = Random.Range(i, bag.Count);
            (bag[i], bag[j]) = (bag[j], bag[i]);
        }

        for (int i = 0; i < bag.Count && result.Count < count; i++)
        {
            result.Add(bag[i]);
        }

        while (result.Count < count)
        {
            result.Add(_unlocked[Random.Range(0, _unlocked.Count)]);
        }

        return result;
    }

    /// <summary>按稀有度挑选最多 count 个可移除模块（不实际移除），供诅咒预览。</summary>
    public List<ModuleType> PickPurgeByRarity(ModuleRarity rarity, int count)
    {
        var candidates = new List<ModuleType>();
        for (int i = 0; i < _unlocked.Count; i++)
        {
            ModuleType t = _unlocked[i];
            if (ModuleCatalog.GetRarity(t) != rarity)
            {
                continue;
            }

            // 与 TryRemove 保底一致：不可同时掏空激光与收束器的预览也不选会死锁的
            if (!CanRemove(t))
            {
                continue;
            }

            candidates.Add(t);
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        if (candidates.Count > count)
        {
            candidates.RemoveRange(count, candidates.Count - count);
        }

        return candidates;
    }

    public bool CanRemove(ModuleType type)
    {
        if (!_unlocked.Contains(type))
        {
            return false;
        }

        if (type == ModuleType.Projectile || type == ModuleType.Redirector)
        {
            bool hasOtherCore = false;
            for (int i = 0; i < _unlocked.Count; i++)
            {
                ModuleType t = _unlocked[i];
                if (t == type)
                {
                    continue;
                }

                if (t == ModuleType.Projectile || t == ModuleType.Redirector)
                {
                    hasOtherCore = true;
                    break;
                }
            }

            if (!hasOtherCore)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>按稀有度从池中移除最多 count 个（不含无法移除的核心保底失败项）。</summary>
    public int PurgeByRarity(ModuleRarity rarity, int count)
    {
        List<ModuleType> picks = PickPurgeByRarity(rarity, count);
        int removed = 0;
        for (int i = 0; i < picks.Count; i++)
        {
            if (TryRemove(picks[i]))
            {
                removed++;
            }
        }

        return removed;
    }

    public int PurgeSpecific(IReadOnlyList<ModuleType> types)
    {
        if (types == null || types.Count == 0)
        {
            return 0;
        }

        int removed = 0;
        for (int i = 0; i < types.Count; i++)
        {
            if (TryRemove(types[i]))
            {
                removed++;
            }
        }

        return removed;
    }

    public List<ModuleType> GetUnlockedOfRarity(ModuleRarity rarity)
    {
        var list = new List<ModuleType>();
        for (int i = 0; i < _unlocked.Count; i++)
        {
            if (ModuleCatalog.GetRarity(_unlocked[i]) == rarity)
            {
                list.Add(_unlocked[i]);
            }
        }

        return list;
    }
}
