using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 模块发现三选一：可显示「替换 xxx」。
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
    Action _onSkip;
    readonly List<Option> _options = new List<Option>();

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
        _onSkip = onSkip;

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

        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _onPick = null;
        _onSkip = null;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    void OnClick(int index)
    {
        if (index < 0 || index >= _options.Count)
        {
            return;
        }

        Option opt = _options[index];
        Action<Option> pick = _onPick;
        Hide();
        pick?.Invoke(opt);
    }

    void Update()
    {
        // Escape 跳过（不选）
        if (group != null && group.blocksRaycasts && Input.GetKeyDown(KeyCode.Escape))
        {
            Action skip = _onSkip;
            Hide();
            skip?.Invoke();
        }
    }
}
