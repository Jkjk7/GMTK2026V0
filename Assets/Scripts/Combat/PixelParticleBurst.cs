using UnityEngine;

/// <summary>Analytic pixel fragments and sampled trails. No physics, global RNG, materials or gameplay callbacks.</summary>
public sealed class PixelParticleBurst : MonoBehaviour
{
    public enum Pattern { Impact, Explosion, Disintegrate, Implode }
    const int FragmentLimit = 12;
    const int TrailLimit = 8;
    const float PixelGrid = 1f / 96f;
    readonly SpriteRenderer[] _pieces = new SpriteRenderer[FragmentLimit + TrailLimit];
    readonly Vector2[] _velocities = new Vector2[FragmentLimit];
    readonly Vector2[] _origins = new Vector2[FragmentLimit];
    readonly float[] _lifetimes = new float[FragmentLimit];
    readonly float[] _sizes = new float[FragmentLimit];
    readonly float[] _angles = new float[FragmentLimit];
    SpriteRenderer _outerRing, _innerRing, _flash;
    Color _color;
    Pattern _pattern;
    int _count;
    float _age, _size, _intensity;
    bool _leased;
    uint _random;
    internal CombatAccentPool Owner { get; set; }
    public float LifetimeSeconds { get; private set; }

    internal void Initialize(Color color, float size, Pattern pattern, float intensity, uint seed)
    {
        EnsureRenderers();
        _random = seed == 0 ? 1u : seed;
        _color = color;
        _pattern = pattern;
        _size = Mathf.Clamp(size, .25f, 3.5f);
        _intensity = Mathf.Clamp01(intensity);
        _age = 0f;
        _leased = true;
        enabled = true;
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
        LifetimeSeconds = pattern == Pattern.Disintegrate ? .78f : .68f;
        int desired = pattern == Pattern.Impact ? 6 : FragmentLimit;
        _count = Mathf.Clamp(Mathf.RoundToInt(desired * Mathf.Lerp(.3f, 1f, _intensity)), 2, FragmentLimit);
        float turn = Next() * Mathf.PI * 2f;
        for (int i = 0; i < FragmentLimit; i++)
        {
            float angle = turn + (i + Next() * .55f) * Mathf.PI * 2f / _count;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            _velocities[i] = direction * (_size * Mathf.Lerp(1.15f, 3.3f, Next()));
            _origins[i] = pattern == Pattern.Disintegrate
                ? new Vector2((i % 4 - 1.5f) * .09f, (i / 4 - 1f) * .09f) : Vector2.zero;
            if (pattern == Pattern.Implode)
            {
                _origins[i] = direction * _size * .65f;
                _velocities[i] = -direction * _size * 2.2f;
            }
            _lifetimes[i] = Mathf.Lerp(.28f, LifetimeSeconds - .06f, Next());
            _sizes[i] = Mathf.Lerp(.045f, .105f, Next()) * Mathf.Clamp(_size, .65f, 1.5f);
            _angles[i] = Next() * 360f;
            _pieces[i].sprite = pattern == Pattern.Disintegrate || i % 3 == 0
                ? PixelVfxSprites.Chip : PixelVfxSprites.Pixel;
        }
        Draw();
    }

    float Next()
    {
        _random ^= _random << 13;
        _random ^= _random >> 17;
        _random ^= _random << 5;
        return (_random & 0x00ffffff) / 16777216f;
    }

    void EnsureRenderers()
    {
        if (_flash != null) return;
        for (int i = 0; i < _pieces.Length; i++)
            _pieces[i] = Create(i < FragmentLimit ? "PixelFragment" : "FragmentTrail", PixelVfxSprites.Pixel, 25);
        _outerRing = Create("OuterShockwave", PixelVfxSprites.Ring, 24);
        _innerRing = Create("InnerShockwave", PixelVfxSprites.Ring, 24);
        _flash = Create("ImpactCross", PixelVfxSprites.Burst, 27);
    }

    SpriteRenderer Create(string name, Sprite sprite, int order)
    {
        var child = new GameObject(name);
        child.transform.SetParent(transform, false);
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        renderer.enabled = false;
        return renderer;
    }

    void Update() => Advance(Time.deltaTime);

    void Advance(float delta)
    {
        if (!_leased) return;
        _age += Mathf.Max(0f, delta);
        if (_age >= LifetimeSeconds) { Finish(); return; }
        Draw();
    }

    Vector3 Position(int index, float age)
    {
        // Exponential drag keeps the effect near its hit point, while the death chips fall away.
        float travel = (1f - Mathf.Exp(-age * 5f)) / 5f;
        Vector2 offset = _origins[index] + _velocities[index] * travel;
        if (_pattern == Pattern.Disintegrate) offset.y -= age * age * 1.8f;
        return new Vector3(Mathf.Round(offset.x / PixelGrid) * PixelGrid,
            Mathf.Round(offset.y / PixelGrid) * PixelGrid, 0f);
    }

    void Draw()
    {
        for (int i = 0; i < FragmentLimit; i++)
        {
            bool visible = i < _count && _age < _lifetimes[i];
            SpriteRenderer piece = _pieces[i];
            piece.enabled = visible;
            if (visible)
            {
                float t = _age / _lifetimes[i];
                Color tint = Color.Lerp(new Color(1f, .94f, .75f), _color, Mathf.Clamp01(t * 3f + .2f));
                if (_pattern == Pattern.Disintegrate) tint = Color.Lerp(_color, _color * .35f, t);
                tint.a = _color.a * (1f - t * t) * Mathf.Lerp(.65f, 1f, _intensity);
                piece.color = tint;
                piece.transform.localPosition = Position(i, _age);
                piece.transform.localRotation = Quaternion.Euler(0f, 0f, _angles[i] + _age * (i % 2 == 0 ? 260f : -210f));
                float width = _sizes[i] * Mathf.Lerp(1f, .35f, t);
                piece.transform.localScale = new Vector3(width, width * (i % 3 == 0 ? 1.8f : 1f), 1f);
            }
            if (i >= TrailLimit) continue;
            SpriteRenderer trail = _pieces[FragmentLimit + i];
            trail.enabled = visible && _age > .025f && _intensity >= .4f;
            if (!trail.enabled) continue;
            Vector3 head = Position(i, _age);
            Vector3 tail = Position(i, Mathf.Max(0f, _age - .045f));
            Vector3 span = head - tail;
            trail.transform.localPosition = (head + tail) * .5f;
            trail.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(span.y, span.x) * Mathf.Rad2Deg);
            trail.transform.localScale = new Vector3(Mathf.Max(_sizes[i], span.magnitude), _sizes[i] * .46f, 1f);
            Color trailColor = _color;
            trailColor.a *= .42f * (1f - _age / _lifetimes[i]);
            trail.color = trailColor;
        }
        bool disintegrate = _pattern == Pattern.Disintegrate;
        DrawRing(_outerRing, _age, disintegrate ? .27f : .42f, 1f, .63f);
        DrawRing(_innerRing, _age - .055f, .30f, .70f, .45f);
        _flash.enabled = _age < .11f;
        if (_flash.enabled)
        {
            Color flash = Color.Lerp(_color, new Color(1f, .96f, .8f), .5f);
            flash.a = .9f * (1f - _age / .11f);
            _flash.color = flash;
            float diameter = Mathf.Min(.62f, _size * .4f) * (1f + _age * 8f);
            _flash.transform.localScale = Vector3.one * diameter;
        }
    }

    void DrawRing(SpriteRenderer ring, float age, float life, float multiplier, float opacity)
    {
        ring.enabled = age >= 0f && age < life && (ring == _outerRing || _intensity >= .5f);
        if (!ring.enabled) return;
        float t = age / life;
        float diameter = _size * multiplier * Mathf.Lerp(.18f, 1.25f, 1f - (1f - t) * (1f - t));
        if (_pattern == Pattern.Implode) diameter = _size * multiplier * Mathf.Lerp(1.2f, .05f, t);
        ring.transform.localScale = Vector3.one * diameter;
        ring.transform.localRotation = Quaternion.Euler(0f, 0f, ring == _innerRing ? 22.5f : 0f);
        Color tint = _color;
        tint.a *= opacity * (1f - t) * Mathf.Lerp(.55f, 1f, _intensity);
        ring.color = tint;
    }

    internal void Finish()
    {
        if (!_leased) return;
        _leased = false;
        for (int i = 0; i < _pieces.Length; i++) if (_pieces[i] != null) _pieces[i].enabled = false;
        if (_outerRing != null) _outerRing.enabled = false;
        if (_innerRing != null) _innerRing.enabled = false;
        if (_flash != null) _flash.enabled = false;
        if (Owner != null) Owner.Return(this);
        else gameObject.SetActive(false);
    }

    void OnDisable() => Finish();
}
