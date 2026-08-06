using UnityEngine;

/// <summary>
/// 中续器：球穿过时汲取能量至 cap；已满则刷新球寿命并清空储能（不吞球）。
/// </summary>
public class RelayModule : PathEffectModule
{
    public const int EnergyCap = 20;

    [SerializeField] int storedEnergy;
    SpriteRenderer _hudFill;

    public override ModuleType ModuleType => global::ModuleType.Relay;
    public int StoredEnergy => storedEnergy;

    protected override PathShape GetDefaultShape() => PathShape.Straight;

    protected override Color GetBodyTint() => new Color(0.35f, 0.9f, 0.75f, 1f);

    public void ClearEnergy()
    {
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

        if (storedEnergy >= EnergyCap)
        {
            ball.RefreshLifetime();
            storedEnergy = 0;
            ClearEnergyResidue();
            RefreshVisual();
            return true;
        }

        storedEnergy = AbsorbBallEnergy(ball, storedEnergy, EnergyCap);
        RefreshVisual();
        return true;
    }

    public override void RefreshVisual()
    {
        base.RefreshVisual();
        EnsureHud();
        if (_hudFill != null)
        {
            float t = EnergyCap > 0 ? storedEnergy / (float)EnergyCap : 0f;
            _hudFill.transform.localScale = new Vector3(Mathf.Clamp01(t), 1f, 1f);
        }
    }

    void EnsureHud()
    {
        if (_hudFill != null)
        {
            return;
        }

        var bg = new GameObject("EnergyHud");
        bg.transform.SetParent(transform, false);
        bg.transform.localPosition = new Vector3(0f, -0.7f, 0f);
        bg.transform.localScale = new Vector3(1.1f, 0.18f, 1f);
        var bgSr = bg.AddComponent<SpriteRenderer>();
        bgSr.sprite = PrototypeSprites.Square;
        bgSr.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        bgSr.sortingOrder = 11;

        var fill = new GameObject("Fill");
        fill.transform.SetParent(bg.transform, false);
        fill.transform.localPosition = Vector3.zero;
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(0.3f, 1f, 0.7f, 1f);
        _hudFill.sortingOrder = 12;
    }
}
