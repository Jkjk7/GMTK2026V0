using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每 5 波结束：发射器强化三选一（从未满档的维度中抽）。
/// </summary>
public class EmitterUpgradeDirector : MonoBehaviour
{
    DraftChoiceView _draftUi;
    EmitterRunUpgrades _upgrades;
    Action _onFinished;
    readonly List<EmitterUpgradeKind> _choices = new List<EmitterUpgradeKind>(3);

    public void Initialize(EmitterRunUpgrades upgrades, DraftChoiceView draftUi)
    {
        _upgrades = upgrades;
        _draftUi = draftUi;
    }

    public bool ShouldOfferAfterWave(int waveDisplay)
    {
        return waveDisplay > 0 && waveDisplay % 5 == 0 && waveDisplay < 15;
    }

    public void BeginDraft(int waveDisplay, Action onFinished)
    {
        _onFinished = onFinished;
        if (_upgrades == null || _draftUi == null)
        {
            Finish();
            return;
        }

        BuildChoices();
        if (_choices.Count == 0)
        {
            Finish();
            return;
        }

        var labels = new List<string>(_choices.Count);
        for (int i = 0; i < _choices.Count; i++)
        {
            labels.Add(_upgrades.FormatOptionLabel(_choices[i]));
        }

        _draftUi.ShowCustom(
            $"发射器强化！（第{waveDisplay}波后）",
            labels,
            OnPickedIndex,
            Finish);
    }

    void BuildChoices()
    {
        _choices.Clear();
        var pool = new List<EmitterUpgradeKind>(4);
        foreach (EmitterUpgradeKind kind in System.Enum.GetValues(typeof(EmitterUpgradeKind)))
        {
            if (_upgrades.CanUpgrade(kind))
            {
                pool.Add(kind);
            }
        }

        Shuffle(pool);
        int take = Mathf.Min(3, pool.Count);
        for (int i = 0; i < take; i++)
        {
            _choices.Add(pool[i]);
        }
    }

    static void Shuffle(List<EmitterUpgradeKind> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void OnPickedIndex(int index)
    {
        if (index >= 0 && index < _choices.Count && _upgrades != null)
        {
            _upgrades.TryUpgrade(_choices[index]);
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
