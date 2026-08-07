using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 开始界面：开始游戏 / 设置 / 退出。设置含语言、全屏与开发者模式（密码 314）。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    Font _font;
    GameObject _settingsRoot;
    GameObject _quitConfirmRoot;
    Text _devStatusText;
    Text _devErrorText;
    InputField _devPasswordField;
    Button _langEnButton;
    Button _langZhButton;
    Button _fullscreenButton;
    Text _fullscreenLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void HookSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameFlow.IsMainMenuScene(scene.name))
        {
            return;
        }

        if (FindObjectOfType<MainMenuController>() != null)
        {
            return;
        }

        var go = new GameObject("MainMenuController");
        go.AddComponent<MainMenuController>();
    }

    void Awake()
    {
        GameSettings.EnsureLoaded();
        EnsureEventSystem();
        EnsureCamera();
        _font = ResolveUiFont();
        BuildUi();
        RefreshLanguageButtons();
        RefreshDevStatus();
        RefreshFullscreenButton();
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        if (_quitConfirmRoot != null && _quitConfirmRoot.activeSelf)
        {
            HideQuitConfirm();
            return;
        }

        if (_settingsRoot != null && _settingsRoot.activeSelf)
        {
            SetSettingsVisible(false);
        }
    }

    void EnsureCamera()
    {
        if (Camera.main != null)
        {
            return;
        }

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        camGo.tag = "MainCamera";
        camGo.AddComponent<AudioListener>();
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void BuildUi()
    {
        var canvasGo = new GameObject("MainMenuCanvas");
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = CreateImage(canvasGo.transform, "Background", Vector2.zero, Vector2.one);
        bg.color = new Color(0.07f, 0.08f, 0.11f, 1f);

        var accent = CreateImage(
            canvasGo.transform,
            "Accent",
            new Vector2(0f, 0.55f),
            new Vector2(1f, 1f));
        accent.color = new Color(0.12f, 0.16f, 0.22f, 1f);

        Text title = CreateText(
            canvasGo.transform,
            "Title",
            new Vector2(0.1f, 0.72f),
            new Vector2(0.9f, 0.9f),
            56,
            FontStyle.Bold,
            new Color(0.95f, 0.86f, 0.45f, 1f));
        title.text = "TimeSands";
        title.alignment = TextAnchor.MiddleCenter;

        Text subtitle = CreateText(
            canvasGo.transform,
            "Subtitle",
            new Vector2(0.15f, 0.64f),
            new Vector2(0.85f, 0.74f),
            22,
            FontStyle.Normal,
            new Color(0.75f, 0.8f, 0.88f, 1f));
        subtitle.text = "Athor: JKSeven, Czhan;";
        subtitle.alignment = TextAnchor.MiddleCenter;

        CreateMenuButton(
            canvasGo.transform,
            "StartButton",
            new Vector2(0.35f, 0.46f),
            new Vector2(0.65f, 0.56f),
            GameLocalization.Text("Start Game", "开始游戏"),
            StartGame);

        CreateMenuButton(
            canvasGo.transform,
            "SettingsButton",
            new Vector2(0.35f, 0.34f),
            new Vector2(0.65f, 0.44f),
            GameLocalization.Text("Settings", "设置"),
            () => SetSettingsVisible(true));

        CreateMenuButton(
            canvasGo.transform,
            "QuitButton",
            new Vector2(0.35f, 0.22f),
            new Vector2(0.65f, 0.32f),
            GameLocalization.Text("Quit", "退出游戏"),
            ShowQuitConfirm);

        BuildSettingsPanel(canvasGo.transform);
        BuildQuitConfirm(canvasGo.transform);
    }

    void BuildSettingsPanel(Transform canvas)
    {
        _settingsRoot = new GameObject("SettingsPanel");
        _settingsRoot.transform.SetParent(canvas, false);
        var rootRt = _settingsRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);

        var dim = _settingsRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);

        var panel = CreateImage(
            _settingsRoot.transform,
            "Panel",
            new Vector2(0.28f, 0.12f),
            new Vector2(0.72f, 0.88f));
        panel.color = new Color(0.1f, 0.12f, 0.16f, 0.98f);

        Text title = CreateText(
            panel.transform,
            "SettingsTitle",
            new Vector2(0.08f, 0.9f),
            new Vector2(0.92f, 0.98f),
            28,
            FontStyle.Bold,
            new Color(0.95f, 0.88f, 0.5f, 1f));
        title.text = GameLocalization.Text("Settings", "设置");
        title.alignment = TextAnchor.MiddleCenter;

        Text displayLabel = CreateText(
            panel.transform,
            "DisplayLabel",
            new Vector2(0.08f, 0.8f),
            new Vector2(0.92f, 0.88f),
            20,
            FontStyle.Normal,
            new Color(0.85f, 0.9f, 0.95f, 1f));
        displayLabel.text = GameLocalization.Text("Display", "显示");
        displayLabel.alignment = TextAnchor.MiddleLeft;

        _fullscreenButton = CreateMenuButton(
            panel.transform,
            "FullscreenToggle",
            new Vector2(0.08f, 0.68f),
            new Vector2(0.92f, 0.78f),
            string.Empty,
            () =>
            {
                GameSettings.ToggleFullscreen();
                RefreshFullscreenButton();
            });
        _fullscreenLabel = _fullscreenButton.GetComponentInChildren<Text>();

        Text langLabel = CreateText(
            panel.transform,
            "LangLabel",
            new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.66f),
            20,
            FontStyle.Normal,
            new Color(0.85f, 0.9f, 0.95f, 1f));
        langLabel.text = GameLocalization.Text("Language", "语言");
        langLabel.alignment = TextAnchor.MiddleLeft;

        _langEnButton = CreateMenuButton(
            panel.transform,
            "LangEN",
            new Vector2(0.08f, 0.46f),
            new Vector2(0.48f, 0.56f),
            "English",
            () =>
            {
                GameSettings.SetLanguage(GameLanguage.English);
                RefreshLanguageButtons();
                RebuildForLanguage();
            });

        _langZhButton = CreateMenuButton(
            panel.transform,
            "LangZH",
            new Vector2(0.52f, 0.46f),
            new Vector2(0.92f, 0.56f),
            "中文",
            () =>
            {
                GameSettings.SetLanguage(GameLanguage.SimplifiedChinese);
                RefreshLanguageButtons();
                RebuildForLanguage();
            });

        Text devLabel = CreateText(
            panel.transform,
            "DevLabel",
            new Vector2(0.08f, 0.36f),
            new Vector2(0.92f, 0.44f),
            20,
            FontStyle.Normal,
            new Color(0.85f, 0.9f, 0.95f, 1f));
        devLabel.text = GameLocalization.Text("Developer Mode", "开发者模式");
        devLabel.alignment = TextAnchor.MiddleLeft;

        _devPasswordField = CreateInputField(
            panel.transform,
            "DevPassword",
            new Vector2(0.08f, 0.26f),
            new Vector2(0.62f, 0.34f),
            GameLocalization.Text("Password", "密码"));

        CreateMenuButton(
            panel.transform,
            "DevUnlock",
            new Vector2(0.64f, 0.26f),
            new Vector2(0.92f, 0.34f),
            GameLocalization.Text("Unlock", "开启"),
            TryUnlockDeveloper);

        CreateMenuButton(
            panel.transform,
            "DevDisable",
            new Vector2(0.08f, 0.16f),
            new Vector2(0.48f, 0.24f),
            GameLocalization.Text("Disable", "关闭"),
            () =>
            {
                GameSettings.DisableDeveloperMode();
                RefreshDevStatus();
            });

        _devStatusText = CreateText(
            panel.transform,
            "DevStatus",
            new Vector2(0.5f, 0.16f),
            new Vector2(0.92f, 0.24f),
            16,
            FontStyle.Bold,
            new Color(0.55f, 0.9f, 0.6f, 1f));
        _devStatusText.alignment = TextAnchor.MiddleCenter;

        _devErrorText = CreateText(
            panel.transform,
            "DevError",
            new Vector2(0.08f, 0.08f),
            new Vector2(0.92f, 0.14f),
            15,
            FontStyle.Normal,
            new Color(1f, 0.45f, 0.4f, 1f));
        _devErrorText.alignment = TextAnchor.MiddleCenter;
        _devErrorText.text = string.Empty;

        CreateMenuButton(
            panel.transform,
            "CloseSettings",
            new Vector2(0.3f, 0.01f),
            new Vector2(0.7f, 0.07f),
            GameLocalization.Text("Back", "返回"),
            () => SetSettingsVisible(false));

        SetSettingsVisible(false);
    }

    void BuildQuitConfirm(Transform canvas)
    {
        _quitConfirmRoot = new GameObject("QuitConfirm");
        _quitConfirmRoot.transform.SetParent(canvas, false);
        var rootRt = _quitConfirmRoot.AddComponent<RectTransform>();
        StretchFull(rootRt);

        var dim = _quitConfirmRoot.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.72f);

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

        CreateMenuButton(
            panel.transform,
            "Yes",
            new Vector2(0.08f, 0.1f),
            new Vector2(0.46f, 0.38f),
            GameLocalization.Text("Confirm", "确定"),
            QuitGame);

        CreateMenuButton(
            panel.transform,
            "No",
            new Vector2(0.54f, 0.1f),
            new Vector2(0.92f, 0.38f),
            GameLocalization.Text("Cancel", "取消"),
            HideQuitConfirm);

        HideQuitConfirm();
    }

    void TryUnlockDeveloper()
    {
        string password = _devPasswordField != null ? _devPasswordField.text : string.Empty;
        if (GameSettings.TryEnableDeveloperMode(password))
        {
            if (_devErrorText != null)
            {
                _devErrorText.text = string.Empty;
            }

            if (_devPasswordField != null)
            {
                _devPasswordField.text = string.Empty;
            }

            RefreshDevStatus();
            return;
        }

        if (_devErrorText != null)
        {
            _devErrorText.text = GameLocalization.Text("Wrong password", "密码错误");
        }
    }

    void RefreshDevStatus()
    {
        if (_devStatusText == null)
        {
            return;
        }

        _devStatusText.text = GameSettings.DeveloperMode
            ? GameLocalization.Text("ON", "已开启")
            : GameLocalization.Text("OFF", "未开启");
        _devStatusText.color = GameSettings.DeveloperMode
            ? new Color(0.55f, 0.9f, 0.6f, 1f)
            : new Color(0.7f, 0.55f, 0.5f, 1f);
    }

    void RefreshLanguageButtons()
    {
        bool zh = GameLocalization.IsChinese;
        StyleLangButton(_langEnButton, !zh);
        StyleLangButton(_langZhButton, zh);
    }

    static void StyleLangButton(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = selected
                ? new Color(0.35f, 0.42f, 0.22f, 1f)
                : new Color(0.18f, 0.2f, 0.26f, 1f);
        }
    }

    void RebuildForLanguage()
    {
        // 语言影响主菜单文案：立即销毁再重建，避免同帧双 Canvas
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        BuildUi();
        RefreshLanguageButtons();
        RefreshDevStatus();
        RefreshFullscreenButton();
        SetSettingsVisible(true);
    }

    void SetSettingsVisible(bool visible)
    {
        if (_settingsRoot != null)
        {
            _settingsRoot.SetActive(visible);
        }

        if (visible)
        {
            HideQuitConfirm();
            RefreshLanguageButtons();
            RefreshDevStatus();
            RefreshFullscreenButton();
            if (_devErrorText != null)
            {
                _devErrorText.text = string.Empty;
            }
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

    void ShowQuitConfirm()
    {
        SetSettingsVisible(false);
        if (_quitConfirmRoot != null)
        {
            _quitConfirmRoot.SetActive(true);
            _quitConfirmRoot.transform.SetAsLastSibling();
        }
    }

    void HideQuitConfirm()
    {
        if (_quitConfirmRoot != null)
        {
            _quitConfirmRoot.SetActive(false);
        }
    }

    void StartGame()
    {
        GameFlow.LoadGameForCurrentLanguage();
    }

    void QuitGame()
    {
        GameSettings.QuitApplication();
    }

    Button CreateMenuButton(
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
            22,
            FontStyle.Bold,
            new Color(0.95f, 0.92f, 0.8f, 1f));
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return button;
    }

    InputField CreateInputField(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        string placeholder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.14f, 0.15f, 0.2f, 1f);

        var input = go.AddComponent<InputField>();
        input.contentType = InputField.ContentType.Password;
        input.lineType = InputField.LineType.SingleLine;

        Text text = CreateText(
            go.transform,
            "Text",
            new Vector2(0.05f, 0f),
            new Vector2(0.95f, 1f),
            18,
            FontStyle.Normal,
            Color.white);
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        Text ph = CreateText(
            go.transform,
            "Placeholder",
            new Vector2(0.05f, 0f),
            new Vector2(0.95f, 1f),
            18,
            FontStyle.Italic,
            new Color(1f, 1f, 1f, 0.35f));
        ph.text = placeholder;
        ph.alignment = TextAnchor.MiddleLeft;
        ph.raycastTarget = false;

        input.textComponent = text;
        input.placeholder = ph;
        return input;
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

    static Font ResolveUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return font;
    }
}
