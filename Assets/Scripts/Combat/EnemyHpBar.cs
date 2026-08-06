using UnityEngine;

/// <summary>敌人头顶血条；Boss 更大、显示数值，并放在体型下方以免出屏。</summary>
public class EnemyHpBar : MonoBehaviour
{
    SpriteRenderer _bg;
    SpriteRenderer _fill;
    TextMesh _label;
    bool _isBoss;
    float _barWidth = 0.9f;

    public static EnemyHpBar Attach(Enemy enemy, bool isBoss)
    {
        if (enemy == null)
        {
            return null;
        }

        var existing = enemy.GetComponentInChildren<EnemyHpBar>();
        if (existing != null)
        {
            existing._isBoss = isBoss;
            existing.ConfigureLayout();
            return existing;
        }

        var go = new GameObject("HpBar");
        go.transform.SetParent(enemy.transform, false);
        var bar = go.AddComponent<EnemyHpBar>();
        bar._isBoss = isBoss;
        bar.Build();
        return bar;
    }

    void Build()
    {
        ConfigureLayout();

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(transform, false);
        bgGo.transform.localScale = new Vector3(_barWidth, _isBoss ? 0.18f : 0.1f, 1f);
        _bg = bgGo.AddComponent<SpriteRenderer>();
        _bg.sprite = PrototypeSprites.Square;
        _bg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        _bg.sortingOrder = 30;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(transform, false);
        _fill = fillGo.AddComponent<SpriteRenderer>();
        _fill.sprite = PrototypeSprites.Square;
        _fill.color = _isBoss
            ? new Color(1f, 0.35f, 0.2f, 1f)
            : new Color(0.35f, 0.9f, 0.4f, 1f);
        _fill.sortingOrder = 31;

        if (_isBoss)
        {
            var labelGo = new GameObject("HpLabel");
            labelGo.transform.SetParent(transform, false);
            // 血条在 Boss 下方：数值再略偏下，避免被体型挡住
            labelGo.transform.localPosition = new Vector3(0f, -0.32f, 0f);
            labelGo.transform.localScale = new Vector3(0.06f, 0.06f, 1f);
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 40;
            _label.color = Color.white;
            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = 32;
            }
        }
    }

    void ConfigureLayout()
    {
        if (_isBoss)
        {
            // 抵消 Boss 巨大 localScale，让血条以世界尺度显示在脚下。
            float parentScale = transform.parent != null
                ? Mathf.Abs(transform.parent.lossyScale.y)
                : 1f;
            float inv = 1f / Mathf.Max(0.01f, parentScale);
            transform.localScale = new Vector3(inv, inv, 1f);

            const float belowWorld = -1.55f;
            transform.localPosition = new Vector3(0f, belowWorld * inv, 0f);
            _barWidth = 2.8f;
        }
        else
        {
            transform.localScale = Vector3.one;
            transform.localPosition = new Vector3(0f, 0.85f, 0f);
            _barWidth = 0.95f;
        }

        if (_bg != null)
        {
            _bg.transform.localScale = new Vector3(_barWidth, _isBoss ? 0.18f : 0.1f, 1f);
        }

        if (_label != null)
        {
            _label.transform.localPosition = new Vector3(0f, _isBoss ? -0.32f : 0.28f, 0f);
        }
    }

    public void Refresh(int current, int max)
    {
        if (_fill == null)
        {
            return;
        }

        float t = max > 0 ? Mathf.Clamp01(current / (float)max) : 0f;
        float h = _isBoss ? 0.16f : 0.08f;
        _fill.transform.localScale = new Vector3(Mathf.Max(0.02f, _barWidth * t), h, 1f);
        _fill.transform.localPosition = new Vector3((-_barWidth + _barWidth * t) * 0.5f, 0f, 0f);

        if (_label != null)
        {
            _label.text = $"{current}/{max}";
        }
    }
}
