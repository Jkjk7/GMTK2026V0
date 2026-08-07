using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 局内 ESC 设置：全屏/窗口、继续、退出（二次确认）。
/// 仅作最上层遮罩；不取消草稿/确认/拖拽，关闭后原界面继续。
/// </summary>
public class InGameSettingsHud : MonoBehaviour
{
    static InGameSettingsHud s_instance;

    Font _font;

    GameObject _root;
    GameObject _quitConfirmRoot;
    Button _fullscreenButton;
    Text _fullscreenLabel;
    float _savedTimeScale = 1f;
    bool _open;
    bool _quitConfirmOpen;

    public static bool IsOpen => s_instance != null && s_instance._open;

    public void Initialize(Font font)
    {
        _font = font;
        BuildUi();
        ForceClosed();
        RefreshFullscreenButton();
    }

    void OnEnable()
    {
        s_instance = this;
    }

    void OnDisable()
    {
        if (s_instance == this)
        {
            s_instance = null;
        }

        if (_open)
        {
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
            _open = false;
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (_quitConfirmOpen)
        {
            HideQuitConfirm();
            return;
        }

        SetOpen(!_open);
    }

    /// <summary>初始化时强制关闭：不能依赖 SetOpen(false)（此时 _open 已是 false 会早退）。</summary>
    void ForceClosed()
    {
        _open = false;
        if (_root != null)
        {
            _root.SetActive(false);
        }

        HideQuitConfirm();
        if (Time.timeScale <= 0f)
        {
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }
    }

    void SetOpen(bool open)
    {
        if (_open == open)
        {
            // 状态应与面板一致；若不一致则纠偏（防止初始化漏关）
            if (_root != null && _root.activeSelf != open)
            {
                _root.SetActive(open);
            }

            if (open)
            {
                BringToFront();
                RefreshFullscreenButton();
            }

            return;
        }

        _open = open;
        if (_root != null)
        {
            _root.SetActive(open);
        }

        if (open)
        {
            HideQuitConfirm();
            BringToFront();
            _savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            RefreshFullscreenButton();
        }
        else
        {
            HideQuitConfirm();
            Time.timeScale = _savedTimeScale > 0f ? _savedTimeScale : 1f;
        }
    }

    void BringToFront()
    {
        transform.SetAsLastSibling();
        if (_root != null)
        {
            _root.transform.SetAsLastSibling();
        }
    }

    void BuildUi()
    {
        _root = new GameObject("InGameSettingsRoot");
        _root.transform.SetParent(transform, false);
        var rootRt = _root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        var dim = _root.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = CreateImage(
            _root.transform,
            "Panel",
            new Vector2(0.32f, 0.28f),
            new Vector2(0.68f, 0.72f));
        panel.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

        Text title = CreateText(
            panel.transform,
            "Title",
            new Vector2(0.08f, 0.78f),
            new Vector2(0.92f, 0.94f),
            28,
            FontStyle.Bold,
            new Color(0.95f, 0.88f, 0.5f, 1f));
        title.text = GameLocalization.Text("Settings", "设置");
        title.alignment = TextAnchor.MiddleCenter;

        _fullscreenButton = CreateButton(
            panel.transform,
            "FullscreenToggle",
            new Vector2(0.12f, 0.52f),
            new Vector2(0.88f, 0.68f),
            string.Empty,
            () =>
            {
                GameSettings.ToggleFullscreen();
                RefreshFullscreenButton();
            });
        _fullscreenLabel = _fullscreenButton.GetComponentInChildren<Text>();

        CreateButton(
            panel.transform,
            "Resume",
            new Vector2(0.12f, 0.32f),
            new Vector2(0.88f, 0.46f),
            GameLocalization.Text("Resume", "继续游戏"),
            () => SetOpen(false));

        CreateButton(
            panel.transform,
            "Quit",
            new Vector2(0.12f, 0.12f),
            new Vector2(0.88f, 0.26f),
            GameLocalization.Text("Quit", "退出游戏"),
            ShowQuitConfirm);

        BuildQuitConfirm();
    }

    void BuildQuitConfirm()
    {
        _quitConfirmRoot = new GameObject("QuitConfirm");
        _quitConfirmRoot.transform.SetParent(transform, false);
        var rt = _quitConfirmRoot.AddComponent<RectTransform>();
        StretchFull(rt);
        var dim = _quitConfirmRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);

        var panel = CreateImage(
            _quitConfirmRoot.transform,
            "Panel",
            new Vector2(0.3f, 0.38f),
            new Vector2(0.7f, 0.62f));
        panel.color = new Color(0.12f, 0.13f, 0.18f, 1f);

        Text body = CreateText(
            panel.transform,
            "Body",
            new Vector2(0.08f, 0.45f),
            new Vector2(0.92f, 0.9f),
            20,
            FontStyle.Normal,
            new Color(0.92f, 0.9f, 0.85f, 1f));
        body.text = GameLocalization.Text(
            "Are you sure you want to quit?",
            "确定要退出游戏吗？");
        body.alignment = TextAnchor.MiddleCenter;

        CreateButton(
            panel.transform,
            "Yes",
            new Vector2(0.08f, 0.1f),
            new Vector2(0.46f, 0.38f),
            GameLocalization.Text("Confirm", "确定"),
            () =>
            {
                Time.timeScale = 1f;
                GameSettings.QuitApplication();
            });

        CreateButton(
            panel.transform,
            "No",
            new Vector2(0.54f, 0.1f),
            new Vector2(0.92f, 0.38f),
            GameLocalization.Text("Cancel", "取消"),
            HideQuitConfirm);
    }

    void ShowQuitConfirm()
    {
        _quitConfirmOpen = true;
        if (_quitConfirmRoot != null)
        {
            _quitConfirmRoot.SetActive(true);
            _quitConfirmRoot.transform.SetAsLastSibling();
        }
    }

    void HideQuitConfirm()
    {
        _quitConfirmOpen = false;
        if (_quitConfirmRoot != null)
        {
            _quitConfirmRoot.SetActive(false);
        }
    }

    void RefreshFullscreenButton()
    {
        if (_fullscreenLabel == null)
        {
            return;
        }

        _fullscreenLabel.text = GameSettings.IsFullscreen
            ? GameLocalization.Text("Windowed", "切换为窗口化")
            : GameLocalization.Text("Fullscreen", "切换为全屏");
    }

    Button CreateButton(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string label,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.26f, 1f);
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null)
        {
            button.onClick.AddListener(onClick);
        }

        Text text = CreateText(
            go.transform,
            "Label",
            Vector2.zero,
            Vector2.one,
            20,
            FontStyle.Bold,
            new Color(0.95f, 0.92f, 0.8f, 1f));
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return button;
    }

    Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go.AddComponent<Image>();
    }

    Text CreateText(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        int size,
        FontStyle style,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
