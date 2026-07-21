using UnityEngine;

/// <summary>
/// 大卫炸弹投掷物：飞向开火时锁定的目标点，到达后爆炸 AOE。
/// </summary>
public class BombProjectile : MonoBehaviour
{
    Vector3 _target;
    float _speed = 8f;
    int _damage;
    float _aoeRadius;
    bool _exploded;

    public void Launch(Vector3 from, Vector3 target, int damage, float aoeRadius, float speed = 8f)
    {
        transform.position = from;
        _target = target;
        _target.z = 0f;
        _damage = damage;
        _aoeRadius = aoeRadius;
        _speed = Mathf.Max(2f, speed);
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
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy e = enemies[i];
            if (e == null || !e.IsAlive)
            {
                continue;
            }

            if (Vector3.Distance(origin, e.transform.position) <= _aoeRadius)
            {
                e.TakeDamage(_damage);
            }
        }

        ShowBlastFx(origin);
        Destroy(gameObject);
    }

    void ShowBlastFx(Vector3 at)
    {
        var go = new GameObject("BombBlast");
        go.transform.position = at;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Circle;
        sr.color = new Color(1f, 0.55f, 0.12f, 0.6f);
        sr.sortingOrder = 22;
        go.transform.localScale = Vector3.one * (_aoeRadius * 2f);
        Destroy(go, 0.14f);
    }

    void EnsureVisual()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        sr.sprite = PrototypeSprites.Circle;
        sr.color = new Color(0.08f, 0.08f, 0.1f, 1f);
        sr.sortingOrder = 21;
        transform.localScale = Vector3.one * 0.28f;
    }
}
