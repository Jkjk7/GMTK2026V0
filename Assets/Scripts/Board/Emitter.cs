using UnityEngine;

/// <summary>
/// 熔炉（原发射器）：战斗中从沙漏 SandClock 实时抽沙（1000 粒/秒 = 实时），
/// 缓冲攒满 capMs（默认 2000ms ≈ 2 秒）即结晶成一枚能量球射入入口格 (0,3)，缓冲清空。
/// </summary>
public class Emitter : MonoBehaviour
{
    [Header("Furnace")]
    [Tooltip("无升级系统时的熔炉容量（毫秒）。")]
    [SerializeField] int defaultCapMs = 2000;

    [Tooltip("球生成点相对入口格中心的左侧偏移（以格为单位）。")]
    [SerializeField] float leftOffsetInCells = 0.85f;

    [Header("Entry")]
    [SerializeField] int entryCol = 0;
    [SerializeField] int entryRow = 3;

    GridBoard _board;
    EnergyBallManager _ballManager;
    GameSession _session;
    SandClock _sandClock;
    int _bufferMs;
    float _drainAccumulator;
    SpriteRenderer _visual;
    Vector3 _baseScale = new Vector3(0.55f, 0.55f, 1f);

    Transform _energyHud;
    SpriteRenderer _energyHudFill;

    public GridCoord EntryCell => new GridCoord(entryCol, entryRow);
    public int BufferMs => _bufferMs;
    public int CapMs => EmitterRunUpgrades.Instance != null
        ? EmitterRunUpgrades.Instance.FurnaceCapMs
        : Mathf.Max(1, defaultCapMs);

    public float BufferFill01
    {
        get
        {
            int cap = CapMs;
            return cap > 0 ? Mathf.Clamp01(_bufferMs / (float)cap) : 0f;
        }
    }

    public void Initialize(
        GridBoard board,
        EnergyBallManager ballManager,
        GameSession session,
        SandClock sandClock = null)
    {
        _board = board;
        _ballManager = ballManager;
        _session = session;
        _sandClock = sandClock;
        _bufferMs = 0;
        _drainAccumulator = 0f;
        SnapToOffBoardPosition();
        EnsureVisual();
        RefreshEnergyHud();
    }

    public void BindSandClock(SandClock sandClock)
    {
        _sandClock = sandClock;
    }

    void Update()
    {
        if (_board == null || _ballManager == null)
        {
            return;
        }

        if (_session != null && !_session.IsCombatActive)
        {
            RefreshEnergyHud();
            return;
        }

        if (_visual != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2.4f) * 0.06f;
            transform.localScale = _baseScale * pulse;
        }

        _drainAccumulator += Time.deltaTime * 1000f;
        int deltaMs = (int)_drainAccumulator;
        if (deltaMs <= 0)
        {
            RefreshEnergyHud();
            return;
        }

        _drainAccumulator -= deltaMs;
        int drained = _sandClock != null ? _sandClock.TryDrain(deltaMs) : deltaMs;
        _bufferMs += drained;
        if (drained > 0)
        {
            SandVfxService.Instance?.NotifyDrain(drained);
        }

        int cap = CapMs;
        while (_bufferMs >= cap)
        {
            _bufferMs -= cap;
            TryFire();
        }

        RefreshEnergyHud();
    }

    public void SnapToOffBoardPosition()
    {
        if (_board == null)
        {
            return;
        }

        Vector3 entryCenter = _board.CellToWorld(EntryCell);
        Vector3 left = _board.transform.TransformVector(Vector3.left * leftOffsetInCells * _board.CellSize);
        transform.position = entryCenter + left;
    }

    void TryFire()
    {
        _ballManager.TrySpawn(transform.position, GridDirection.Right);
    }

    void EnsureVisual()
    {
        if (_visual != null)
        {
            return;
        }

        _visual = gameObject.GetComponent<SpriteRenderer>();
        if (_visual == null)
        {
            _visual = gameObject.AddComponent<SpriteRenderer>();
        }

        _visual.sprite = PrototypeSprites.Square;
        _visual.color = new Color(0.95f, 0.75f, 0.2f, 1f);
        _visual.sortingOrder = 5;
        _baseScale = new Vector3(0.55f, 0.55f, 1f);
        transform.localScale = _baseScale;

        var arrow = new GameObject("Arrow");
        arrow.transform.SetParent(transform, false);
        arrow.transform.localPosition = new Vector3(0.55f, 0f, 0f);
        arrow.transform.localScale = new Vector3(0.35f, 0.2f, 1f);
        var asr = arrow.AddComponent<SpriteRenderer>();
        asr.sprite = PrototypeSprites.Square;
        asr.color = new Color(1f, 0.9f, 0.4f, 1f);
        asr.sortingOrder = 6;

        EnsureEnergyHud();
    }

    void EnsureEnergyHud()
    {
        if (_energyHud != null)
        {
            return;
        }

        var hudGo = new GameObject("EnergyHud");
        hudGo.transform.SetParent(transform, false);
        hudGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        hudGo.transform.localScale = new Vector3(1f / 0.55f, 1f / 0.55f, 1f);
        _energyHud = hudGo.transform;

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(hudGo.transform, false);
        bgGo.transform.localScale = new Vector3(0.95f, 0.16f, 1f);
        var bg = bgGo.AddComponent<SpriteRenderer>();
        bg.sprite = PrototypeSprites.Square;
        bg.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        bg.sortingOrder = 18;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(hudGo.transform, false);
        _energyHudFill = fillGo.AddComponent<SpriteRenderer>();
        _energyHudFill.sprite = PrototypeSprites.Square;
        _energyHudFill.sortingOrder = 19;
    }

    void RefreshEnergyHud()
    {
        EnsureEnergyHud();
        if (_energyHudFill == null)
        {
            return;
        }

        float fill = BufferFill01;
        const float barWidth = 0.9f;
        _energyHudFill.transform.localScale = new Vector3(Mathf.Max(0.04f, barWidth * fill), 0.12f, 1f);
        _energyHudFill.transform.localPosition = new Vector3((-barWidth + barWidth * fill) * 0.5f, 0f, 0f);
        _energyHudFill.color = Color.Lerp(
            new Color(0.55f, 0.75f, 1f, 1f),
            new Color(1f, 0.85f, 0.3f, 1f),
            fill);
    }
}
