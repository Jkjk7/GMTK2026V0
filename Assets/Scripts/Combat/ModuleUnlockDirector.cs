using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 前 15 波每 3 波、之后每 2 波提供一次模块解锁三选一；满池时选项带「替换已有」。
/// 每次出现选项可免费刷新一次（类似金铲铲海克斯）。
/// </summary>
public class ModuleUnlockDirector : MonoBehaviour
{
    RunModulePool _pool;
    DraftChoiceView _draftUi;
    Action _onFinished;
    readonly List<DraftChoiceView.Option> _options = new List<DraftChoiceView.Option>(3);
    int _waveDisplay;
    bool _canRefresh;

    public void Initialize(RunModulePool pool, DraftChoiceView draftUi)
    {
        _pool = pool;
        _draftUi = draftUi;
    }

    public bool ShouldOfferAfterWave(int waveDisplay)
    {
        if (waveDisplay <= 0 || waveDisplay >= WaveSpawnBudget.WaveCount)
        {
            return false;
        }

        return waveDisplay < 15
            ? waveDisplay % 3 == 0
            : waveDisplay % 2 == 0;
    }

    public void BeginDraft(int waveDisplay, Action onFinished)
    {
        _onFinished = onFinished;
        _waveDisplay = waveDisplay;
        _canRefresh = true;
        if (_pool == null || _draftUi == null)
        {
            Finish();
            return;
        }

        PresentOptions();
    }

    void PresentOptions()
    {
        BuildOptions(_waveDisplay);
        if (_options.Count == 0)
        {
            Finish();
            return;
        }

        _draftUi.Show(
            _options,
            OnPicked,
            Finish,
            _canRefresh ? OnRefresh : null);
    }

    void OnRefresh()
    {
        if (!_canRefresh)
        {
            return;
        }

        _canRefresh = false;
        PresentOptions();
    }

    void BuildOptions(int waveDisplay)
    {
        var exclude = new HashSet<ModuleType>();
        if (!_canRefresh)
        {
            // 刷新时尽量避开刚才那一组
            for (int i = 0; i < _options.Count; i++)
            {
                exclude.Add(_options[i].AddType);
            }
        }

        _options.Clear();
        var candidates = GetCandidatesForWave(waveDisplay);
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (_pool.Contains(candidates[i]) || exclude.Contains(candidates[i]))
            {
                candidates.RemoveAt(i);
            }
        }

        // 排除后不够 3 个时，放宽：只排除已在池中的
        if (candidates.Count < 3 && exclude.Count > 0)
        {
            candidates = GetCandidatesForWave(waveDisplay);
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (_pool.Contains(candidates[i]))
                {
                    candidates.RemoveAt(i);
                }
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
        list.Add(ModuleType.Bomb);
        list.Add(ModuleType.FrostBomb);
        list.Add(ModuleType.Miner);
        list.Add(ModuleType.Portal);
        list.Add(ModuleType.Relay);
        list.Add(ModuleType.Accelerator);
        list.Add(ModuleType.FireEnchant);
        list.Add(ModuleType.Surprise);
        list.Add(ModuleType.Heatwave);
        list.Add(ModuleType.FrostFreeze);
        list.Add(ModuleType.ArcaneMissile);
        list.Add(ModuleType.FlameWall);
        // 寒冰/火花已在开局池，不再进解锁候选
        if (waveDisplay >= 6)
        {
            list.Add(ModuleType.FlameAmp);
            list.Add(ModuleType.IceAmp);
            list.Add(ModuleType.FrostCannon);
        }

        // 史诗：偏后期
        if (waveDisplay >= 9)
        {
            list.Add(ModuleType.BlackHole);
            list.Add(ModuleType.Fusion);
            list.Add(ModuleType.Fission);
            list.Add(ModuleType.LaserCannon);
        }

        if (waveDisplay >= 12)
        {
            list.Add(ModuleType.Splitter);
        }

        if (waveDisplay >= 15)
        {
            list.Add(ModuleType.FlameBlessing);
            list.Add(ModuleType.Purify);
            list.Add(ModuleType.FrostMushroom);
        }

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
        _canRefresh = false;
        cb?.Invoke();
    }
}
