using UnityEngine;

/// <summary>
/// 火焰增幅：场上被动提高全局灼烧伤害。
/// </summary>
public class FlameAmpModule : ModuleBase
{
    [SerializeField] int burnBonus = 1;
    SpriteRenderer _body;
    TextMesh _levelLabel;
    TextMesh _bonusLabel;

    public override ModuleType ModuleType => global::ModuleType.FlameAmp;
    public int BurnBonus => burnBonus;

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        ApplyLevelStats(data.Level);
        if (BoundBoard != null)
        {
            RunModifiers.Instance?.RecalcFlameAmp();
        }
    }

    public override void BindToCell(GridBoard board, GridCoord cell)
    {
        base.BindToCell(board, cell);
        RunModifiers.Instance?.RecalcFlameAmp();
    }

    void ApplyLevelStats(int level)
    {
        burnBonus = ModuleCatalog.GetFlameAmpBonus(level);
        EnsureLevelLabel(level);
        EnsureBonusLabel();
        RefreshVisual();
    }

    void OnEnable()
    {
        if (BoundBoard != null)
        {
            RunModifiers.Instance?.RecalcFlameAmp();
        }
    }

    void OnDisable()
    {
        RunModifiers.Instance?.RecalcFlameAmp();
    }

    void OnDestroy()
    {
        RunModifiers.Instance?.RecalcFlameAmp();
    }

    public override void OnBallEnter(EnergyBall ball)
    {
        // 被动模块：不吸收能量
    }

    public override void RefreshVisual()
    {
        EnsureVisual();
        EnsureBonusLabel();
        if (_body != null)
        {
            _body.color = ModuleCatalog.GetDisplayColor(ModuleType);
        }

        if (_bonusLabel != null)
        {
            _bonusLabel.text = $"+{burnBonus}";
        }
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

    void EnsureBonusLabel()
    {
        if (_bonusLabel != null)
        {
            return;
        }

        var go = new GameObject("BonusLabel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, -0.05f, 0f);
        go.transform.localScale = new Vector3(0.07f, 0.07f, 1f);
        _bonusLabel = go.AddComponent<TextMesh>();
        _bonusLabel.anchor = TextAnchor.MiddleCenter;
        _bonusLabel.fontSize = 36;
        _bonusLabel.color = new Color(1f, 0.85f, 0.4f, 1f);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 12;
        }
    }

    void Awake()
    {
        EnsureVisual();
        RefreshVisual();
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
        _body.sortingOrder = 8;
        transform.localScale = Vector3.one * 0.55f;
    }
}
