using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 烈焰墙：持续攻击模块。有能量时在战斗区中线维持火墙；
/// 敌人穿过受一次伤害并灼烧；能量按秒连续消耗，耗尽关闭。
/// 多座各自独立判定与结算。
/// </summary>
public class FlameWallModule : ModuleBase
{
    public const int EnergyCap = 30;
    const float WallHalfWidth = 0.35f;
    const float WallVisualHeight = 2.2f;
    const float WallVisualWidth = 0.22f;

    [SerializeField] float currentEnergy;
    SpriteRenderer _body;
    SpriteRenderer _hudFill;
    TextMesh _levelLabel;
    GameObject _wallVisual;
    SpriteRenderer _wallSr;
    BattleLane _lane;
    readonly HashSet<int> _hitEnemyIds = new HashSet<int>();
    readonly HashSet<int> _aliveEnemyIds = new HashSet<int>();
    readonly Dictionary<int, float> _lastEnemyX = new Dictionary<int, float>(64);
    readonly List<int> _scratchRemove = new List<int>(32);

    public override ModuleType ModuleType => global::ModuleType.FlameWall;
    public int CurrentEnergy => Mathf.FloorToInt(currentEnergy + 1e-4f);
    public int EnergyCapacity => EnergyCap;
    public bool IsWallActive => currentEnergy > 0f && _wallVisual != null && _wallVisual.activeSelf;

    float DrainPerSecond => ModuleCatalog.GetFlameWallEnergyDrainPerSecond(ModuleLevel);

    public void ClearEnergy()
    {
        currentEnergy = 0f;
        ClearEnergyResidue();
        ShutdownWall();
        RefreshVisual();
    }

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        currentEnergy = Mathf.Min(currentEnergy, EnergyCap);
        EnsureLevelLabel(data.Level);
        RefreshVisual();
    }

    void Update()
    {
        if (currentEnergy <= 0f)
        {
            ShutdownWall();
            return;
        }

        float drain = DrainPerSecond * Time.deltaTime;
        currentEnergy = Mathf.Max(0f, currentEnergy - drain);
        if (currentEnergy <= 0f)
        {
            currentEnergy = 0f;
            ClearEnergyResidue();
            ShutdownWall();
            RefreshVisual();
            return;
        }

        EnsureWallActive();
        TickCrossingHits();
        RefreshHudOnly();
    }

    void OnDestroy()
    {
        DestroyWallVisual();
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        if (ball == null)
        {
            return;
        }

        int floor = Mathf.FloorToInt(currentEnergy + 1e-4f);
        float frac = Mathf.Max(0f, currentEnergy - floor);
        int next = AbsorbBallEnergy(ball, floor, EnergyCap);
        currentEnergy = Mathf.Min(EnergyCap, next + frac);
        RefreshVisual();
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureHud();
        if (_body != null)
        {
            _body.sprite = PrototypeSprites.Triangle;
            _body.color = new Color(1f, 0.2f, 0.08f, 1f);
        }

        RefreshHudOnly();
        if (currentEnergy > 0f)
        {
            EnsureWallActive();
        }
        else
        {
            ShutdownWall();
        }
    }

    void RefreshHudOnly()
    {
        if (_hudFill == null)
        {
            return;
        }

        float t = EnergyCap > 0 ? currentEnergy / EnergyCap : 0f;
        _hudFill.transform.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
        _hudFill.color = currentEnergy > 0f
            ? new Color(1f, 0.45f, 0.1f, 1f)
            : new Color(0.45f, 0.45f, 0.45f, 1f);
    }

    void EnsureWallActive()
    {
        if (!TryResolveLane(out BattleLane lane))
        {
            return;
        }

        float midX = (lane.SpawnX + lane.EndX) * 0.5f;
        Vector3 pos = new Vector3(midX, lane.LaneY, 0f);

        if (_wallVisual == null)
        {
            _wallVisual = new GameObject("FlameWallVisual");
            _wallSr = _wallVisual.AddComponent<SpriteRenderer>();
            _wallSr.sprite = PrototypeSprites.Square;
            _wallSr.sortingOrder = 55;
            _wallVisual.transform.localScale = new Vector3(WallVisualWidth, WallVisualHeight, 1f);
        }

        _wallVisual.SetActive(true);
        _wallVisual.transform.position = pos;
        if (_wallSr != null)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 10f);
            _wallSr.color = new Color(1f, 0.35f + 0.2f * pulse, 0.05f, 0.55f + 0.25f * pulse);
        }
    }

    void ShutdownWall()
    {
        _hitEnemyIds.Clear();
        _lastEnemyX.Clear();
        if (_wallVisual != null)
        {
            _wallVisual.SetActive(false);
        }
    }

    void DestroyWallVisual()
    {
        if (_wallVisual != null)
        {
            Destroy(_wallVisual);
            _wallVisual = null;
            _wallSr = null;
        }
    }

    void TickCrossingHits()
    {
        if (!TryResolveLane(out BattleLane lane))
        {
            return;
        }

        float midX = (lane.SpawnX + lane.EndX) * 0.5f;
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        _aliveEnemyIds.Clear();

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            int id = e.GetInstanceID();
            _aliveEnemyIds.Add(id);
            float x = e.transform.position.x;

            if (!_lastEnemyX.TryGetValue(id, out float prevX))
            {
                _lastEnemyX[id] = x;
                if (!_hitEnemyIds.Contains(id) && Mathf.Abs(x - midX) <= WallHalfWidth)
                {
                    ApplyWallHit(e, id);
                }

                continue;
            }

            if (!_hitEnemyIds.Contains(id))
            {
                bool crossedMid = (prevX - midX) * (x - midX) <= 0f && !Mathf.Approximately(prevX, x);
                bool enteredZone = Mathf.Abs(prevX - midX) > WallHalfWidth
                                   && Mathf.Abs(x - midX) <= WallHalfWidth;
                if (crossedMid || enteredZone)
                {
                    ApplyWallHit(e, id);
                }
            }

            _lastEnemyX[id] = x;
        }

        CleanupTracking(_aliveEnemyIds);
    }

    void ApplyWallHit(Enemy e, int id)
    {
        if (e == null || !e.IsAlive || _hitEnemyIds.Contains(id))
        {
            return;
        }

        _hitEnemyIds.Add(id);
        int dmg = ModuleCatalog.GetFlameWallDamage(ModuleLevel);
        float burn = ModuleCatalog.GetFlameWallBurnDuration(ModuleLevel);
        CombatDamage.Apply(this, e, dmg, CombatDamage.HitEffects.Burn(burn));
    }

    void CleanupTracking(HashSet<int> aliveIds)
    {
        _scratchRemove.Clear();
        foreach (int id in _lastEnemyX.Keys)
        {
            if (!aliveIds.Contains(id))
            {
                _scratchRemove.Add(id);
            }
        }

        for (int i = 0; i < _scratchRemove.Count; i++)
        {
            int removeId = _scratchRemove[i];
            _lastEnemyX.Remove(removeId);
            _hitEnemyIds.Remove(removeId);
        }
    }

    bool TryResolveLane(out BattleLane lane)
    {
        if (_lane == null)
        {
            _lane = FindObjectOfType<BattleLane>();
        }

        lane = _lane;
        if (lane != null)
        {
            lane.RefreshFromAnchors();
        }

        return lane != null;
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

        _body.sprite = PrototypeSprites.Triangle;
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.75f;
    }

    void EnsureHud()
    {
        if (_hudFill != null)
        {
            return;
        }

        var bg = new GameObject("EnergyHud");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, -0.65f, 0f);
        bg.transform.localScale = new Vector3(1f, 0.16f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = PrototypeSprites.Square;
        bgSr.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        bgSr.sortingOrder = 11;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(1f, 0.45f, 0.1f, 1f);
        _hudFill.sortingOrder = 12;
    }

    void EnsureLevelLabel(int level)
    {
        if (level <= 1)
        {
            if (_levelLabel != null)
            {
                _levelLabel.gameObject.SetActive(false);
            }

            return;
        }

        if (_levelLabel == null)
        {
            var go = new GameObject("LevelLabel");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            go.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
            _levelLabel = go.AddComponent<TextMesh>();
            _levelLabel.anchor = TextAnchor.MiddleCenter;
            _levelLabel.fontSize = 40;
            _levelLabel.color = Color.white;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 12;
            }
        }

        _levelLabel.gameObject.SetActive(true);
        _levelLabel.text = $"Lv{level}";
    }
}
