using UnityEngine;

/// <summary>
/// 冰霜炮直线投射物：快速飞向锁定敌人，命中造成伤害并施加 3 秒 [寒冷]。
/// </summary>
public class FrostCannonProjectile : MonoBehaviour
{
    Enemy _target;
    ModuleBase _source;
    int _damage;
    float _speed = 28f;
    bool _hit;

    public void Launch(Vector3 from, Enemy target, int damage, ModuleBase source, float speed = 28f)
    {
        transform.position = from;
        _target = target;
        _damage = damage;
        _source = source;
        _speed = Mathf.Max(18f, speed);
        _hit = false;
        EnsureVisual();
    }

    void Update()
    {
        if (_hit)
        {
            return;
        }

        if (_target == null || !_target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dest = _target.transform.position;
        dest.z = 0f;
        Vector3 next = Vector3.MoveTowards(transform.position, dest, _speed * Time.deltaTime);
        transform.position = next;

        Vector3 dir = dest - next;
        if (dir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        if ((next - dest).sqrMagnitude <= 0.08f * 0.08f)
        {
            Hit();
        }
    }

    void Hit()
    {
        if (_hit)
        {
            return;
        }

        _hit = true;
        if (_target != null && _target.IsAlive)
        {
            CombatDamage.Apply(
                _source,
                _target,
                _damage,
                CombatDamage.HitEffects.Chill(3f, ModuleCatalog.IceSlowPercent));
        }

        Destroy(gameObject);
    }

    void EnsureVisual()
    {
        var sr = gameObject.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }

        sr.sprite = PrototypeSprites.Circle;
        sr.color = new Color(0.55f, 0.9f, 1f, 1f);
        sr.sortingOrder = 21;
        transform.localScale = new Vector3(0.42f, 0.28f, 1f);
    }
}
