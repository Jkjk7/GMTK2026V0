using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 沙漏面板：屏幕左侧占位沙漏图形 + 上方精确到毫秒的倒计时（mm:ss.mmm）。
/// 漏怪罚沙时弹浮动 "-X.XXXs" 文字；剩余低于阈值时倒计时变红。
/// 漏沙/回填粒子由 SandVfxService 驱动。
/// </summary>
public class SandClockPanel : MonoBehaviour
{
    const int WarningThresholdMs = 20_000;

    static readonly Color NormalColor = new Color(0.95f, 0.88f, 0.62f, 1f);
    static readonly Color WarningColor = new Color(1f, 0.4f, 0.35f, 1f);
    static readonly Color GainColor = new Color(0.5f, 0.95f, 0.55f, 1f);

    Text _countdownText;
    Text _floatText;
    Image _glassTop;
    Image _glassBottom;
    SandClock _clock;
    RectTransform _glassBottomRt;

    float _floatTimer;
    float _shakeTimer;
    Vector2 _basePos;
    RectTransform _rt;

    public void Bind(SandClock clock, Text countdownText, Text floatText, Image glassTop, Image glassBottom)
    {
        _clock = clock;
        _countdownText = countdownText;
        _floatText = floatText;
        _glassTop = glassTop;
        _glassBottom = glassBottom;
        _glassBottomRt = glassBottom != null ? glassBottom.rectTransform : null;
        _rt = GetComponent<RectTransform>();
        _basePos = _rt != null ? _rt.anchoredPosition : Vector2.zero;

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
        return UiToWorld(_glassBottomRt != null ? _glassBottomRt : _rt, new Vector2(0.5f, 0.05f));
    }

    /// <summary>沙漏回填落点（世界坐标）。</summary>
    public Vector3 GetWorldFillPosition()
    {
        return UiToWorld(_glassBottomRt != null ? _glassBottomRt : _rt, new Vector2(0.5f, 0.55f));
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

        if (_floatTimer > 0f && _floatText != null)
        {
            _floatTimer -= Time.deltaTime;
            Color c = _floatText.color;
            c.a = Mathf.Clamp01(_floatTimer / 0.4f);
            _floatText.color = c;
            if (_floatTimer <= 0f)
            {
                _floatText.gameObject.SetActive(false);
            }
        }

        if (_shakeTimer > 0f && _rt != null)
        {
            _shakeTimer -= Time.deltaTime;
            float amp = Mathf.Clamp01(_shakeTimer / 0.35f) * 6f;
            _rt.anchoredPosition = _basePos + new Vector2(
                Mathf.Sin(Time.unscaledTime * 60f) * amp, 0f);
            if (_shakeTimer <= 0f)
            {
                _rt.anchoredPosition = _basePos;
            }
        }
    }

    void Refresh()
    {
        if (_countdownText == null)
        {
            return;
        }

        int ms = _clock != null ? _clock.RemainingMs : 0;
        _countdownText.text = FormatMs(ms);
        _countdownText.color = ms <= WarningThresholdMs ? WarningColor : NormalColor;

        // 沙漏上下腔按剩余比例简单填色（相对开局值，可溢出封顶）
        if (_glassTop != null && _glassBottom != null)
        {
            float ratio = Mathf.Clamp01(ms / (float)SandClock.InitialSandMs);
            Color sand = ms <= WarningThresholdMs ? WarningColor : NormalColor;
            _glassTop.color = Color.Lerp(new Color(sand.r, sand.g, sand.b, 0.15f), sand, ratio);
            _glassBottom.color = new Color(sand.r, sand.g, sand.b, 0.35f);
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
