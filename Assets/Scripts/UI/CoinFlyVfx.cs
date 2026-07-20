using UnityEngine;

/// <summary>
/// 金色金币飞入金币栏（短停 + 贝塞尔 + 拖尾）。
/// commitAmount&gt;0 时到达后入账。
/// </summary>
public class CoinFlyVfx : MonoBehaviour
{
    [SerializeField] float holdSeconds = 0.2f;
    [SerializeField] float flySeconds = 0.55f;

    GoldPanel _panel;
    int _commitAmount;
    bool _commitOnArrive;
    float _t;
    Vector3 _start;
    Vector3 _control;
    Vector3 _end;
    bool _flying;
    LineRenderer _trail;
    SpriteRenderer _sr;

    public void Play(Vector3 worldFrom, GoldPanel panel, int commitAmount, bool commitOnArrive)
    {
        _panel = panel;
        _commitAmount = commitAmount;
        _commitOnArrive = commitOnArrive;
        _start = worldFrom;
        transform.position = worldFrom;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = PrototypeSprites.Circle;
        _sr.color = new Color(1f, 0.85f, 0.25f, 1f);
        _sr.sortingOrder = 40;
        transform.localScale = Vector3.one * 0.35f;

        _trail = gameObject.AddComponent<LineRenderer>();
        _trail.positionCount = 8;
        _trail.startWidth = 0.08f;
        _trail.endWidth = 0.02f;
        _trail.material = new Material(Shader.Find("Sprites/Default"));
        _trail.startColor = new Color(1f, 0.9f, 0.3f, 0.9f);
        _trail.endColor = new Color(1f, 0.7f, 0.1f, 0f);
        _trail.sortingOrder = 39;
        _trail.useWorldSpace = true;

        _t = -holdSeconds;
        _flying = false;
        ResolveEnd();
    }

    void ResolveEnd()
    {
        _end = _panel != null ? _panel.GetWorldIconPosition() : _start + Vector3.up * 2f;
        _control = (_start + _end) * 0.5f + Vector3.up * 1.5f;
    }

    void Update()
    {
        _t += Time.deltaTime;
        if (!_flying)
        {
            if (_t >= 0f)
            {
                _flying = true;
                ResolveEnd();
            }

            return;
        }

        float u = Mathf.Clamp01(_t / flySeconds);
        Vector3 a = Vector3.Lerp(_start, _control, u);
        Vector3 b = Vector3.Lerp(_control, _end, u);
        transform.position = Vector3.Lerp(a, b, u);

        if (_trail != null)
        {
            for (int i = 0; i < _trail.positionCount; i++)
            {
                float tu = Mathf.Clamp01(u - (1f - i / (float)_trail.positionCount) * 0.15f);
                Vector3 ta = Vector3.Lerp(_start, _control, tu);
                Vector3 tb = Vector3.Lerp(_control, _end, tu);
                _trail.SetPosition(i, Vector3.Lerp(ta, tb, tu));
            }
        }

        if (u >= 1f)
        {
            if (_commitOnArrive && _commitAmount > 0)
            {
                if (_panel != null)
                {
                    _panel.CommitFlyGold(_commitAmount);
                }
                else
                {
                    Economy.Instance?.AddGold(_commitAmount);
                }
            }

            Destroy(gameObject);
        }
    }
}
