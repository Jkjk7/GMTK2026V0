using UnityEngine;

/// <summary>Pixel silhouettes and status accents. Only child visuals animate.</summary>
public sealed class EnemyVisualController : MonoBehaviour
{
    Enemy _enemy;
    SpriteRenderer _body;
    SpriteRenderer _statusSource;
    SpriteRenderer _burnEmber;
    SpriteRenderer _chillRim;
    SpriteRenderer _sandGrain;
    Transform _silhouette;
    Transform _sandOrbit;
    int _pose = -1;

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
        // Enemy still owns its status/hit-flash color. Hide its old geometric body only.
        if (_statusSource != null) _statusSource.enabled = false;
        Refresh();
    }

    void Build()
    {
        _silhouette = new GameObject("EnemySilhouette").transform;
        _silhouette.SetParent(transform, false);
        _body = Add("Body", PixelEnemySprites.Get(_enemy.GoldType), Vector3.zero,
            Vector3.one * 1.35f, Color.white, 12, _silhouette);
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
        if (_enemy == null) return;
        Refresh();
        // Step animation at 8fps, keeping world movement/colliders untouched.
        int pose = _enemy.IsFrozen ? 0 : (Mathf.FloorToInt(Time.time * 8f) & 1);
        if (pose != _pose)
        {
            _pose = pose;
            _silhouette.localPosition = new Vector3(0f, pose * (1f / 48f), 0f);
            _burnEmber.transform.localRotation = Quaternion.Euler(0, 0, pose * 90f);
        }
        if (SandVisible)
            _sandOrbit.localRotation = Quaternion.Euler(0, 0, Mathf.Floor(Time.time * 8f) * 12f);
    }

    public void Refresh()
    {
        if (_enemy == null || _body == null) return;
        SetVisible(_burnEmber, _enemy.IsBurning);
        SetVisible(_chillRim, _enemy.IsChilled || _enemy.IsFrozen);
        SetVisible(_sandGrain, _enemy.HasSandBuff);
        if (_statusSource != null)
        {
            Color tint = Color.Lerp(Color.white, _statusSource.color, 0.28f);
            if (_body.color != tint) _body.color = tint;
        }
    }

    static void SetVisible(SpriteRenderer renderer, bool visible)
    {
        if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
    }

    SpriteRenderer Add(string name, Sprite sprite, Vector3 position, Vector3 scale,
        Color color, int order, Transform parent = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent != null ? parent : transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return renderer;
    }
}
