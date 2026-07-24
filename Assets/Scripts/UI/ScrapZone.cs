using UnityEngine;

/// <summary>
/// 分解区：库存或战场模块拖入，确认后返还。
/// 底板可非均匀缩放以匹配金币区尺寸；文字独立均匀缩放，避免压扁。
/// </summary>
public class ScrapZone : MonoBehaviour
{
    [SerializeField] float halfWidth = 0.85f;
    [SerializeField] float halfHeight = 0.85f;

    TextMesh _label;
    Transform _labelRoot;
    SpriteRenderer _bg;
    Transform _bgRoot;
    bool _highlighted;
    ModuleCardData _previewCard;
    bool _hasPreview;

    public void Initialize(Vector3 worldCenter)
    {
        Initialize(worldCenter, halfWidth, halfHeight);
    }

    public void Initialize(Vector3 worldCenter, float halfW, float halfH)
    {
        halfWidth = Mathf.Max(0.05f, halfW);
        halfHeight = Mathf.Max(0.05f, halfH);
        transform.position = worldCenter;
        transform.localScale = Vector3.one;
        EnsureVisual(forceRescale: true);
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

    void EnsureVisual(bool forceRescale = false)
    {
        transform.localScale = Vector3.one;

        if (_bgRoot == null)
        {
            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(transform, false);
            _bgRoot = bgGo.transform;
            _bg = bgGo.AddComponent<SpriteRenderer>();
            _bg.sprite = PrototypeSprites.Square;
            _bg.sortingOrder = -1;
            forceRescale = true;
        }

        if (forceRescale && _bgRoot != null)
        {
            _bgRoot.localPosition = Vector3.zero;
            _bgRoot.localScale = new Vector3(halfWidth * 2f, halfHeight * 2f, 1f);
        }

        if (_labelRoot == null)
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            _labelRoot = labelGo.transform;
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 32;
            _label.characterSize = 0.08f;
            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 5;
            }

            forceRescale = true;
        }

        if (forceRescale && _labelRoot != null)
        {
            // 文字均匀缩放：按较短边适配，避免随底板被压扁
            float uniform = Mathf.Min(halfWidth, halfHeight) * 0.22f;
            uniform = Mathf.Clamp(uniform, 0.06f, 0.2f);
            _labelRoot.localPosition = Vector3.zero;
            _labelRoot.localScale = new Vector3(uniform, uniform, 1f);
        }
    }
}
