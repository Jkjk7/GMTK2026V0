using UnityEngine;

/// <summary>Giant procedural countdown dial behind the board and battle lane.</summary>
public sealed class CountdownRingView : MonoBehaviour
{
    static readonly Color Dark = Hex("#211826");
    static readonly Color Brass = Hex("#B58248");
    static readonly Color Gold = Hex("#FFD676");
    static readonly Color Orange = new Color(1f, 0.38f, 0.08f, 0.9f);
    static readonly Color Red = new Color(0.95f, 0.08f, 0.08f, 0.9f);

    readonly SpriteRenderer[] _ticks = new SpriteRenderer[CountdownVisualRules.TickCount];
    SandClock _clock;

    public int TickRendererCount => _ticks.Length;

    public void Initialize(SandClock clock, Vector3 center, float radius)
    {
        _clock = clock;
        transform.position = new Vector3(center.x, center.y, 0f);
        for (int i = 0; i < _ticks.Length; i++)
        {
            float angle = i * 360f / _ticks.Length;
            var tick = new GameObject($"Tick_{i:00}");
            tick.transform.SetParent(transform, false);
            tick.transform.localPosition =
                Quaternion.Euler(0f, 0f, angle) * (Vector3.up * radius);
            tick.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            tick.transform.localScale = new Vector3(radius * 0.035f, radius * 0.13f, 1f);
            var sr = tick.AddComponent<SpriteRenderer>();
            sr.sprite = PrototypeSprites.Square;
            sr.sortingOrder = -20;
            _ticks[i] = sr;
        }
        Refresh();
    }

    void Update() => Refresh();

    void Refresh()
    {
        int ms = _clock != null ? _clock.RemainingMs : SandClock.InitialSandMs;
        int lit = CountdownVisualRules.GetLitTickCount(ms, SandClock.InitialSandMs);
        bool warning = CountdownVisualRules.IsWarning(ms);
        bool alternate = Mathf.FloorToInt(Time.unscaledTime * 4f) % 2 == 0;
        for (int i = 0; i < _ticks.Length; i++)
        {
            if (_ticks[i] == null) continue;
            _ticks[i].color = i < lit
                ? (warning ? (alternate ? Orange : Red) : (i % 5 == 0 ? Gold : Brass))
                : Dark;
        }
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
