using System;
using UnityEngine;

/// <summary>
/// 单粒沙子飞行动画（世界空间）。到达后可选回调入账。
/// </summary>
public class SandGrainVfx : MonoBehaviour
{
    [SerializeField] float flySeconds = 0.45f;

    float _t;
    Vector3 _start;
    Vector3 _control;
    Vector3 _end;
    Action _onArrive;
    SpriteRenderer _sr;
    LineRenderer _trail;

    public void Play(Vector3 from, Vector3 to, Action onArrive = null, float duration = -1f)
    {
        _start = from;
        _end = to;
        _control = (_start + _end) * 0.5f + new Vector3(0f, 0.6f + UnityEngine.Random.Range(-0.2f, 0.35f), 0f);
        _onArrive = onArrive;
        if (duration > 0f)
        {
            flySeconds = duration;
        }

        transform.position = from;
        _t = 0f;

        _sr = gameObject.AddComponent<SpriteRenderer>();
        _sr.sprite = PrototypeSprites.Circle;
        _sr.color = new Color(0.92f, 0.82f, 0.42f, 0.95f);
        _sr.sortingOrder = 42;
        transform.localScale = Vector3.one * UnityEngine.Random.Range(0.12f, 0.18f);

        _trail = gameObject.AddComponent<LineRenderer>();
        _trail.positionCount = 6;
        _trail.startWidth = 0.05f;
        _trail.endWidth = 0.01f;
        _trail.material = new Material(Shader.Find("Sprites/Default"));
        _trail.startColor = new Color(0.95f, 0.85f, 0.45f, 0.7f);
        _trail.endColor = new Color(0.8f, 0.65f, 0.25f, 0f);
        _trail.sortingOrder = 41;
        _trail.useWorldSpace = true;
    }

    void Update()
    {
        _t += Time.deltaTime;
        float u = Mathf.Clamp01(_t / Mathf.Max(0.05f, flySeconds));
        Vector3 a = Vector3.Lerp(_start, _control, u);
        Vector3 b = Vector3.Lerp(_control, _end, u);
        transform.position = Vector3.Lerp(a, b, u);

        if (_trail != null)
        {
            for (int i = 0; i < _trail.positionCount; i++)
            {
                float tu = Mathf.Clamp01(u - (1f - i / (float)_trail.positionCount) * 0.12f);
                Vector3 ta = Vector3.Lerp(_start, _control, tu);
                Vector3 tb = Vector3.Lerp(_control, _end, tu);
                _trail.SetPosition(i, Vector3.Lerp(ta, tb, tu));
            }
        }

        if (u >= 1f)
        {
            _onArrive?.Invoke();
            Destroy(gameObject);
        }
    }
}
