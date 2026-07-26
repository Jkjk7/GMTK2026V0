using UnityEngine;

/// <summary>Procedural clockwork details and persistent status overlays for an enemy.</summary>
public sealed class EnemyVisualController : MonoBehaviour
{
    static readonly Color Dark = Hex("#211826");
    static readonly Color Brass = Hex("#B58248");
    static readonly Color Gold = Hex("#FFD676");

    Enemy _enemy;
    SpriteRenderer _core;
    Transform _hand;
    SpriteRenderer _burnEmber;
    SpriteRenderer _burnFlame;
    SpriteRenderer _chillRim;
    Transform _sandOrbit;
    SpriteRenderer _sandGrain;

    public bool HasClockworkCore => _core != null && _hand != null;
    public bool BurnVisible => _burnEmber != null && _burnEmber.enabled;
    public bool ChillVisible => _chillRim != null && _chillRim.enabled;
    public bool SandVisible => _sandGrain != null && _sandGrain.enabled;

    public void Initialize(Enemy enemy)
    {
        _enemy = enemy;
        if (_core == null)
        {
            Build();
        }
        Refresh();
    }

    void Build()
    {
        _chillRim = Add("FrostRim", PrototypeSprites.Circle, Vector3.zero,
            Vector3.one * 1.22f, new Color(0.2f, 0.95f, 1f, 0.55f), 9);
        _core = Add("ClockworkCore", PrototypeSprites.Circle, Vector3.zero,
            Vector3.one * 0.58f, Dark, 13);
        _hand = Add("ClockHand", PrototypeSprites.Square, new Vector3(0f, 0.13f, 0f),
            new Vector3(0.07f, 0.34f, 1f), Gold, 14).transform;

        _burnEmber = Add("BurnEmber", PrototypeSprites.Circle, new Vector3(0.23f, 0.22f, 0f),
            Vector3.one * 0.22f, new Color(1f, 0.18f, 0.03f, 0.92f), 15);
        _burnFlame = Add("BurnFlame", PrototypeSprites.Square, new Vector3(-0.18f, 0.28f, 0f),
            new Vector3(0.13f, 0.32f, 1f), new Color(1f, 0.55f, 0.08f, 0.85f), 15);
        _burnFlame.transform.localRotation = Quaternion.Euler(0f, 0f, 35f);

        _sandOrbit = new GameObject("SandOrbit").transform;
        _sandOrbit.SetParent(transform, false);
        _sandGrain = Add("GoldGrain", PrototypeSprites.Circle, new Vector3(0.62f, 0f, 0f),
            Vector3.one * 0.18f, Gold, 15, _sandOrbit);
    }

    void Update()
    {
        if (_hand != null)
        {
            _hand.Rotate(0f, 0f, -120f * Time.deltaTime);
        }
        if (_sandOrbit != null)
        {
            _sandOrbit.Rotate(0f, 0f, 95f * Time.deltaTime);
        }
        if (_burnFlame != null && _burnFlame.enabled)
        {
            float pulse = 0.9f + 0.16f * Mathf.Sin(Time.time * 13f);
            _burnFlame.transform.localScale = new Vector3(0.13f, 0.32f, 1f) * pulse;
        }
        Refresh();
    }

    public void Refresh()
    {
        if (_enemy == null) return;
        bool burning = _enemy.IsBurning;
        if (_burnEmber != null) _burnEmber.enabled = burning;
        if (_burnFlame != null) _burnFlame.enabled = burning;
        if (_chillRim != null) _chillRim.enabled = _enemy.IsChilled;
        if (_sandGrain != null) _sandGrain.enabled = _enemy.HasSandBuff;
    }

    SpriteRenderer Add(
        string name, Sprite sprite, Vector3 position, Vector3 scale,
        Color color, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }
}
