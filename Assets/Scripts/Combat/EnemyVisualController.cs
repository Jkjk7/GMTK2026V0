using UnityEngine;

/// <summary>Cached pixel silhouettes and status accents. Only visual children animate.</summary>
public sealed class EnemyVisualController : MonoBehaviour
{
    Enemy _enemy;
    SpriteRenderer _body, _statusSource, _burnEmber, _chillRim, _sandGrain, _shadow, _core;
    Transform _silhouette, _sandOrbit, _chargeRoot;
    float _clock, _phaseOffset, _hitLeft, _chargeSearch;
    int _lastHp;
    bool _caster;
    static Sprite _shadowSprite;
    const float Pixel = 1f / 48f;

    public bool HasClockworkCore => _body != null;
    public bool BurnVisible => _burnEmber != null && _burnEmber.enabled;
    public bool ChillVisible => _chillRim != null && _chillRim.enabled;
    public bool SandVisible => _sandGrain != null && _sandGrain.enabled;

    public void Initialize(Enemy enemy)
    {
        _enemy = enemy;
        _statusSource = enemy.GetComponent<SpriteRenderer>();
        if (_body == null) Build();
        _body.sprite = PixelEnemySprites.Get(enemy.GoldType);
        _body.name = enemy.GoldType + "Marker";
        _caster = enemy.GoldType == EnemyGoldType.Disassembler || enemy.GoldType == EnemyGoldType.ShieldCaster;
        _phaseOffset = (Mathf.Abs(GetInstanceID() % 97) / 97f) * 2f;
        _clock = _hitLeft = _chargeSearch = 0f;
        _lastHp = enemy.CurrentHitPoints;
        _chargeRoot = null;
        _core.color = enemy.GoldType == EnemyGoldType.Disassembler
            ? new Color(0.83f, 0.48f, 1f, 0.32f) : new Color(1f, 0.82f, 0.35f, 0.32f);
        // Enemy still owns status/hit-flash color. Hide only its old geometric body.
        if (_statusSource != null) _statusSource.enabled = false;
        RestPose();
        Refresh();
    }

    void Build()
    {
        _shadow = Add("ContactShadow", ShadowSprite(), new Vector3(0f, -0.43f, 0f),
            new Vector3(1.05f, 0.21f, 1f), new Color(0.025f, 0.035f, 0.055f, 0.38f), 8);
        _silhouette = new GameObject("EnemySilhouette").transform;
        _silhouette.SetParent(transform, false);
        _body = Add("Body", PixelEnemySprites.Get(_enemy.GoldType), Vector3.zero,
            Vector3.one * 1.35f, Color.white, 12, _silhouette);
        _core = Add("ClockworkGlimmer", PixelVfxSprites.Burst, new Vector3(0f, -0.015f, 0f),
            Vector3.one * 0.12f, new Color(1f, 0.82f, 0.35f, 0.32f), 14, _silhouette);
        _chillRim = Add("FrostRim", PixelVfxSprites.Ring, Vector3.zero,
            Vector3.one * 1.3f, new Color(0.45f, 0.9f, 1f, 0.8f), 13);
        _burnEmber = Add("BurnEmber", PixelVfxSprites.Burst, new Vector3(-0.3f, 0.4f, 0f),
            Vector3.one * 0.38f, new Color(1f, 0.5f, 0.13f, 1f), 15);
        _sandOrbit = new GameObject("SandOrbit").transform;
        _sandOrbit.SetParent(transform, false);
        _sandGrain = Add("GoldGrain", PixelVfxSprites.Chip, new Vector3(0.69f, 0f, 0f),
            Vector3.one * 0.17f, new Color(1f, 0.83f, 0.4f, 1f), 15, _sandOrbit);
    }

    void LateUpdate()
    {
        float delta = GameSession.Instance == null || GameSession.Instance.IsCombatActive ? Time.deltaTime : 0f;
        Tick(delta);
    }

    void Tick(float delta)
    {
        if (_enemy == null || _body == null) return;
        delta = Mathf.Max(0f, delta);
        if (_enemy.CurrentHitPoints < _lastHp) _hitLeft = 0.14f;
        _lastHp = _enemy.CurrentHitPoints;
        _hitLeft = Mathf.Max(0f, _hitLeft - delta);
        Refresh();

        bool animate = PresentationSettings.MotionEnabled && !_enemy.IsFrozen && _enemy.IsAlive;
        if (!animate)
        {
            RestPose();
            return;
        }

        _clock += delta;
        // Twelve held poses per second keep the hand-drawn pixel rhythm, independent of frame rate.
        float t = Mathf.Floor((_clock + _phaseOffset) * 12f) / 12f;
        Vector3 position = Vector3.zero, scale = Vector3.one;
        float angle = 0f, step;
        switch (_enemy.GoldType)
        {
            case EnemyGoldType.Swarm:
                step = Mathf.Sin(t * 23f);
                position.y = Mathf.Abs(step) * 0.075f;
                scale.x += step * 0.10f; scale.y -= step * 0.08f;
                angle = step * 4f;
                break;
            case EnemyGoldType.Tank:
                step = Mathf.Sin(t * 5.2f);
                position.y = Mathf.Abs(step) * 0.022f;
                scale.y -= Mathf.Abs(step) * 0.027f;
                angle = step * 1.3f;
                break;
            case EnemyGoldType.Elite:
                step = Mathf.Sin(t * 5f);
                position.y = 0.025f + step * 0.035f;
                scale.x += Mathf.Sin(t * 2.5f) * 0.026f;
                scale.y -= step * 0.022f;
                angle = step * 2.5f;
                break;
            case EnemyGoldType.Boss:
                step = Mathf.Sin(t * 2f);
                position.y = Mathf.Abs(step) * 0.018f;
                scale.x += step * 0.018f; scale.y += step * 0.032f;
                angle = Mathf.Sin(t) * 0.45f;
                break;
            case EnemyGoldType.Disassembler:
                step = Mathf.Sin(t * 3.6f);
                position.y = 0.05f + step * 0.065f;
                scale.y += Mathf.Sin(t * 1.8f) * 0.025f;
                angle = step * 2.1f;
                break;
            case EnemyGoldType.ShieldCaster:
                step = Mathf.Sin(t * 2.8f);
                position.y = 0.035f + step * 0.045f;
                scale.x += Mathf.Sin(t * 1.4f) * 0.025f;
                angle = -step * 1.4f;
                break;
            default:
                step = Mathf.Sin(t * 8f);
                position.y = Mathf.Abs(step) * 0.045f;
                scale.x += Mathf.Abs(step) * 0.025f;
                scale.y -= Mathf.Abs(step) * 0.04f;
                angle = step * 2f;
                break;
        }

        bool charging = ObserveCharge(delta);
        if (charging)
        {
            position.y += 0.045f;
            scale.x *= 0.96f; scale.y *= 1.035f;
            angle *= 0.25f;
        }
        float hit = _hitLeft / 0.14f;
        position.x += hit * 0.09f;
        scale.x *= 1f - hit * 0.1f; scale.y *= 1f + hit * 0.08f;
        position.x = Snap(position.x); position.y = Snap(position.y);
        _silhouette.localPosition = position;
        _silhouette.localScale = scale;
        _silhouette.localRotation = Quaternion.Euler(0f, 0f, angle);
        _shadow.transform.localScale = new Vector3(1.05f - Mathf.Max(0f, position.y) * 0.6f, 0.21f, 1f);

        Color core = _core.color;
        core.a = (charging ? 0.65f : 0.18f) + (0.5f + 0.5f * Mathf.Sin(t * (charging ? 13f : 3f))) * 0.22f;
        _core.color = core;
        _core.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(t * 4f) * 45f);
        _core.transform.localPosition = _caster
            ? new Vector3(Snap(Mathf.Cos(t * 2f) * 0.16f), Snap(0.09f + Mathf.Sin(t * 2f) * 0.14f), 0)
            : new Vector3(0f, -0.015f, 0f);
        if (BurnVisible)
        {
            _burnEmber.transform.localPosition = new Vector3(-0.3f, 0.4f + (Mathf.FloorToInt(t * 8f) & 1) * Pixel, 0f);
            _burnEmber.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(t * 8f) * 90f);
        }
        if (SandVisible) _sandOrbit.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(t * 8f) * 12f);
    }

    bool ObserveCharge(float delta)
    {
        if (!_caster) return false;
        if (_chargeRoot == null)
        {
            _chargeSearch -= delta;
            if (_chargeSearch <= 0f)
            {
                _chargeSearch = 0.2f;
                _chargeRoot = transform.Find(_enemy.GoldType == EnemyGoldType.Disassembler ? "PurpleChargeFx" : "GoldChargeFx");
            }
        }
        return _chargeRoot != null && _chargeRoot.gameObject.activeSelf;
    }

    void RestPose()
    {
        if (_silhouette == null) return;
        _silhouette.localPosition = Vector3.zero;
        _silhouette.localScale = Vector3.one;
        _silhouette.localRotation = Quaternion.identity;
        _shadow.transform.localScale = new Vector3(1.05f, 0.21f, 1f);
        _core.transform.localPosition = new Vector3(0f, -0.015f, 0f);
        _core.transform.localRotation = Quaternion.identity;
        Color core = _core.color; core.a = 0.24f; _core.color = core;
        _burnEmber.transform.localPosition = new Vector3(-0.3f, 0.4f, 0);
        _burnEmber.transform.localRotation = Quaternion.identity;
        _sandOrbit.localRotation = Quaternion.identity;
    }

    void OnDisable() { RestPose(); }

    public void Refresh()
    {
        if (_enemy == null || _body == null) return;
        SetVisible(_burnEmber, _enemy.IsBurning);
        SetVisible(_chillRim, _enemy.IsChilled || _enemy.IsFrozen);
        SetVisible(_sandGrain, _enemy.HasSandBuff);
        Color tint = _statusSource != null ? Color.Lerp(Color.white, _statusSource.color, 0.28f) : Color.white;
        if (_hitLeft > 0f) tint = Color.Lerp(tint, Color.white, _hitLeft / 0.14f);
        if (_body.color != tint) _body.color = tint;
    }

    static float Snap(float value) => Mathf.Round(value / Pixel) * Pixel;
    static void SetVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
    }

    static Sprite ShadowSprite()
    {
        if (_shadowSprite != null) return _shadowSprite;
        const int width = 24, height = 8;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "PixelContactShadow";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int inset = (y == 0 || y == 7) ? 6 : (y == 1 || y == 6) ? 3 : 1;
                pixels[y * width + x] = x >= inset && x < width - inset
                    ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            }
        texture.SetPixels32(pixels); texture.Apply();
        _shadowSprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
        return _shadowSprite;
    }

    SpriteRenderer Add(string name, Sprite sprite, Vector3 position, Vector3 scale,
        Color color, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        return renderer;
    }
}
