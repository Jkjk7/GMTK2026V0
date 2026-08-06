using UnityEngine;

/// <summary>超过阈值的伤害跳字；伤害越高字越大。</summary>
public static class DamageNumberPopup
{
    public const int Threshold = 30;

    public static void TrySpawn(Vector3 worldPos, int damage)
    {
        if (damage <= Threshold)
        {
            return;
        }

        float intensity = Mathf.Clamp01((damage - Threshold) / 170f);
        float baseScale = Mathf.Lerp(0.11f, 0.28f, intensity);
        int fontSize = Mathf.RoundToInt(Mathf.Lerp(34f, 64f, intensity));
        Color color = Color.Lerp(
            new Color(1f, 0.92f, 0.55f, 1f),
            new Color(1f, 0.35f, 0.2f, 1f),
            intensity);

        var go = new GameObject("DamagePopup");
        float jitterX = Random.Range(-0.18f, 0.18f);
        go.transform.position = worldPos + new Vector3(jitterX, 0.55f, 0f);
        go.transform.localScale = Vector3.one * baseScale;

        var tm = go.AddComponent<TextMesh>();
        tm.text = damage.ToString();
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontSize = fontSize;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sortingOrder = 42;
        }

        var motion = go.AddComponent<DamagePopupMotion>();
        motion.Initialize(baseScale, Mathf.Lerp(0.55f, 0.85f, intensity));
        Object.Destroy(go, 1f);
    }

    sealed class DamagePopupMotion : MonoBehaviour
    {
        TextMesh _tm;
        float _life = 0.7f;
        float _t;
        float _baseScale = 0.12f;
        Vector3 _drift;

        public void Initialize(float baseScale, float lifeSeconds)
        {
            _baseScale = baseScale;
            _life = Mathf.Max(0.4f, lifeSeconds);
            _drift = new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(1.1f, 1.7f), 0f);
            _tm = GetComponent<TextMesh>();
        }

        void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / _life);
            transform.position += _drift * Time.deltaTime;
            float pop = 1f + 0.22f * Mathf.Sin(Mathf.PI * Mathf.Clamp01(u * 2.2f));
            transform.localScale = Vector3.one * (_baseScale * pop);

            if (_tm != null)
            {
                Color c = _tm.color;
                c.a = Mathf.Clamp01(1f - u);
                _tm.color = c;
            }
        }
    }
}
