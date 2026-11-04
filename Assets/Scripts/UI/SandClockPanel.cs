using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 像素沙漏面板：铜框、青色玻璃、与实际资源同步的上下砂堆和毫秒倒计时。
/// 漏怪罚沙时弹浮动 "-X.XXXs" 文字；剩余低于阈值时倒计时变红。
/// 漏沙/回填粒子由 SandVfxService 驱动。
/// </summary>
public class SandClockPanel : MonoBehaviour
{
    static readonly Color NormalColor = new Color(0.95f, 0.88f, 0.62f, 1f);
    static readonly Color WarningColor = new Color(1f, 0.4f, 0.35f, 1f);
    static readonly Color GainColor = new Color(0.5f, 0.95f, 0.55f, 1f);

    Text _countdownText;
    Text _floatText;
    Image _glassTop;
    Image _glassBottom;
    Image _frame;
    Image _reflections;
    readonly Image[] _grains = new Image[6];
    SandClock _clock;
    RectTransform _glassBottomRt;
    int _lastMs;
    float _flowTimer;
    float _flowAge;
    float _ratio;

    float _floatTimer;
    float _shakeTimer;
    Vector2 _basePos;
    RectTransform _rt;

    public Image FrameImage => _frame;

    public void Bind(
        SandClock clock,
        Text countdownText,
        Text floatText,
        Image glassTop,
        Image glassBottom,
        Image frame = null)
    {
        if (_clock != null)
        {
            _clock.OnPenalty -= OnPenalty;
            _clock.OnSandGained -= OnSandGained;
        }
        _clock = clock;
        _countdownText = countdownText;
        _floatText = floatText;
        _glassTop = glassTop;
        _glassBottom = glassBottom;
        _frame = frame;
        _glassBottomRt = glassBottom != null ? glassBottom.rectTransform : null;
        _rt = GetComponent<RectTransform>();
        _basePos = _rt != null ? _rt.anchoredPosition : Vector2.zero;
        _lastMs = _clock != null ? _clock.RemainingMs : 0;
        _flowTimer = 0f;
        PreparePixelArt();

        if (_clock != null)
        {
            _clock.OnPenalty += OnPenalty;
            _clock.OnSandGained += OnSandGained;
        }

        if (_floatText != null)
        {
            _floatText.gameObject.SetActive(false);
        }

        Refresh();
    }

    /// <summary>沙漏下腔中心（世界坐标），漏沙起点。</summary>
    public Vector3 GetWorldLeakPosition()
    {
        return UiToWorld(_frame != null ? _frame.rectTransform : _glassBottomRt != null ? _glassBottomRt : _rt,
            new Vector2(0.5f, _frame != null ? 0.17f : 0.05f));
    }

    /// <summary>沙漏回填落点（世界坐标）。</summary>
    public Vector3 GetWorldFillPosition()
    {
        return UiToWorld(_frame != null ? _frame.rectTransform : _glassBottomRt != null ? _glassBottomRt : _rt,
            new Vector2(0.5f, _frame != null ? 0.27f : 0.55f));
    }

    void PreparePixelArt()
    {
        if (_frame == null) return;
        _frame.sprite = PixelHourglassSprites.Frame;
        _frame.type = Image.Type.Simple;
        _frame.color = Color.white;
        _frame.preserveAspect = true;
        _frame.raycastTarget = false;
        RectTransform frameRect = _frame.rectTransform;
        frameRect.anchorMin = new Vector2(.13f, .01f);
        frameRect.anchorMax = new Vector2(.87f, .70f);
        frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
        PrepareSand(_glassTop, frameRect);
        PrepareSand(_glassBottom, frameRect);
        if (_reflections == null)
        {
            _reflections = MakeImage("GlassReflections", frameRect);
            Stretch(_reflections.rectTransform);
        }
        _reflections.sprite = PixelHourglassSprites.Reflections;
        _reflections.preserveAspect = true;
        if (_glassTop != null) _glassTop.transform.SetSiblingIndex(0);
        if (_glassBottom != null) _glassBottom.transform.SetSiblingIndex(1);
        _reflections.transform.SetAsLastSibling();
        for (int i = 0; i < _grains.Length; i++)
        {
            if (_grains[i] == null) _grains[i] = MakeImage("SandGrain_" + i, frameRect);
            _grains[i].sprite = PixelHourglassSprites.Grain;
            _grains[i].enabled = false;
            _grains[i].transform.SetAsLastSibling();
        }
    }

    static void PrepareSand(Image sand, RectTransform parent)
    {
        if (sand == null) return;
        sand.transform.SetParent(parent, false);
        Stretch(sand.rectTransform);
        sand.type = Image.Type.Simple;
        sand.preserveAspect = true;
        sand.raycastTarget = false;
    }

    static Image MakeImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static Vector3 UiToWorld(RectTransform target, Vector2 normalizedLocal)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        // 0=BL, 1=TL, 2=TR, 3=BR（Screen Space Overlay 下也是屏幕像素世界）
        Vector3 bl = corners[0];
        Vector3 tr = corners[2];
        Vector3 screen = new Vector3(
            Mathf.Lerp(bl.x, tr.x, normalizedLocal.x),
            Mathf.Lerp(bl.y, tr.y, normalizedLocal.y),
            0f);

        Camera cam = Camera.main;
        if (cam == null)
        {
            return screen;
        }

        // Overlay UI 的 worldCorners 已是屏幕像素；转到游戏世界
        float z = Mathf.Abs(cam.transform.position.z);
        Vector3 sp = new Vector3(screen.x, screen.y, z);
        return cam.ScreenToWorldPoint(sp);
    }

    void OnDestroy()
    {
        if (_clock != null)
        {
            _clock.OnPenalty -= OnPenalty;
            _clock.OnSandGained -= OnSandGained;
        }
    }

    void Update()
    {
        Refresh();
        bool motion = PresentationSettings.MotionEnabled;
        TickFlow(motion);

        if (_floatTimer > 0f && _floatText != null)
        {
            _floatTimer -= Time.deltaTime;
            Color c = _floatText.color;
            c.a = motion ? Mathf.Clamp01(_floatTimer / 0.4f) : 1f;
            _floatText.color = c;
            if (_floatTimer <= 0f)
            {
                _floatText.gameObject.SetActive(false);
            }
        }

        if (!motion && _rt != null)
        {
            _shakeTimer = 0f;
            _rt.anchoredPosition = _basePos;
        }
        else if (_shakeTimer > 0f && _rt != null)
        {
            _shakeTimer -= Time.deltaTime;
            float amp = Mathf.Clamp01(_shakeTimer / 0.35f) * 6f;
            _rt.anchoredPosition = _basePos + new Vector2(
                Mathf.Sin(Time.time * 60f) * amp, 0f);
            if (_shakeTimer <= 0f)
            {
                _rt.anchoredPosition = _basePos;
            }
        }
    }

    void Refresh()
    {
        int ms = _clock != null ? _clock.RemainingMs : 0;
        if (ms < _lastMs && ms > 0) _flowTimer = .55f;
        _lastMs = ms;
        bool warning = CountdownVisualRules.IsWarning(ms);
        if (_countdownText != null)
        {
            _countdownText.text = FormatMs(ms);
            _countdownText.color = warning ? WarningColor : NormalColor;
            _countdownText.transform.localScale = warning && PresentationSettings.MotionEnabled
                ? Vector3.one * (1.025f + 0.025f * Mathf.Sin(Time.time * 8f))
                : Vector3.one;
        }

        // 资源不变：仅将实际毫秒值映射到缓存像素砂堆，超过开局容量仍显示精确时间。
        _ratio = Mathf.Clamp01(ms / (float)SandClock.InitialSandMs);
        if (_glassTop != null && _glassBottom != null)
        {
            _glassTop.sprite = PixelHourglassSprites.Sand(true, _ratio);
            _glassBottom.sprite = PixelHourglassSprites.Sand(false, 1f - _ratio);
            _glassTop.color = warning ? new Color(1f, .57f, .34f) : Color.white;
            _glassBottom.color = warning ? new Color(.93f, .54f, .33f) : new Color(.85f, .88f, .92f);
        }
    }

    void TickFlow(bool motion)
    {
        bool flowing = motion && _flowTimer > 0f && _ratio > 0f && Time.timeScale > 0f;
        _flowTimer = Mathf.Max(0f, _flowTimer - Time.deltaTime);
        if (flowing) _flowAge += Time.deltaTime;
        if (_frame == null) return;
        Rect rect = _frame.rectTransform.rect;
        float pixel = Mathf.Min(rect.width / PixelHourglassSprites.Width, rect.height / PixelHourglassSprites.Height);
        float landing = Mathf.Min(29f, 14f + (1f - _ratio) * 18f);
        for (int i = 0; i < _grains.Length; i++)
        {
            Image grain = _grains[i];
            if (grain == null) continue;
            grain.enabled = flowing;
            if (!flowing) continue;
            float phase = Mathf.Repeat(_flowAge * 1.7f + i / (float)_grains.Length, 1f);
            grain.rectTransform.anchoredPosition = new Vector2((i % 3 - 1) * pixel,
                Mathf.Round(Mathf.Lerp(33f, landing, phase) - 32f) * pixel);
            grain.rectTransform.sizeDelta = new Vector2(pixel, pixel * (i % 2 == 0 ? 2f : 1f));
            grain.color = CountdownVisualRules.IsWarning(_lastMs) ? new Color(1f, .60f, .36f) : Color.white;
        }
    }

    /// <summary>毫秒 → "mm:ss.mmm"。</summary>
    public static string FormatMs(int ms)
    {
        if (ms < 0)
        {
            ms = 0;
        }

        int totalSeconds = ms / 1000;
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        int millis = ms % 1000;
        return $"{minutes:00}:{seconds:00}.{millis:000}";
    }

    void OnPenalty(int penaltyMs)
    {
        ShowFloat($"-{penaltyMs / 1000f:0.0}s", WarningColor);
        _shakeTimer = 0.35f;
    }

    void OnSandGained(int gainMs)
    {
        // 小额击杀补沙太频繁，只提示 1 秒以上的补沙（清波奖等）
        if (gainMs < 1000)
        {
            return;
        }

        ShowFloat($"+{gainMs / 1000f:0.0}s", GainColor);
    }

    void ShowFloat(string content, Color color)
    {
        if (_floatText == null)
        {
            return;
        }

        _floatText.gameObject.SetActive(true);
        _floatText.text = content;
        color.a = 1f;
        _floatText.color = color;
        _floatTimer = 1.1f;
    }
}
