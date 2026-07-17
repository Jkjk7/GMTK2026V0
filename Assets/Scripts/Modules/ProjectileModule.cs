using UnityEngine;

/// <summary>
/// 射弹模块（攻击输出）。
/// 职责：被光球命中时吸收能量；只要有能量，每 0.1 秒消耗 1 点并对最近（最左）敌人造成 5 点伤害。
/// 特效：用 LineRenderer 从本模块拉一条短暂拖尾线到目标。
/// </summary>
public class ProjectileModule : ModuleBase
{
    [Header("Energy")]
    [SerializeField] int energyCapacity = 10;
    [SerializeField] int currentEnergy;

    [Header("Firing")]
    [Tooltip("有能量时的开火间隔（秒）。")]
    [SerializeField] float fireInterval = 0.1f;

    [SerializeField] int damagePerShot = 5;

    [Header("VFX")]
    [SerializeField] float trailVisibleSeconds = 0.08f;

    float _fireTimer;
    float _trailTimer;
    LineRenderer _line;
    SpriteRenderer _body;
    SpriteRenderer _energyBar;

    public override ModuleType ModuleType => global::ModuleType.Projectile;

    /// <summary>当前储能。</summary>
    public int CurrentEnergy => currentEnergy;

    void Awake()
    {
        EnsureVisual();
        EnsureLine();
        RefreshVisual();
    }

    void Update()
    {
        UpdateTrailFade();

        if (currentEnergy <= 0)
        {
            _fireTimer = 0f;
            return;
        }

        _fireTimer += Time.deltaTime;
        while (_fireTimer >= fireInterval && currentEnergy > 0)
        {
            _fireTimer -= fireInterval;
            ConsumeEnergyAndFire();
        }
    }

    /// <summary>
    /// 光球进入：吸收 1 点能量（不超过容量）。
    /// </summary>
    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        int gain = ball.Energy;
        currentEnergy = Mathf.Min(energyCapacity, currentEnergy + gain);
        RefreshVisual();
    }

    /// <summary>
    /// 消耗 1 点能量，向最左侧敌人开火并造成伤害。
    /// </summary>
    void ConsumeEnergyAndFire()
    {
        EnemyTarget target = FindLeftmostEnemy();
        if (target == null)
        {
            // 没有目标时仍消耗能量，避免能量卡死；也可改为不消耗，原型选择消耗。
            currentEnergy = Mathf.Max(0, currentEnergy - 1);
            RefreshVisual();
            return;
        }

        currentEnergy = Mathf.Max(0, currentEnergy - 1);
        target.TakeDamage(damagePerShot);
        ShowTrail(target.transform.position);
        RefreshVisual();
    }

    /// <summary>
    /// 选择世界坐标 X 最小的敌人（“最左边”）。
    /// </summary>
    EnemyTarget FindLeftmostEnemy()
    {
        EnemyTarget[] enemies = FindObjectsOfType<EnemyTarget>();
        if (enemies == null || enemies.Length == 0)
        {
            return null;
        }

        EnemyTarget best = null;
        float bestX = float.PositiveInfinity;
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyTarget e = enemies[i];
            if (e == null || !e.isActiveAndEnabled)
            {
                continue;
            }

            float x = e.transform.position.x;
            if (x < bestX)
            {
                bestX = x;
                best = e;
            }
        }

        return best;
    }

    void ShowTrail(Vector3 targetWorld)
    {
        if (_line == null)
        {
            return;
        }

        _line.enabled = true;
        _line.SetPosition(0, transform.position);
        _line.SetPosition(1, targetWorld);
        _trailTimer = trailVisibleSeconds;
    }

    void UpdateTrailFade()
    {
        if (_line == null || !_line.enabled)
        {
            return;
        }

        _trailTimer -= Time.deltaTime;
        if (_trailTimer <= 0f)
        {
            _line.enabled = false;
        }
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        float fill = energyCapacity > 0 ? (float)currentEnergy / energyCapacity : 0f;
        _energyBar.transform.localScale = new Vector3(0.8f, Mathf.Max(0.05f, fill) * 0.8f, 1f);
        _energyBar.transform.localPosition = new Vector3(0f, -0.55f + fill * 0.4f, 0f);
        _energyBar.color = Color.Lerp(new Color(0.3f, 0.3f, 0.3f), new Color(1f, 0.45f, 0.2f), fill);
    }

    void EnsureVisual()
    {
        if (_body != null)
        {
            return;
        }

        _body = gameObject.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = gameObject.AddComponent<SpriteRenderer>();
        }

        _body.sprite = PrototypeSprites.Square;
        _body.color = new Color(0.9f, 0.35f, 0.25f, 1f);
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.6f;

        var barGo = new GameObject("EnergyBar");
        barGo.transform.SetParent(transform, false);
        _energyBar = barGo.AddComponent<SpriteRenderer>();
        _energyBar.sprite = PrototypeSprites.Square;
        _energyBar.sortingOrder = 9;
    }

    void EnsureLine()
    {
        if (_line != null)
        {
            return;
        }

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.06f;
        _line.endWidth = 0.02f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = new Color(1f, 0.85f, 0.4f, 1f);
        _line.endColor = new Color(1f, 0.4f, 0.2f, 0.2f);
        _line.sortingOrder = 25;
        _line.enabled = false;
        _line.useWorldSpace = true;
    }
}
