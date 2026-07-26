using UnityEngine;

/// <summary>
/// 统一伤害入口：附魔倍率、融化反应、挂烧/寒。
/// </summary>
public static class CombatDamage
{
    public struct HitEffects
    {
        public bool IsBurnHit;
        public bool IsChillHit;
        public float ApplyBurnSeconds;
        public float ApplyChillSeconds;
        public float ChillPercent;

        public static HitEffects None => default;

        public static HitEffects Burn(float seconds) => new HitEffects
        {
            IsBurnHit = true,
            ApplyBurnSeconds = Mathf.Max(0f, seconds)
        };

        public static HitEffects Chill(float seconds, float percent = ModuleCatalog.IceSlowPercent) =>
            new HitEffects
            {
                IsChillHit = true,
                ApplyChillSeconds = Mathf.Max(0f, seconds),
                ChillPercent = percent
            };
    }

    public static void Apply(ModuleBase source, Enemy target, int rawDamage, HitEffects fx)
    {
        if (target == null || !target.IsAlive || rawDamage <= 0)
        {
            return;
        }

        CellEnchant enchant = CellEnchant.None;
        if (source != null && source.BoundBoard != null)
        {
            enchant = source.BoundBoard.GetEnchant(source.Cell);
        }

        float mult = 1f;
        if (enchant == CellEnchant.DamageUp)
        {
            mult *= 1.2f;
        }
        else if (enchant == CellEnchant.Shrink)
        {
            mult *= 0.5f;
        }

        bool wasBurning = target.IsBurning;
        bool wasChilled = target.IsChilled;
        bool melt = (fx.IsChillHit && wasBurning) || (fx.IsBurnHit && wasChilled);

        int damage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * mult));
        if (melt)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(damage * 1.5f));
            SpawnMeltPopup(target.transform.position);
            CombatVfxService.SpawnMelt(target.transform.position);
        }

        target.TakeDamage(damage);

        // 融化：烧↔寒抵消，本发不再重新挂状态
        if (melt)
        {
            target.ClearBurnAndChill();
            return;
        }

        float burnSec = fx.ApplyBurnSeconds;
        float chillSec = fx.ApplyChillSeconds;
        float chillPct = fx.ChillPercent > 0f ? fx.ChillPercent : ModuleCatalog.IceSlowPercent;

        if (enchant == CellEnchant.Flame)
        {
            burnSec = Mathf.Max(burnSec, 3f);
        }

        if (enchant == CellEnchant.Frost)
        {
            chillSec = Mathf.Max(chillSec, 3f);
        }

        if (burnSec > 0f)
        {
            target.ApplyBurn(burnSec);
        }

        if (chillSec > 0f)
        {
            target.ApplySlow(chillPct, chillSec);
        }
    }

    static void SpawnMeltPopup(Vector3 worldPos)
    {
        var go = new GameObject("MeltPopup");
        go.transform.position = worldPos + new Vector3(0f, 0.45f, 0f);
        go.transform.localScale = new Vector3(0.12f, 0.12f, 1f);
        var tm = go.AddComponent<TextMesh>();
        tm.text = "Melt";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = 36;
        tm.color = new Color(1f, 0.75f, 0.35f, 1f);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 40;
        }

        go.AddComponent<MeltPopupMotion>();
        Object.Destroy(go, 0.7f);
    }

    sealed class MeltPopupMotion : MonoBehaviour
    {
        TextMesh _tm;
        float _t;

        void Awake()
        {
            _tm = GetComponent<TextMesh>();
        }

        void Update()
        {
            _t += Time.deltaTime;
            transform.position += Vector3.up * (1.2f * Time.deltaTime);
            if (_tm != null)
            {
                Color c = _tm.color;
                c.a = Mathf.Clamp01(1f - _t / 0.65f);
                _tm.color = c;
            }
        }
    }
}
