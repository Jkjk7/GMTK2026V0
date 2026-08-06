using System.Collections;
using UnityEngine;

/// <summary>
/// 核裂变：吸收能量≥5 后，0.5s 内依次射出 5 颗默认球（能1/寿12/速8）。
/// </summary>
public class FissionModule : PathEffectModule
{
    public const int EnergyThreshold = 5;
    public const int BurstCount = 5;
    public const float BurstDuration = 0.5f;
    public const float SpawnSpeed = 8f;
    public const float SpawnLife = 12f;
    public const float SpawnEnergy = 1f;

    [SerializeField] int storedEnergy;
    bool _bursting;
    SpriteRenderer _hudFill;

    public override ModuleType ModuleType => global::ModuleType.Fission;
    public int StoredEnergy => storedEnergy;

    protected override PathShape GetDefaultShape() => PathShape.Straight;

    protected override Color GetBodyTint() => new Color(1f, 0.55f, 0.2f, 1f);

    public void ClearEnergy()
    {
        StopAllCoroutines();
        _bursting = false;
        storedEnergy = 0;
        ClearEnergyResidue();
        RefreshVisual();
    }

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null)
        {
            return false;
        }

        if (_bursting)
        {
            return true;
        }

        // 裂变产物只穿行，不可再被核裂变吃掉（防无限）
        if (ball.IgnoreFissionAbsorb)
        {
            return true;
        }

        storedEnergy = AbsorbBallEnergy(ball, storedEnergy, 9999);
        ball.Despawn();
        RefreshVisual();

        if (storedEnergy >= EnergyThreshold)
        {
            storedEnergy = 0;
            ClearEnergyResidue();
            RefreshVisual();
            StartCoroutine(BurstRoutine(defaultExit));
        }

        return false;
    }

    IEnumerator BurstRoutine(GridDirection exitDir)
    {
        _bursting = true;
        EnergyBallManager mgr = FindBallManager();
        float interval = BurstDuration / BurstCount;
        for (int i = 0; i < BurstCount; i++)
        {
            if (Board != null && mgr != null)
            {
                Vector3 center = Board.CellToWorld(Cell);
                EnergyBall spawned = mgr.TrySpawnBall(center, exitDir, SpawnSpeed, SpawnLife, SpawnEnergy);
                if (spawned != null)
                {
                    spawned.MarkIgnoreFissionAbsorb();
                    spawned.MarkCellTriggered(Cell);
                    spawned.NudgeAlongDirection(0.51f);
                }
            }

            if (i < BurstCount - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }

        _bursting = false;
    }

    public override void RefreshVisual()
    {
        base.RefreshVisual();
        EnsureHud();
        if (_hudFill != null)
        {
            float t = EnergyThreshold > 0 ? Mathf.Clamp01(storedEnergy / (float)EnergyThreshold) : 0f;
            _hudFill.transform.localScale = new Vector3(t, 1f, 1f);
        }
    }

    void EnsureHud()
    {
        if (_hudFill != null)
        {
            return;
        }

        var bg = new GameObject("FissionHud");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        bg.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = PrototypeSprites.Square;
        bgSr.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        bgSr.sortingOrder = 11;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(1f, 0.6f, 0.2f, 1f);
        _hudFill.sortingOrder = 12;
    }
}
