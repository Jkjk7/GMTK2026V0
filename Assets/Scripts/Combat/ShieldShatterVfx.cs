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

    public void Initialize(float size)
    {
        float s = Mathf.Clamp(size, 0.5f, 3f);
        _shards = new Shard[ShardCount];
        for (int i = 0; i < ShardCount; i++)
        {
            float ang = (i / (float)ShardCount) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            var go = new GameObject($"Shard_{i}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = dir * (0.15f * s);
            float shardScale = Random.Range(0.18f, 0.32f) * s;
            go.transform.localScale = new Vector3(shardScale, shardScale * Random.Range(0.55f, 1f), 1f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = i % 2 == 0 ? PrototypeSprites.Circle : PrototypeSprites.Square;
            sr.color = new Color(1f, 0.9f, 0.45f, 0.55f);
            sr.sortingOrder = 45;

            _shards[i] = new Shard
            {
                Transform = go.transform,
                Renderer = sr,
                Velocity = new Vector3(dir.x, dir.y, 0f) * Random.Range(2.2f, 4.2f) * s,
                Spin = Random.Range(-420f, 420f)
            };
        }

        // 中心半透明爆闪环
        var burst = new GameObject("Burst");
        burst.transform.SetParent(transform, false);
        var burstSr = burst.AddComponent<SpriteRenderer>();
        burstSr.sprite = PrototypeSprites.Circle;
        burstSr.color = new Color(1f, 0.95f, 0.7f, 0.35f);
        burstSr.sortingOrder = 44;
        burst.transform.localScale = Vector3.one * (1.2f * s);
        Destroy(burst, 0.18f);

        Destroy(gameObject, Lifetime + 0.05f);
    }

    void Update()
    {
        if (_shards == null)
        {
            return;
        }

        float dt = Time.deltaTime;
        _age += dt;
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
    }
}
