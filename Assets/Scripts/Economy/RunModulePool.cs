using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本局商店解锁池。开局仅激光+收束器；解锁事件可加入/替换，上限 12。
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

    /// <summary>商店货架：约 4 攻击位 + 其余功能/随机，全部来自解锁池。</summary>
    public ModuleType RollShopSlotType(int slotIndex)
    {
        if (_unlocked.Count == 0)
        {
            return ModuleType.Redirector;
        }

        var attacks = new List<ModuleType>();
        var utils = new List<ModuleType>();
        for (int i = 0; i < _unlocked.Count; i++)
        {
            ModuleType t = _unlocked[i];
            if (ModuleCatalog.IsAttackModule(t))
            {
                attacks.Add(t);
            }
            else
            {
                utils.Add(t);
            }
        }

        if (slotIndex < 4 && attacks.Count > 0)
        {
            return attacks[Random.Range(0, attacks.Count)];
        }

        if (slotIndex == 4 && utils.Count > 0)
        {
            return utils[Random.Range(0, utils.Count)];
        }

        return _unlocked[Random.Range(0, _unlocked.Count)];
    }

    /// <summary>为满池三选一生成尽量不重复的替换目标。</summary>
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
}
