using UnityEngine;

/// <summary>
/// 棋盘上方球数 HUD：当前/最大。
/// </summary>
public class BallCountHud : MonoBehaviour
{
    EnergyBallManager _manager;
    TextMesh _label;

    public void Initialize(EnergyBallManager manager, Vector3 worldPosition)
    {
        _manager = manager;
        transform.position = worldPosition;
        EnsureLabel();
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    void Refresh()
    {
        EnsureLabel();
        if (_label == null)
        {
            return;
        }

        int cur = _manager != null ? _manager.ActiveCount : 0;
        int max = _manager != null ? _manager.MaxBalls : 40;
        _label.text = $"{cur}/{max}";
        float t = max > 0 ? cur / (float)max : 0f;
        _label.color = t >= 0.9f
            ? new Color(1f, 0.45f, 0.35f, 1f)
            : new Color(0.85f, 0.95f, 1f, 1f);
    }

    void EnsureLabel()
    {
        if (_label != null)
        {
            return;
        }

        _label = gameObject.GetComponent<TextMesh>();
        if (_label == null)
        {
            _label = gameObject.AddComponent<TextMesh>();
        }

        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
        _label.fontSize = 48;
        _label.characterSize = 0.08f;
        _label.color = new Color(0.85f, 0.95f, 1f, 1f);
        var mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 30;
        }
    }
}
