using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每 3 波结束提供模块解锁三选一；满池时选项带「替换已有」。
/// </summary>
public class ModuleUnlockDirector : MonoBehaviour
{
    RunModulePool _pool;
    DraftChoiceView _draftUi;
    Action _onFinished;
    readonly List<DraftChoiceView.Option> _options = new List<DraftChoiceView.Option>(3);

    public void Initialize(RunModulePool pool, DraftChoiceView draftUi)
    {
        _pool = pool;
        _draftUi = draftUi;
    }

    public bool ShouldOfferAfterWave(int waveDisplay)
    {
        return waveDisplay > 0 && waveDisplay % 3 == 0 && waveDisplay < 15;
    }

    public void BeginDraft(int waveDisplay, Action onFinished)
    {
        _onFinished = onFinished;
        if (_pool == null || _draftUi == null)
        {
            Finish();
            return;
        }

        BuildOptions(waveDisplay);
        if (_options.Count == 0)
        {
            Finish();
            return;
        }

        _draftUi.Show(_options, OnPicked, Finish);
    }

    void BuildOptions(int waveDisplay)
    {
        _options.Clear();
        var candidates = GetCandidatesForWave(waveDisplay);
        // 去掉已在池中的
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (_pool.Contains(candidates[i]))
            {
                candidates.RemoveAt(i);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        Shuffle(candidates);
        int take = Mathf.Min(3, candidates.Count);
        bool full = _pool.IsFull;
        List<ModuleType> replaces = full
            ? _pool.PickDistinctReplaceTargets(take)
            : null;

        for (int i = 0; i < take; i++)
        {
            ModuleType add = candidates[i];
            ModuleType? remove = null;
            if (full && replaces != null && i < replaces.Count)
            {
                // 避免替换成自己（理论上 candidates 已不在池）
                remove = replaces[i];
            }

            _options.Add(new DraftChoiceView.Option
            {
                AddType = add,
                ReplaceType = remove
            });
        }
    }

    List<ModuleType> GetCandidatesForWave(int waveDisplay)
    {
        var list = new List<ModuleType>();
        // 波3：炸弹、寒冰为主
        list.Add(ModuleType.Bomb);
        list.Add(ModuleType.IceLaser);
        list.Add(ModuleType.Miner);
        // 波段仅影响「优先展示」，三者都进候选；已拥有的会被过滤
        return list;
    }

    static void Shuffle(List<ModuleType> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void OnPicked(DraftChoiceView.Option opt)
    {
        if (_pool == null)
        {
            Finish();
            return;
        }

        if (opt.ReplaceType.HasValue)
        {
            _pool.TryReplace(opt.ReplaceType.Value, opt.AddType);
        }
        else
        {
            _pool.TryAdd(opt.AddType);
        }

        Finish();
    }

    void Finish()
    {
        _draftUi?.Hide();
        Action cb = _onFinished;
        _onFinished = null;
        cb?.Invoke();
    }
}
