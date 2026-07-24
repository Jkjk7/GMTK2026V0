using UnityEngine;

/// <summary>
/// 黑洞投掷物：中速飞向落点，展开吸引场将敌人拉向中心。
/// </summary>
public class BlackHoleProjectile : MonoBehaviour
{
    Vector3 _target;
    float _speed = 9f;
    float _radius;
    float _duration;
    float _strength;
    bool _arrived;
    float _fieldTimer;
    SpriteRenderer _visual;

    public void Launch(Vector3 from, Vector3 target, float radius, float duration, float strength, float speed = 9f)
    {
        transform.position = from;
        _target = target;
        _target.z = 0f;
        _radius = radius;
        _duration = duration;
        _strength = strength;
        _speed = Mathf.Max(6f, speed);
        _arrived = false;
        _fieldTimer = 0f;
        EnsureVisual(false);
    }

    void Update()
    {
        if (!_arrived)
        {
            Vector3 pos = transform.position;
            pos.z = 0f;
            Vector3 next = Vector3.MoveTowards(pos, _target, _speed * Time.deltaTime);
            transform.position = next;
            if ((next - _target).sqrMagnitude <= 0.0004f)
            {
                BeginField();
            }

            return;
        }

        _fieldTimer += Time.deltaTime;
        PullEnemies();
        PulseVisual();
        if (_fieldTimer >= _duration)
        {
            Destroy(gameObject);
        }
    }

    void BeginField()
    {
        _arrived = true;
        transform.position = _target;
        EnsureVisual(true);
    }

    void PullEnemies()
    {
        if (GameSession.Instance != null && !GameSession.Instance.IsCombatActive)
        {
            return;
        }

        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Vector3 center = _target;
        float eps = 0.35f;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            Vector3 p = e.transform.position;
            float dist = Vector3.Distance(center, p);
            if (dist > _radius || dist < 0.05f)
            {
                continue;
            }

            // 越近吸力越强
            float t = 1f - dist / Mathf.Max(0.01f, _radius);
            float force = _strength * (0.35f + 0.65f * t) / (dist + eps);
            Vector3 dir = (center - p).normalized;
            e.ApplyExternalPull(dir * force * Time.deltaTime);
        }
    }

    void PulseVisual()
    {
        if (_visual == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.06f;
        float scale = _radius * 2f * pulse;
        transform.localScale = Vector3.one * scale;
        Color c = _visual.color;
        c.a = 0.22f + 0.12f * Mathf.Sin(Time.time * 6f);
        _visual.color = c;
    }

    void EnsureVisual(bool field)
    {
        if (_visual == null)
        {
            _visual = gameObject.GetComponent<SpriteRenderer>();
            if (_visual == null)
            {
                _visual = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        _visual.sprite = PrototypeSprites.Circle;
        _visual.sortingOrder = 21;
        if (field)
        {
            _visual.color = new Color(0.35f, 0.1f, 0.55f, 0.28f);
            transform.localScale = Vector3.one * (_radius * 2f);
        }
        else
        {
            _visual.color = new Color(0.15f, 0.05f, 0.25f, 1f);
            transform.localScale = Vector3.one * 0.32f;
        }
    }
}
