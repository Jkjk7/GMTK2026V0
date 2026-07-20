using UnityEngine;

/// <summary>
/// 金币掉落与飞入特效入口。仅最后一片入账总额，避免 +N 显示碎片化。
/// </summary>
public class GoldDropService : MonoBehaviour
{
    public static GoldDropService Instance { get; private set; }

    GoldPanel _panel;
    Transform _vfxRoot;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Initialize(GoldPanel panel, Transform vfxRoot)
    {
        _panel = panel;
        _vfxRoot = vfxRoot != null ? vfxRoot : transform;
    }

    public void GrantGoldWithFly(int amount, Vector3 worldFrom)
    {
        if (amount <= 0)
        {
            return;
        }

        if (_panel == null)
        {
            Economy.Instance?.AddGold(amount);
            return;
        }

        int pieces = Mathf.Clamp(amount, 1, 3);
        for (int i = 0; i < pieces; i++)
        {
            Vector3 jitter = worldFrom + new Vector3(
                Random.Range(-0.25f, 0.25f),
                Random.Range(-0.15f, 0.35f),
                0f);
            bool last = i == pieces - 1;
            SpawnFly(jitter, last ? amount : 0, last);
        }
    }

    void SpawnFly(Vector3 from, int commitAmount, bool last)
    {
        var go = new GameObject("CoinFly");
        go.transform.SetParent(_vfxRoot, false);
        var vfx = go.AddComponent<CoinFlyVfx>();
        vfx.Play(from, _panel, commitAmount, last);
    }
}
