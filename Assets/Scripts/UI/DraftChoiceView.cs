using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三选一草稿：模块发现，或通用文本选项（发射器强化等）。
/// </summary>
public class DraftChoiceView : MonoBehaviour
{
    public struct Option
    {
        public ModuleType AddType;
        public ModuleType? ReplaceType;
    }

    [SerializeField] CanvasGroup group;
    [SerializeField] Text titleText;
    [SerializeField] Button[] buttons;
    [SerializeField] Text[] buttonLabels;

    Action<Option> _onPick;
    Action<int> _onPickIndex;
    Action _onSkip;
    readonly List<Option> _options = new List<Option>();
    int _customCount;

    public void Bind(CanvasGroup canvasGroup, Text title, Button[] btns, Text[] labels)
    {
        group = canvasGroup;
        titleText = title;
        buttons = btns;
        buttonLabels = labels;
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onClick.AddListener(() => OnClick(idx));
        }

        Hide();
    }

    public void Show(List<Option> options, Action<Option> onPick, Action onSkip = null)
    {
        _options.Clear();
        if (options != null)
        {
            _options.AddRange(options);
        }

        _onPick = onPick;
        _onPickIndex = null;
        _onSkip = onSkip;
        _customCount = 0;

        if (titleText != null)
        {
            titleText.text = "发现新模块！选择一个加入商店池";
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = i < _options.Count;
            buttons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            Option opt = _options[i];
            string name = ModuleCatalog.GetDisplayName(opt.AddType);
            string tag = ModuleCatalog.GetTag(opt.AddType);
            string line = string.IsNullOrEmpty(tag) ? name : $"{name}\n[{tag}]";
            if (opt.ReplaceType.HasValue)
            {
                line += $"\n(替换{ModuleCatalog.GetDisplayName(opt.ReplaceType.Value)})";
            }

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
            {
                buttonLabels[i].text = line;
            }
        }

        SetVisible(true);
    }

    public void ShowCustom(string title, IList<string> labels, Action<int> onPick, Action onSkip = null)
    {
        _options.Clear();
        _onPick = null;
        _onPickIndex = onPick;
        _onSkip = onSkip;
        _customCount = labels != null ? labels.Count : 0;

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title) ? "请选择" : title;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = labels != null && i < labels.Count;
            buttons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
            {
                buttonLabels[i].text = labels[i];
            }
        }

        SetVisible(true);
    }

    public void Hide()
    {
        _onPick = null;
        _onPickIndex = null;
        _onSkip = null;
        _customCount = 0;
        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }

        if (visible)
        {
            gameObject.SetActive(true);
        }
    }

    void OnClick(int index)
    {
        if (_onPickIndex != null)
        {
            if (index < 0 || index >= _customCount)
            {
                return;
            }

            Action<int> pick = _onPickIndex;
            Hide();
            pick?.Invoke(index);
            return;
        }

        if (index < 0 || index >= _options.Count)
        {
            return;
        }

        Option opt = _options[index];
        Action<Option> modulePick = _onPick;
        Hide();
        modulePick?.Invoke(opt);
    }

    void Update()
    {
        if (group != null && group.blocksRaycasts && Input.GetKeyDown(KeyCode.Escape))
        {
            Action skip = _onSkip;
            Hide();
            skip?.Invoke();
        }
    }
}
