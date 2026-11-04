using UnityEngine;

/// <summary>Reusable cosmetic sprite accent; never participates in gameplay.</summary>
public sealed class TransientSpriteVfx : MonoBehaviour
{
    SpriteRenderer _renderer;
    Vector3 _startScale;
    float _age;
    Color _startColor;
    bool _leased;

    internal CombatAccentPool Owner { get; set; }

    public float LifetimeSeconds { get; private set; }

    public void Initialize(Sprite sprite, Color color, float lifetimeSeconds, float size)
    {
        LifetimeSeconds = Mathf.Clamp(lifetimeSeconds, 0.05f, 1f);
        if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = sprite != null ? sprite : PixelVfxSprites.Burst;
        _age = 0f;
        _leased = true;
        enabled = true;
        _startColor = color;
        _renderer.color = color;
        _renderer.enabled = true;
        _renderer.sortingOrder = 26;
        _startScale = Vector3.one * size;
        transform.localScale = _startScale;
        transform.localRotation = Quaternion.identity;
    }

    void Update()
    {
        if (!_leased) return;
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / LifetimeSeconds);
        transform.localScale = _startScale * Mathf.Lerp(0.45f, 1.55f, t);
        transform.Rotate(0f, 0f, 180f * Time.deltaTime);
        if (_renderer != null)
        {
            Color color = _startColor;
            color.a *= 1f - t;
            _renderer.color = color;
        }
        if (_age >= LifetimeSeconds) Finish();
    }

    internal void Finish()
    {
        if (!_leased) return;
        _leased = false;
        if (Owner != null) Owner.Return(this);
        else if (Application.isPlaying) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    void OnDisable() => Finish();
}
