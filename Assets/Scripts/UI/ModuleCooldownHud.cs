using UnityEngine;

/// <summary>
/// 模块开火间隔：头顶转圈进度 + 剩余秒数（世界空间）。
/// </summary>
public class ModuleCooldownHud : MonoBehaviour
{
    Transform _root;
    SpriteRenderer _disk;
    SpriteRenderer _wedge;
    TextMesh _label;
    float _lastShown = -1f;

    public static ModuleCooldownHud Ensure(Transform moduleRoot, Vector3 localOffset)
    {
        if (moduleRoot == null)
        {
            return null;
        }

        var existing = moduleRoot.GetComponentInChildren<ModuleCooldownHud>(true);
        if (existing != null)
        {
            return existing;
        }

        var go = new GameObject("CooldownHud");
        go.transform.SetParent(moduleRoot, false);
        go.transform.localPosition = localOffset;
        var hud = go.AddComponent<ModuleCooldownHud>();
        hud.Build();
        return hud;
    }

    void Build()
    {
        _root = transform;

        var diskGo = new GameObject("Disk");
        diskGo.transform.SetParent(_root, false);
        diskGo.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
        _disk = diskGo.AddComponent<SpriteRenderer>();
        _disk.sprite = PrototypeSprites.Circle;
        _disk.color = new Color(0.05f, 0.05f, 0.08f, 0.72f);
        _disk.sortingOrder = 40;

        var wedgeGo = new GameObject("Progress");
        wedgeGo.transform.SetParent(_root, false);
        wedgeGo.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
        _wedge = wedgeGo.AddComponent<SpriteRenderer>();
        _wedge.sprite = PrototypeSprites.Circle;
        _wedge.color = new Color(0.95f, 0.75f, 0.25f, 0.85f);
        _wedge.sortingOrder = 41;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(_root, false);
        labelGo.transform.localPosition = Vector3.zero;
        labelGo.transform.localScale = new Vector3(0.07f, 0.07f, 1f);
        _label = labelGo.AddComponent<TextMesh>();
        _label.anchor = TextAnchor.MiddleCenter;
        _label.alignment = TextAlignment.Center;
        _label.fontSize = 48;
        _label.color = Color.white;
        _label.characterSize = 0.5f;
        var mr = labelGo.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 42;
        }

        SetVisible(false);
    }

    /// <summary>remaining≤0 或 total≤0 时隐藏。</summary>
    public void SetCooldown(float remaining, float total)
    {
        if (remaining <= 0.01f || total <= 0.01f)
        {
            if (_lastShown >= 0f)
            {
                SetVisible(false);
                _lastShown = -1f;
            }

            return;
        }

        if (_disk == null)
        {
            Build();
        }

        SetVisible(true);
        float t = Mathf.Clamp01(remaining / total);
        if (_wedge != null)
        {
            float s = 0.25f + 0.47f * t;
            _wedge.transform.localScale = new Vector3(s, s, 1f);
            _wedge.color = Color.Lerp(
                new Color(0.4f, 0.85f, 0.45f, 0.9f),
                new Color(0.95f, 0.55f, 0.2f, 0.9f),
                t);
        }

        if (_label != null)
        {
            string text = remaining >= 10f
                ? Mathf.CeilToInt(remaining).ToString()
                : remaining.ToString("0.0");
            if (_lastShown < 0f || Mathf.Abs(_lastShown - remaining) > 0.05f || _label.text != text)
            {
                _label.text = text;
                _lastShown = remaining;
            }
        }
    }

    void SetVisible(bool visible)
    {
        if (_root != null)
        {
            _root.gameObject.SetActive(visible);
        }
    }
}
