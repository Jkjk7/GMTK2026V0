using UnityEngine;

/// <summary>
/// 沙子粒子调度：沙漏→发射器漏沙；沙怪击杀→沙漏回填（到达后入账）。
/// 测试单位：1 粒视觉 = GrainMs（默认 10ms）。
/// </summary>
public class SandVfxService : MonoBehaviour
{
    public const int GrainMs = 10;

    public static SandVfxService Instance { get; private set; }

    Transform _vfxRoot;
    SandClockPanel _panel;
    Emitter _emitter;
    float _drainVisualAcc;
    int _activeDrainGrains;
    const int MaxDrainGrains = 48;

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

    public void Initialize(SandClockPanel panel, Emitter emitter, Transform vfxRoot)
    {
        _panel = panel;
        _emitter = emitter;
        _vfxRoot = vfxRoot != null ? vfxRoot : transform;
    }

    /// <summary>熔炉抽沙时调用：从沙漏底部飞向发射器（纯表现）。</summary>
    public void NotifyDrain(int drainedMs)
    {
        if (drainedMs <= 0)
        {
            return;
        }

        _drainVisualAcc += drainedMs;
        while (_drainVisualAcc >= GrainMs && _activeDrainGrains < MaxDrainGrains)
        {
            _drainVisualAcc -= GrainMs;
            SpawnDrainGrain();
        }

        // 粒子达上限时丢弃积压，避免卡顿
        if (_activeDrainGrains >= MaxDrainGrains)
        {
            _drainVisualAcc = 0f;
        }
    }

    /// <summary>沙怪击杀：沙粒飞入沙漏，最后一粒到达后入账。</summary>
    public void GrantSandWithFly(int amountMs, Vector3 worldFrom)
    {
        if (amountMs <= 0)
        {
            return;
        }

        if (_panel == null || SandClock.Instance == null)
        {
            SandClock.Instance?.AddSand(amountMs);
            return;
        }

        int pieces = Mathf.Clamp(amountMs / GrainMs, 4, 24);
        Vector3 end = _panel.GetWorldFillPosition();

        for (int i = 0; i < pieces; i++)
        {
            Vector3 jitter = worldFrom + new Vector3(
                Random.Range(-0.35f, 0.35f),
                Random.Range(-0.2f, 0.4f),
                0f);
            // 与金币飞入一致：仅最后一粒到达时入账全额
            bool last = i == pieces - 1;
            int commit = last ? amountMs : 0;
            SpawnGrain(jitter, end, commit, 0.5f + Random.Range(0f, 0.2f));
        }
    }

    void SpawnDrainGrain()
    {
        if (_panel == null || _emitter == null)
        {
            return;
        }

        Vector3 from = _panel.GetWorldLeakPosition();
        Vector3 to = _emitter.transform.position;
        from += new Vector3(Random.Range(-0.08f, 0.08f), Random.Range(-0.05f, 0.05f), 0f);
        _activeDrainGrains++;
        SpawnGrain(from, to, 0, 0.28f + Random.Range(0f, 0.12f), () => { _activeDrainGrains--; });
    }

    void SpawnGrain(Vector3 from, Vector3 to, int commitMs, float duration, System.Action extraOnArrive = null)
    {
        var go = new GameObject("SandGrain");
        go.transform.SetParent(_vfxRoot, false);
        var grain = go.AddComponent<SandGrainVfx>();
        grain.Play(from, to, () =>
        {
            extraOnArrive?.Invoke();
            if (commitMs > 0)
            {
                SandClock.Instance?.AddSand(commitMs);
            }
        }, duration);
    }
}
