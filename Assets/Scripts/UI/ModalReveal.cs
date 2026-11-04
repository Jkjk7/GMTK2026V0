using UnityEngine;
using UnityEngine.UI;

/// <summary>A short presentation-only spring. The caller owns visibility, alpha and input.</summary>
[DisallowMultipleComponent]
public sealed class ModalReveal : MonoBehaviour
{
    const float Duration = 0.34f;
    RectTransform _panel;
    Image _glint;
    Vector3 _position, _scale;
    Quaternion _rotation;
    float _age;
    bool _playing, _hasPose, _magical;

    public static ModalReveal Play(RectTransform panel, bool magical = false)
    {
        if (panel == null) return null;
        var reveal = panel.GetComponent<ModalReveal>();
        if (reveal == null) reveal = panel.gameObject.AddComponent<ModalReveal>();
        reveal.Begin(panel, magical);
        return reveal;
    }

    public static void Cancel(RectTransform panel)
    {
        if (panel == null) return;
        var reveal = panel.GetComponent<ModalReveal>();
        if (reveal != null) reveal.Stop();
    }

    void Begin(RectTransform panel, bool magical)
    {
        // Restore an interrupted spring before capturing the new layout's baseline.
        if (_playing) RestorePose();
        _panel = panel;
        _position = panel.anchoredPosition3D;
        _scale = panel.localScale;
        _rotation = panel.localRotation;
        _hasPose = true;
        _magical = magical;
        _age = 0;
        _playing = true;
        if (_glint == null)
        {
            var child = new GameObject("ModalEdgeGlint", typeof(RectTransform), typeof(Image));
            child.transform.SetParent(panel, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(-2, -2);
            rect.offsetMax = new Vector2(2, 2);
            _glint = child.GetComponent<Image>();
            _glint.sprite = PixelUiSprites.Frame;
            _glint.type = Image.Type.Sliced;
            _glint.pixelsPerUnitMultiplier = 2;
            _glint.raycastTarget = false;
        }
        _glint.gameObject.SetActive(true);
        enabled = true;
        Tick(0);
    }

    void Update() => Tick(Time.unscaledDeltaTime);

    void Tick(float unscaledDelta)
    {
        if (!_playing || _panel == null) return;
        _age += Mathf.Max(0, unscaledDelta);
        float t = Mathf.Clamp01(_age / Duration);
        if (PresentationSettings.MotionEnabled)
        {
            float spring = Spring(t);
            _panel.localScale = Vector3.Scale(_scale, new Vector3(Mathf.LerpUnclamped(0.93f, 1, spring), Mathf.LerpUnclamped(0.93f, 1, spring), 1));
            _panel.anchoredPosition3D = _position + Vector3.up * Mathf.Round(Mathf.LerpUnclamped(-12, 0, spring));
        }
        else RestorePose();
        Color color = _magical ? new Color(0.68f, 0.82f, 1) : new Color(1, 0.9f, 0.65f);
        color.a = 0.65f * Mathf.Sin(t * Mathf.PI);
        _glint.color = color;
        if (t >= 1) Stop();
    }

    // Fast overshoot, then exact rest: no perpetual bounce or hidden coroutine.
    internal static float Spring(float t)
    {
        float x = t - 1;
        return 1 + 2.4f * x * x * x + 1.4f * x * x;
    }

    void Stop()
    {
        RestorePose();
        _playing = false;
        if (_glint != null) _glint.gameObject.SetActive(false);
        enabled = false;
    }

    void RestorePose()
    {
        if (!_hasPose || _panel == null) return;
        _panel.anchoredPosition3D = _position;
        _panel.localScale = _scale;
        _panel.localRotation = _rotation;
    }

    void OnDisable()
    {
        if (_playing) RestorePose();
        _playing = false;
        if (_glint != null) _glint.gameObject.SetActive(false);
    }
}
