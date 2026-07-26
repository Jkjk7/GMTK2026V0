using UnityEngine;

/// <summary>A bounded one-shot sprite accent that expands, rotates, fades, and self-destroys.</summary>
public sealed class TransientSpriteVfx : MonoBehaviour
{
    SpriteRenderer _renderer;
    Vector3 _startScale;
    float _age;

    public float LifetimeSeconds { get; private set; }

    public void Initialize(Sprite sprite, Color color, float lifetimeSeconds, float size)
    {
        LifetimeSeconds = Mathf.Clamp(lifetimeSeconds, 0.05f, 1f);
        _renderer = gameObject.AddComponent<SpriteRenderer>();
        _renderer.sprite = sprite != null ? sprite : PrototypeSprites.Circle;
        _renderer.color = color;
        _renderer.sortingOrder = 40;
        _startScale = Vector3.one * size;
        transform.localScale = _startScale;
        if (Application.isPlaying)
        {
            Destroy(gameObject, LifetimeSeconds);
        }
    }

    void Update()
    {
        _age += Time.deltaTime;
        float t = Mathf.Clamp01(_age / LifetimeSeconds);
        transform.localScale = _startScale * Mathf.Lerp(0.45f, 1.55f, t);
        transform.Rotate(0f, 0f, 180f * Time.deltaTime);
        if (_renderer != null)
        {
            Color color = _renderer.color;
            color.a = 1f - t;
            _renderer.color = color;
        }
    }
}
