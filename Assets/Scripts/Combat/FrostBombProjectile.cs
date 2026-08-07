using UnityEngine;

/// <summary>
/// 冰霜炸弹投掷物：飞向锁定点，落地后留下持续挂寒的霜环。
/// </summary>
public class FrostBombProjectile : MonoBehaviour
{
    Vector3 _target;
    float _speed = 16f;
    float _aoeRadius;
    float _ringDuration;
    bool _exploded;

    public void Launch(
        Vector3 from,
        Vector3 target,
        float aoeRadius,
        float ringDuration,
        ModuleBase source = null,
        float speed = 16f)
    {
        transform.position = from;
        _target = target;
        _target.z = 0f;
        _aoeRadius = aoeRadius;
        _ringDuration = ringDuration;
        _ = source;
        _speed = Mathf.Max(12f, speed);
        _exploded = false;
        EnsureVisual();
    }

    void Update()
    {
        if (_exploded)
        {
            return;
        }

        Vector3 pos = transform.position;
        pos.z = 0f;
        Vector3 next = Vector3.MoveTowards(pos, _target, _speed * Time.deltaTime);
        transform.position = next;

        if ((next - _target).sqrMagnitude <= 0.0004f)
        {
            Explode();
        }
    }

    void Explode()
    {
        if (_exploded)
        {
            return;
        }

        _exploded = true;
        Vector3 origin = _target;
        // 伤害为 0：落地只留霜环持续挂寒（半径已在 Catalog 含 AOE 倍率）
        FrostRingZone.Spawn(origin, _aoeRadius, _ringDuration);
        ShowBlastFx(origin, _aoeRadius);
        Destroy(gameObject);
    }

    void ShowBlastFx(Vector3 at, float radius)
    {
        var go = new GameObject("FrostBombBlast");
        go.transform.position = at;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Circle;
        sr.color = new Color(0.4f, 0.85f, 1f, 0.55f);
        sr.sortingOrder = 22;
        go.transform.localScale = Vector3.one * (radius * 2f);
        Destroy(go, 0.16f);
    }

    void EnsureVisual()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        sr.sprite = PrototypeSprites.Circle;
        sr.color = new Color(0.35f, 0.75f, 1f, 1f);
        sr.sortingOrder = 21;
        transform.localScale = Vector3.one * 0.32f;
    }
}
