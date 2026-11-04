using UnityEngine;

/// <summary>Small cached furnace accents driven by observed buffer fill, never by simulation writes.</summary>
public sealed class WorldVisualAnimator : MonoBehaviour
{
    Emitter _emitter;
    Transform _nozzle;
    SpriteRenderer _core, _rim;
    float _clock, _lastFill, _recoil;
    static readonly Vector3 NozzleRest = new Vector3(0.47f, 0f, 0f);

    public static void AttachEmitter(Emitter emitter)
    {
        if (emitter == null) return;
        var animator = emitter.GetComponent<WorldVisualAnimator>();
        if (animator == null) animator = emitter.gameObject.AddComponent<WorldVisualAnimator>();
        animator._emitter = emitter;
        animator._lastFill = emitter.BufferFill01;
        if (animator._nozzle == null) animator.Build();
    }

    void Build()
    {
        var root = new GameObject("FurnacePresentation").transform;
        root.SetParent(transform, false);
        _rim = Add(root, "BrassRim", PixelVfxSprites.Ring, Vector3.zero,
            new Vector3(0.94f, 0.94f, 1f), new Color(0.34f, 0.23f, 0.11f, 1f), 6);
        Add(root, "CoreSocket", PrototypeSprites.Square, Vector3.zero,
            new Vector3(0.46f, 0.46f, 1f), new Color(0.12f, 0.16f, 0.2f), 7);
        _core = Add(root, "AmberCore", PixelVfxSprites.Chip, Vector3.zero,
            new Vector3(0.39f, 0.39f, 1f), new Color(1f, 0.68f, 0.18f), 8);
        _nozzle = Add(root, "Nozzle", PrototypeSprites.Square, NozzleRest,
            new Vector3(0.32f, 0.19f, 1f), new Color(0.36f, 0.29f, 0.17f), 7).transform;
        Add(_nozzle, "HotTip", PrototypeSprites.Square, new Vector3(0.35f, 0f, 0f),
            new Vector3(0.28f, 0.75f, 1f), new Color(1f, 0.83f, 0.4f), 8);
    }

    void LateUpdate()
    {
        float delta = GameSession.Instance == null || GameSession.Instance.IsCombatActive ? Time.deltaTime : 0f;
        Tick(delta);
    }

    void Tick(float delta)
    {
        if (_emitter == null || _nozzle == null) return;
        float fill = _emitter.BufferFill01;
        // A wrap of the existing fill meter is the emission cue; no gameplay callback or RNG needed.
        if (fill + 0.02f < _lastFill) _recoil = 0.18f;
        _lastFill = fill;
        delta = Mathf.Max(0f, delta);
        _recoil = Mathf.Max(0f, _recoil - delta);
        bool motion = PresentationSettings.MotionEnabled;
        if (motion) _clock += delta;
        float t = Mathf.Floor(_clock * 12f) / 12f;
        float recoil = motion ? _recoil / 0.18f : 0f;
        _nozzle.localPosition = NozzleRest + Vector3.left * (Mathf.Round(recoil * 5f) / 48f);
        _nozzle.localScale = new Vector3(0.32f * (1f - recoil * 0.18f), 0.19f * (1f + recoil * 0.1f), 1f);
        _core.transform.localRotation = Quaternion.Euler(0, 0, motion ? Mathf.Floor(t * 3f) * 90f : 0f);
        float pulse = motion ? Mathf.Sin(t * 5f) * 0.035f : 0f;
        _core.transform.localScale = Vector3.one * (0.32f + fill * 0.1f + pulse);
        _core.color = Color.Lerp(new Color(0.92f, 0.43f, 0.12f), new Color(1f, 0.91f, 0.5f), fill);
        _rim.color = Color.Lerp(new Color(0.34f, 0.23f, 0.11f), new Color(0.7f, 0.48f, 0.2f), fill);
    }

    void OnDisable()
    {
        if (_nozzle == null) return;
        _recoil = 0f;
        _nozzle.localPosition = NozzleRest;
        _nozzle.localScale = new Vector3(0.32f, 0.19f, 1f);
        _core.transform.localRotation = Quaternion.identity;
    }

    static SpriteRenderer Add(Transform parent, string name, Sprite sprite, Vector3 position,
        Vector3 scale, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position; go.transform.localScale = scale;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
        return renderer;
    }
}
