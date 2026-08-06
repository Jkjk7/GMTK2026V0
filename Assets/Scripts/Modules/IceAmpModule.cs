using UnityEngine;

/// <summary>
/// 寒冰增幅：场上被动提高 [寒冷] 造成的减速；多座叠加，总减速上限 70%。
/// </summary>
public class IceAmpModule : ModuleBase
{
    [SerializeField] float slowBonus = 0.05f;
    SpriteRenderer _body;
    TextMesh _levelLabel;
    TextMesh _bonusLabel;

    public override ModuleType ModuleType => global::ModuleType.IceAmp;
    public float SlowBonus => slowBonus;

    public override void ApplyCardData(ModuleCardData data)
    {
        base.ApplyCardData(data);
        ApplyLevelStats(data.Level);
        if (BoundBoard != null)
        {
            RunModifiers.Instance?.RecalcIceAmp();
        }
    }

    public override void BindToCell(GridBoard board, GridCoord cell)
    {
        base.BindToCell(board, cell);
        RunModifiers.Instance?.RecalcIceAmp();
    }

    void ApplyLevelStats(int level)
    {
        slowBonus = ModuleCatalog.GetIceAmpSlowBonus(level);
        EnsureLevelLabel(level);
        EnsureBonusLabel();
        RefreshVisual();
    }

    void OnEnable()
    {
        if (BoundBoard != null)
        {
            RunModifiers.Instance?.RecalcIceAmp();
        }
    }

    void OnDisable()
    {
        RunModifiers.Instance?.RecalcIceAmp();
    }

    void OnDestroy()
    {
        RunModifiers.Instance?.RecalcIceAmp();
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
            _bonusLabel.text = $"+{slowBonus * 100f:0}%";
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
        _bonusLabel.color = new Color(0.7f, 0.95f, 1f, 1f);
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
