using UnityEngine;

/// <summary>
/// 中续器：吸收能量至 cap 20；储能&gt;0 时下一球刷新寿命并清空储能（不吸收）。
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
        RefreshVisual();
    }

    protected override bool OnValidBallEnter(EnergyBall ball, GridDirection entrySide, GridDirection defaultExit)
    {
        if (ball == null)
        {
            return false;
        }

        if (storedEnergy > 0)
        {
            ball.RefreshLifetime();
            storedEnergy = 0;
            RefreshVisual();
            return true;
        }

        storedEnergy = Mathf.Min(EnergyCap, storedEnergy + ball.Energy);
        ball.Despawn();
        RefreshVisual();
        return false;
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
        fill.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
        fill.transform.localScale = new Vector3(0f, 1f, 1f);
        _hudFill = fill.AddComponent<SpriteRenderer>();
        _hudFill.sprite = PrototypeSprites.Square;
        _hudFill.color = new Color(0.3f, 1f, 0.7f, 1f);
        _hudFill.sortingOrder = 12;
        var anchor = new GameObject("Anchor");
        anchor.transform.SetParent(fill.transform, false);
        // pivot left: scale.x grows to the right from -0.5 local
        fill.transform.localPosition = Vector3.zero;
    }
}
