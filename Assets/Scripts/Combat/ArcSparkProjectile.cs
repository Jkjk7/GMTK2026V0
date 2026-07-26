using UnityEngine;

/// <summary>
/// Noita 风格弧线火花/雪花弹：额定角内随机初速，再弯向目标。
/// </summary>
public class ArcSparkProjectile : MonoBehaviour
{
    public enum Style
    {
        Ember,
        Snowflake
    }

    Enemy _target;
    ModuleBase _source;
    CombatDamage.HitEffects _fx;
    Vector3 _velocity;
    int _damage;
    float _speed = 11f;
    float _homing = 7.5f;
    float _life = 1.6f;
    float _hitRadius = 0.3f;
    bool _hit;
    SpriteRenderer _core;
    SpriteRenderer _glow;
    TrailRenderer _trail;
    Style _style;

    static Material s_trailMat;

    public static ArcSparkProjectile Spawn(
        Vector3 from,
        Enemy target,
        ModuleBase source,
        int damage,
        CombatDamage.HitEffects fx,
        Style style,
        float spreadDegrees = 55f)
    {
        var go = new GameObject(style == Style.Ember ? "EmberSpark" : "SnowflakeBolt");
        var bolt = go.AddComponent<ArcSparkProjectile>();
        bolt.Launch(from, target, source, damage, fx, style, spreadDegrees);
        return bolt;
    }

    void Launch(
        Vector3 from,
        Enemy target,
        ModuleBase source,
        int damage,
        CombatDamage.HitEffects fx,
        Style style,
        float spreadDegrees)
    {
        _target = target;
        _source = source;
        _damage = damage;
        _fx = fx;
        _style = style;
        _hit = false;
        _life = 1.6f;

        from.z = 0f;
        transform.position = from;

        Vector3 aim = target != null ? target.transform.position : from + Vector3.right;
        aim.z = 0f;
        Vector3 toTarget = aim - from;
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            toTarget = Vector3.right;
        }

        float half = Mathf.Max(5f, spreadDegrees) * 0.5f;
        float angle = Random.Range(-half, half);
        // 轻偏弧即可，避免强制大偏角导致高速冲过头
        if (Mathf.Abs(angle) < 8f)
        {
            angle += Random.value < 0.5f ? -10f : 10f;
        }

        Vector3 dir = (Quaternion.Euler(0f, 0f, angle) * toTarget.normalized).normalized;
        float dist = toTarget.magnitude;
        _speed = Mathf.Lerp(9f, 14f, Mathf.Clamp01(dist / 8f)) + Random.Range(-0.8f, 1.2f);
        _homing = Mathf.Lerp(5.5f, 9f, Mathf.Clamp01(dist / 10f));
        if (style == Style.Ember)
        {
            // 速度×2 时转向同步放大，避免冲过头绕圈
            _speed *= 2f;
            _homing *= 2.4f;
            _hitRadius = 0.45f;
        }
        else
        {
            _hitRadius = 0.32f;
        }

        _velocity = dir * (_speed * Random.Range(0.85f, 1.15f));

        EnsureVisual();
        ApplyPalette();
    }

    void Update()
    {
        if (_hit)
        {
            return;
        }

        _life -= Time.deltaTime;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 pos = transform.position;
        pos.z = 0f;

        if (_target == null || !_target.IsAlive)
        {
            _target = FindNearestAliveEnemy(pos);
        }

        if (_target != null)
        {
            Vector3 to = _target.transform.position - pos;
            to.z = 0f;
            float dist = to.magnitude;

            // 已进入打击圈：直接命中锁定目标，避免步进穿模
            if (dist <= _hitRadius)
            {
                Impact(_target);
                return;
            }

            if (dist > 0.001f)
            {
                Vector3 desired = to / dist * _speed;
                float turn = Mathf.Clamp01(_homing * Time.deltaTime);
                float nearBoost = Mathf.Lerp(0.7f, 2.4f, 1f - Mathf.Clamp01(dist / 4f));
                turn = Mathf.Clamp01(turn * nearBoost);
                _velocity = Vector3.Lerp(_velocity, desired, turn);
                if (_velocity.sqrMagnitude > 0.0001f)
                {
                    _velocity = _velocity.normalized * _speed;
                }
            }
        }
        else
        {
            // 目标丢失：沿当前速度直线飞完寿命
            _velocity *= 0.985f;
        }

        Vector3 prev = pos;
        pos += _velocity * Time.deltaTime;
        transform.position = pos;

        if (_velocity.sqrMagnitude > 0.0001f)
        {
            float ang = Mathf.Atan2(_velocity.y, _velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        // 线段扫掠碰撞：高速步进也不会漏判；优先锁定目标
        Enemy contact = FindContactAlongSegment(prev, pos);
        if (contact != null)
        {
            Impact(contact);
            return;
        }

        PulseVisual();
    }

    Enemy FindNearestAliveEnemy(Vector3 origin)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        Enemy best = null;
        float bestDistanceSq = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy == null || !enemy.IsAlive)
            {
                continue;
            }

            Vector3 delta = enemy.transform.position - origin;
            delta.z = 0f;
            float distanceSq = delta.sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                best = enemy;
            }
        }

        return best;
    }

    Enemy FindContactAlongSegment(Vector3 from, Vector3 to)
    {
        if (_target != null && _target.IsAlive)
        {
            float d = DistancePointToSegment(_target.transform.position, from, to);
            if (d <= _hitRadius)
            {
                return _target;
            }
        }

        float best = float.PositiveInfinity;
        Enemy bestEnemy = null;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive || e == _target)
            {
                continue;
            }

            float d = DistancePointToSegment(e.transform.position, from, to);
            if (d <= _hitRadius && d < best)
            {
                best = d;
                bestEnemy = e;
            }
        }

        return bestEnemy;
    }

    static float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        point.z = 0f;
        a.z = 0f;
        b.z = 0f;
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 0.0000001f)
        {
            return (point - a).magnitude;
        }

        float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / len2);
        Vector3 closest = a + ab * t;
        return (point - closest).magnitude;
    }

    void Impact(Enemy victim)
    {
        if (_hit)
        {
            return;
        }

        _hit = true;
        if (victim != null && victim.IsAlive)
        {
            CombatDamage.Apply(_source, victim, _damage, _fx);
        }

        SpawnPop();
        Destroy(gameObject);
    }

    void SpawnPop()
    {
        var go = new GameObject("SparkPop");
        go.transform.position = transform.position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Circle;
        sr.sortingOrder = 24;
        Color c = CoreColor(_style);
        c.a = 0.75f;
        sr.color = c;
        float s = _style == Style.Ember ? 0.22f : 0.2f;
        go.transform.localScale = Vector3.one * s;
        Destroy(go, 0.08f);
    }

    // 在类最上方定义私有字段，全局只初始化1次随机偏移
    private float _randomPhase;

    // 物体生成时初始化随机值（仅运行一遍）
    private void Awake()
    {
        // 0~1000随机浮点数，用来错开正弦震动相位
        _randomPhase = Random.Range(0f, 1000f);
    }

    void PulseVisual()
    {
        if (_core == null)
        {
            return;
        }

        // 替换原先 GetEntityId() 部分
        float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * 28f + _randomPhase);
        float core = _style == Style.Ember ? 0.14f : 0.13f;
        _core.transform.localScale = Vector3.one * (core * pulse);
        if (_glow != null)
        {
            _glow.transform.localScale = Vector3.one * (core * 2.2f * pulse);
        }
    }

    void EnsureVisual()
    {
        if (_core != null)
        {
            return;
        }

        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(transform, false);
        _glow = glowGo.AddComponent<SpriteRenderer>();
        _glow.sprite = PrototypeSprites.Circle;
        _glow.sortingOrder = 22;

        var coreGo = new GameObject("Core");
        coreGo.transform.SetParent(transform, false);
        _core = coreGo.AddComponent<SpriteRenderer>();
        _core.sprite = PrototypeSprites.Circle;
        _core.sortingOrder = 23;

        _trail = gameObject.AddComponent<TrailRenderer>();
        _trail.time = 0.22f;
        _trail.minVertexDistance = 0.02f;
        _trail.widthMultiplier = 1f;
        _trail.numCapVertices = 4;
        _trail.numCornerVertices = 2;
        _trail.sortingOrder = 21;
        _trail.emitting = true;
        if (s_trailMat == null)
        {
            s_trailMat = new Material(Shader.Find("Sprites/Default"));
        }

        _trail.material = s_trailMat;
        _trail.textureMode = LineTextureMode.Stretch;
        var curve = new AnimationCurve();
        curve.AddKey(0f, 0.1f);
        curve.AddKey(0.35f, 0.06f);
        curve.AddKey(1f, 0f);
        _trail.widthCurve = curve;
    }

    void ApplyPalette()
    {
        Color core = CoreColor(_style);
        Color glow = GlowColor(_style);

        if (_core != null)
        {
            _core.color = core;
            _core.transform.localScale = Vector3.one * (_style == Style.Ember ? 0.14f : 0.13f);
        }

        if (_glow != null)
        {
            glow.a = 0.45f;
            _glow.color = glow;
            _glow.transform.localScale = Vector3.one * (_style == Style.Ember ? 0.32f : 0.3f);
        }

        if (_trail != null)
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(core, 0f),
                    new GradientColorKey(glow, 0.55f),
                    new GradientColorKey(glow, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            _trail.colorGradient = g;
            _trail.time = _style == Style.Ember ? 0.24f : 0.28f;
        }
    }

    static Color CoreColor(Style style)
    {
        return style == Style.Ember
            ? new Color(1f, 0.55f, 0.12f, 1f)
            : new Color(0.82f, 0.95f, 1f, 1f);
    }

    static Color GlowColor(Style style)
    {
        return style == Style.Ember
            ? new Color(1f, 0.25f, 0.05f, 1f)
            : new Color(0.45f, 0.78f, 1f, 1f);
    }
}
