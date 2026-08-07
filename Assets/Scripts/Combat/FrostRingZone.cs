using UnityEngine;

/// <summary>
/// 冰霜环：持续判定范围内敌人，刷新 2 秒 [寒冷]。
/// </summary>
public class FrostRingZone : MonoBehaviour
{
    float _radius;
    float _life;
    float _tickAcc;
    SpriteRenderer _sr;

    public static FrostRingZone Spawn(Vector3 at, float radius, float duration)
    {
        var go = new GameObject("FrostRing");
        go.transform.position = at;
        var zone = go.AddComponent<FrostRingZone>();
        zone.Init(radius, duration);
        return zone;
    }

    void Init(float radius, float duration)
    {
        _radius = Mathf.Max(0.4f, radius);
        _life = Mathf.Max(0.1f, duration);
        _tickAcc = 0f;
        EnsureVisual();
        ApplyTick();
    }

    void Update()
    {
        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        _tickAcc += Time.deltaTime;
        if (_tickAcc >= 0.2f)
        {
            _tickAcc = 0f;
            ApplyTick();
        }

        if (_sr != null)
        {
            float a = Mathf.Clamp01(_life / 0.5f) * 0.35f + 0.2f;
            Color c = _sr.color;
            c.a = a;
            _sr.color = c;
        }
    }

    void ApplyTick()
    {
        float chillPct = RunModifiers.Instance != null
            ? RunModifiers.Instance.GetEffectiveChillSlowPercent()
            : ModuleCatalog.IceSlowPercent;
        Vector3 origin = transform.position;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            if (Vector3.Distance(origin, e.transform.position) <= _radius)
            {
                e.ApplySlow(chillPct, 2f);
            }
        }
    }

    void EnsureVisual()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = PrototypeSprites.Circle;
        _sr.color = new Color(0.35f, 0.8f, 1f, 0.45f);
        _sr.sortingOrder = 12;
        transform.localScale = Vector3.one * (_radius * 2f);
    }
}
