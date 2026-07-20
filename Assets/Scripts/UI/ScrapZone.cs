using UnityEngine;

/// <summary>
/// 棋盘外框左下角分解区：库存或战场模块拖入，确认后返还。
/// </summary>
public class ScrapZone : MonoBehaviour
{
    [SerializeField] float halfWidth = 0.85f;
    [SerializeField] float halfHeight = 0.85f;

    TextMesh _label;
    SpriteRenderer _bg;
    bool _highlighted;
    ModuleCardData _previewCard;
    bool _hasPreview;

    public void Initialize(Vector3 worldCenter)
    {
        transform.position = worldCenter;
        EnsureVisual();
        SetIdle();
    }

    public bool ContainsWorldPoint(Vector3 world)
    {
        Vector3 local = world - transform.position;
        return Mathf.Abs(local.x) <= halfWidth && Mathf.Abs(local.y) <= halfHeight;
    }

    public void SetIdle()
    {
        _highlighted = false;
        _hasPreview = false;
        EnsureVisual();
        if (_bg != null)
        {
            _bg.color = new Color(0.22f, 0.14f, 0.1f, 0.55f);
        }

        if (_label != null)
        {
            _label.text = "分解\n返还部分金币";
            _label.color = new Color(0.7f, 0.6f, 0.4f, 0.85f);
        }
    }

    public void ShowHandPreview(ModuleCardData card)
    {
        _hasPreview = true;
        _previewCard = card;
        _highlighted = true;
        EnsureVisual();
        if (_bg != null)
        {
            _bg.color = new Color(0.55f, 0.35f, 0.12f, 0.75f);
        }

        if (_label != null)
        {
            _label.text = $"分解「{ModuleCatalog.GetDisplayName(card)}」\n返还 {card.ScrapRefund} 金币";
            _label.color = new Color(1f, 0.9f, 0.4f, 1f);
        }
    }

    public bool TryGetPreview(out ModuleCardData card)
    {
        card = _previewCard;
        return _hasPreview;
    }

    public bool TryScrap(ModuleCardData card, Vector3 flyFrom)
    {
        int refund = card.ScrapRefund;
        if (GoldDropService.Instance != null)
        {
            GoldDropService.Instance.GrantGoldWithFly(Mathf.Max(0, refund), flyFrom);
        }
        else if (Economy.Instance != null && refund > 0)
        {
            Economy.Instance.AddGold(refund);
        }

        SetIdle();
        return true;
    }

    void EnsureVisual()
    {
        if (_bg == null)
        {
            _bg = gameObject.AddComponent<SpriteRenderer>();
            _bg.sprite = PrototypeSprites.Square;
            _bg.sortingOrder = -1;
            transform.localScale = new Vector3(halfWidth * 2f, halfHeight * 2f, 1f);
        }

        if (_label == null)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = Vector3.zero;
            labelGo.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 28;
            _label.characterSize = 0.45f;
            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 5;
            }
        }
    }
}
