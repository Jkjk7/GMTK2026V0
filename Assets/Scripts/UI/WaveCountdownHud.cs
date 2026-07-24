using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 右上角剩余波数：波切换时滑动 −1，每 5 波一段换色。
/// </summary>
public class WaveCountdownHud : MonoBehaviour
{
    Text _label;
    Text _outgoing;
    RectTransform _labelRt;
    RectTransform _outgoingRt;
    WaveManager _waves;
    int _displayed = -1;
    float _animT = 1f;
    const float AnimDuration = 0.35f;
    Vector2 _restPos;
    Color _currentColor = Color.white;

    public void Bind(Text label, Text outgoing, WaveManager waves)
    {
        _label = label;
        _outgoing = outgoing;
        _labelRt = label != null ? label.rectTransform : null;
        _outgoingRt = outgoing != null ? outgoing.rectTransform : null;
        _waves = waves;
        if (_labelRt != null)
        {
            _restPos = _labelRt.anchoredPosition;
        }

        if (_outgoing != null)
        {
            _outgoing.gameObject.SetActive(false);
        }

        if (_waves != null)
        {
            _waves.OnWaveChanged += OnWaveChanged;
            if (_waves.TotalWaves > 0)
            {
                OnWaveChanged(_waves.CurrentWaveDisplay, _waves.TotalWaves);
            }
        }
    }

    void OnDestroy()
    {
        if (_waves != null)
        {
            _waves.OnWaveChanged -= OnWaveChanged;
        }
    }

    void OnWaveChanged(int currentWave, int totalWaves)
    {
        int remaining = Mathf.Max(0, totalWaves - currentWave + 1);
        _currentColor = ColorForWave(currentWave);
        if (_displayed < 0)
        {
            _displayed = remaining;
            ApplyLabel(_label, remaining, _currentColor, 1f);
            return;
        }

        if (remaining == _displayed)
        {
            ApplyLabel(_label, remaining, _currentColor, 1f);
            return;
        }

        // 滑动：旧数字上滑淡出，新数字下滑淡入
        if (_outgoing != null && _outgoingRt != null && _labelRt != null)
        {
            ApplyLabel(_outgoing, _displayed, _outgoing.color, 1f);
            _outgoing.gameObject.SetActive(true);
            _outgoingRt.anchoredPosition = _restPos;
            _labelRt.anchoredPosition = _restPos + new Vector2(0f, -28f);
            ApplyLabel(_label, remaining, _currentColor, 0f);
        }
        else
        {
            ApplyLabel(_label, remaining, _currentColor, 1f);
        }

        _displayed = remaining;
        _animT = 0f;
    }

    void Update()
    {
        if (_animT >= 1f || _labelRt == null)
        {
            return;
        }

        _animT = Mathf.Min(1f, _animT + Time.unscaledDeltaTime / AnimDuration);
        float t = Smooth(_animT);
        _labelRt.anchoredPosition = Vector2.Lerp(_restPos + new Vector2(0f, -28f), _restPos, t);
        SetAlpha(_label, t);

        if (_outgoing != null && _outgoingRt != null)
        {
            _outgoingRt.anchoredPosition = Vector2.Lerp(_restPos, _restPos + new Vector2(0f, 28f), t);
            SetAlpha(_outgoing, 1f - t);
            if (_animT >= 1f)
            {
                _outgoing.gameObject.SetActive(false);
            }
        }
    }

    static float Smooth(float t) => t * t * (3f - 2f * t);

    static void ApplyLabel(Text text, int remaining, Color color, float alpha)
    {
        if (text == null)
        {
            return;
        }

        text.text = remaining.ToString();
        color.a = alpha;
        text.color = color;
    }

    static void SetAlpha(Text text, float a)
    {
        if (text == null)
        {
            return;
        }

        Color c = text.color;
        c.a = a;
        text.color = c;
    }

    static Color ColorForWave(int wave)
    {
        int segment = Mathf.Max(0, (Mathf.Max(1, wave) - 1) / 5);
        switch (segment)
        {
            case 0: return new Color(0.75f, 0.9f, 1f, 1f);
            case 1: return new Color(0.55f, 0.95f, 0.7f, 1f);
            case 2: return new Color(1f, 0.9f, 0.45f, 1f);
            case 3: return new Color(1f, 0.65f, 0.35f, 1f);
            default: return new Color(1f, 0.4f, 0.45f, 1f);
        }
    }
}
