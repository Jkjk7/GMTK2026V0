using UnityEngine;

/// <summary>
/// 单个棋盘格的显示状态（逻辑占用仍由 GridBoard 管理）。
/// </summary>
public sealed class GridCellView : MonoBehaviour
{
    [SerializeField] SpriteRenderer baseRenderer;

    Color _normalColor = new Color(0.18f, 0.2f, 0.24f, 1f);

    public void Bind(SpriteRenderer renderer)
    {
        baseRenderer = renderer;
        if (baseRenderer != null)
        {
            _normalColor = baseRenderer.color;
        }
    }

    public void SetNormal()
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = _normalColor;
        }
    }

    public void SetHovered()
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = Color.Lerp(_normalColor, Color.white, 0.25f);
        }
    }

    public void SetValid()
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = new Color(0.25f, 0.55f, 0.35f, 1f);
        }
    }

    public void SetInvalid()
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = new Color(0.55f, 0.22f, 0.22f, 1f);
        }
    }

    public void SetOccupied()
    {
        if (baseRenderer != null)
        {
            baseRenderer.color = Color.Lerp(_normalColor, Color.black, 0.2f);
        }
    }
}
