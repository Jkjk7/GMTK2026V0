using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 三选一草稿：模块发现（图标+描述），或通用文本选项。
/// 模块发现可带一次刷新（类似金铲铲海克斯）。
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
    [SerializeField] Image[] buttonIcons;
    [SerializeField] Text[] buttonDescs;
    [SerializeField] Button refreshButton;
    [SerializeField] Text refreshLabel;

    Action<Option> _onPick;
    Action<int> _onPickIndex;
    Action _onSkip;
    Action _onRefresh;
    readonly List<Option> _options = new List<Option>();
    DraftChoiceHoverTarget[] _hovers;
    RewardReveal _reveal;
    Color[] _rarityColors;
    bool _visible;
    int _customCount;

    public void Bind(
        CanvasGroup canvasGroup,
        Text title,
        Button[] btns,
        Text[] labels,
        Image[] icons = null,
        Text[] descs = null,
        Button refreshBtn = null,
        Text refreshText = null)
    {
        group = canvasGroup;
        titleText = title;
        buttons = btns;
        buttonLabels = labels;
        buttonIcons = icons;
        buttonDescs = descs;
        refreshButton = refreshBtn;
        refreshLabel = refreshText;
        _reveal = RewardReveal.Attach(transform as RectTransform, buttons);
        _rarityColors = new Color[buttons.Length];
        _hovers = new DraftChoiceHoverTarget[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            int idx = i;
            buttons[i].onClick.AddListener(() => OnClick(idx));
            UiFeedback.Attach(buttons[i].gameObject);
            _hovers[i] = buttons[i].GetComponent<DraftChoiceHoverTarget>();
            if (_hovers[i] == null)
            {
                _hovers[i] = buttons[i].gameObject.AddComponent<DraftChoiceHoverTarget>();
            }
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(OnRefreshClicked);
            UiFeedback.Attach(refreshButton.gameObject);
        }

        Hide();
    }

    public void Show(List<Option> options, Action<Option> onPick, Action onSkip = null, Action onRefresh = null)
    {
        _options.Clear();
        if (options != null)
        {
            _options.AddRange(options);
        }

        _onPick = onPick;
        _onPickIndex = null;
        _onSkip = onSkip;
        _onRefresh = onRefresh;
        _customCount = 0;
        ClearHovers();

        if (titleText != null)
        {
            titleText.text = GameLocalization.Text(
                "New module discovered! Add one to the shop pool",
                "发现新模块！选择一个加入商店池");
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = i < _options.Count;
            buttons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            SetCardLayout(i, false);

            Option opt = _options[i];
            ModuleRarity rarity = ModuleCatalog.GetRarity(opt.AddType);
            _rarityColors[i] = ModuleCatalog.GetRarityColor(rarity);
            string name = ModuleCatalog.GetDisplayName(opt.AddType);
            string rarityName = ModuleCatalog.GetRarityName(rarity);
            string line = $"{name}\n[{rarityName}]";
            if (opt.ReplaceType.HasValue)
            {
                line += GameLocalization.Text(
                    $"\n(Replaces {ModuleCatalog.GetDisplayName(opt.ReplaceType.Value)})",
                    $"\n(替换{ModuleCatalog.GetDisplayName(opt.ReplaceType.Value)})");
            }

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
            {
                buttonLabels[i].text = line;
                buttonLabels[i].color = ModuleCatalog.GetRarityColor(rarity);
            }

            if (buttonIcons != null && i < buttonIcons.Length && buttonIcons[i] != null)
            {
                buttonIcons[i].gameObject.SetActive(true);
                ModuleIconVisuals.Apply(buttonIcons[i], opt.AddType);
            }

            if (buttonDescs != null && i < buttonDescs.Length && buttonDescs[i] != null)
            {
                buttonDescs[i].gameObject.SetActive(true);
                buttonDescs[i].text = ModuleCatalog.GetDescription(opt.AddType);
            }

            if (_hovers != null && i < _hovers.Length && _hovers[i] != null)
            {
                _hovers[i].SetModule(opt.AddType);
            }
        }

        SetRefreshVisible(_onRefresh != null);
        SetVisible(true);
        _reveal?.Show(_rarityColors);
    }

    public void ShowCustom(string title, IList<string> labels, Action<int> onPick, Action onSkip = null)
    {
        _options.Clear();
        _onPick = null;
        _onPickIndex = onPick;
        _onSkip = onSkip;
        _onRefresh = null;
        _customCount = labels != null ? labels.Count : 0;
        ClearHovers();
        SetRefreshVisible(false);

        if (titleText != null)
        {
            titleText.text = string.IsNullOrEmpty(title)
                ? GameLocalization.Text("Choose one", "请选择")
                : title;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            bool active = labels != null && i < labels.Count;
            buttons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            SetCardLayout(i, true);

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
            {
                buttonLabels[i].text = labels[i];
                buttonLabels[i].color = Color.white;
            }

            if (buttonIcons != null && i < buttonIcons.Length && buttonIcons[i] != null)
            {
                Image icon = buttonIcons[i];
                icon.gameObject.SetActive(true);
                icon.sprite = RewardReveal.UpgradeEmblem;
                icon.color = RewardReveal.CustomAccent(i);
                icon.type = Image.Type.Simple;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            if (buttonDescs != null && i < buttonDescs.Length && buttonDescs[i] != null)
            {
                buttonDescs[i].gameObject.SetActive(false);
            }
        }

        SetVisible(true);
        _reveal?.Show();
    }

    void SetCardLayout(int index, bool custom)
    {
        if (buttonLabels != null && index < buttonLabels.Length && buttonLabels[index] != null)
        {
            Text label = buttonLabels[index];
            CardArea(label.rectTransform, new Vector2(0.07f, custom ? 0.07f : 0.30f),
                new Vector2(0.93f, custom ? 0.59f : 0.61f));
            label.fontSize = custom ? 22 : 20;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = custom ? 22 : 20;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.lineSpacing = 1.08f;
        }
        if (buttonIcons != null && index < buttonIcons.Length && buttonIcons[index] != null)
        {
            RectTransform icon = buttonIcons[index].rectTransform;
            CardArea(icon, new Vector2(custom ? 0.28f : 0.2f, 0.64f),
                new Vector2(custom ? 0.72f : 0.8f, 0.93f));
        }
        if (buttonDescs != null && index < buttonDescs.Length && buttonDescs[index] != null)
        {
            Text description = buttonDescs[index];
            CardArea(description.rectTransform, new Vector2(0.07f, 0.04f), new Vector2(0.93f, 0.28f));
            description.fontSize = 14;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 12;
            description.resizeTextMaxSize = 14;
            description.horizontalOverflow = HorizontalWrapMode.Wrap;
            description.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }

    static void CardArea(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    public void Hide()
    {
        _reveal?.Hide();
        if (buttons != null)
            foreach (Button button in buttons)
                if (button != null) UiFeedback.Cancel(button.transform);
        if (refreshButton != null) UiFeedback.Cancel(refreshButton.transform);
        ClearHovers();
        ModuleTooltipView.HideAll();
        _onPick = null;
        _onPickIndex = null;
        _onSkip = null;
        _onRefresh = null;
        _customCount = 0;
        SetRefreshVisible(false);
        SetVisible(false);
    }

    void SetRefreshVisible(bool visible)
    {
        if (refreshButton != null)
        {
            refreshButton.gameObject.SetActive(visible);
            refreshButton.interactable = visible;
        }

        if (visible && refreshLabel != null)
        {
            refreshLabel.text = GameLocalization.Text("Refresh (1)", "刷新（1次）");
        }
    }

    void ClearHovers()
    {
        if (_hovers == null)
        {
            return;
        }

        for (int i = 0; i < _hovers.Length; i++)
        {
            _hovers[i]?.Clear();
        }
    }

    void SetVisible(bool visible)
    {
        _visible = visible;
        if (group != null)
        {
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }

        if (visible)
        {
            gameObject.SetActive(true);
            // 盖过「查看已有增幅」等后创建的 HUD，避免挡住选项点击
            transform.SetAsLastSibling();
        }
    }

    void OnRefreshClicked()
    {
        if (!_visible || _onRefresh == null)
        {
            return;
        }

        Action refresh = _onRefresh;
        _onRefresh = null;
        SetRefreshVisible(false);
        refresh.Invoke();
    }

    void OnClick(int index)
    {
        if (!_visible) return;
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

    void OnDisable() => Hide();
}

/// <summary>模块三选一选项悬停：显示与商店/手牌相同的描述弹窗。</summary>
public class DraftChoiceHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    ModuleCardData _card;
    bool _enabled;

    public void SetModule(ModuleType type)
    {
        _card = ModuleCardData.Create(type, 1, 0);
        _enabled = true;
    }

    public void Clear()
    {
        _enabled = false;
        ModuleTooltipView.EndHover(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_enabled)
        {
            ModuleTooltipView.BeginHover(this, _card);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ModuleTooltipView.EndHover(this);
    }
}
