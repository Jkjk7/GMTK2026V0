using UnityEngine;

/// <summary>金盾破碎碎块：向外飞散并淡出。</summary>
public sealed class ShieldShatterVfx : MonoBehaviour
{
    const int ShardCount = 8;
    const float Lifetime = 0.42f;

    struct Shard
    {
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 Velocity;
        public float Spin;
    }

    Shard[] _shards;
    float _age;
    SpriteRenderer _burst;
    bool _leased;

    internal CombatAccentPool Owner { get; set; }

    public void Initialize(float size)
    {
        float s = Mathf.Clamp(size, 0.5f, 3f);
        _age = 0f;
        _leased = true;
        enabled = true;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        EnsureParts();
        for (int i = 0; i < ShardCount; i++)
        {
            // Preserve the original six global RNG draws per shard, even on pool overflow.
            float ang = (i / (float)ShardCount) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            Shard shard = _shards[i];
            shard.Transform.localPosition = dir * (0.15f * s);
            float shardScale = Random.Range(0.18f, 0.32f) * s;
            shard.Transform.localScale = new Vector3(shardScale, shardScale * Random.Range(0.55f, 1f), 1f);
            shard.Transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            shard.Renderer.color = new Color(1f, 0.9f, 0.45f, 0.55f);
            shard.Renderer.enabled = true;
            shard.Velocity = new Vector3(dir.x, dir.y, 0f) * Random.Range(2.2f, 4.2f) * s;
            shard.Spin = Random.Range(-420f, 420f);
            _shards[i] = shard;
        }

        _burst.color = new Color(1f, 0.95f, 0.7f, 0.35f);
        _burst.enabled = true;
        _burst.transform.localScale = Vector3.one * (1.2f * s);
    }

    void EnsureParts()
    {
        if (_shards != null) return;
        _shards = new Shard[ShardCount];
        for (int i = 0; i < ShardCount; i++)
        {
            var go = new GameObject($"Shard_{i}");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = PixelVfxSprites.Chip;
            renderer.sortingOrder = 45;
            _shards[i] = new Shard { Transform = go.transform, Renderer = renderer };
        }

        var burst = new GameObject("Burst");
        burst.transform.SetParent(transform, false);
        _burst = burst.AddComponent<SpriteRenderer>();
        _burst.sprite = PixelVfxSprites.Ring;
        _burst.sortingOrder = 44;
    }

    void Update()
    {
        if (!_leased || _shards == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        _age += dt;
        if (_burst != null && _age >= 0.18f) _burst.enabled = false;
        float t = Mathf.Clamp01(_age / Lifetime);
        float fade = 1f - t;

        for (int i = 0; i < _shards.Length; i++)
        {
            Shard shard = _shards[i];
            if (shard.Transform == null)
            {
                continue;
            }

            shard.Velocity *= 1f - 2.4f * dt;
            shard.Transform.localPosition += shard.Velocity * dt;
            shard.Transform.Rotate(0f, 0f, shard.Spin * dt);
            if (shard.Renderer != null)
            {
                Color c = shard.Renderer.color;
                c.a = 0.55f * fade;
                shard.Renderer.color = c;
            }

            _shards[i] = shard;
        }
        if (_age >= Lifetime + 0.05f) Finish();
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
