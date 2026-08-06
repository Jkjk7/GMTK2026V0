using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 紫拆 / 金盾：前进冷却 → 吟唱 → 施法效果 → 循环。
/// 紫拆：冷却 5s、吟唱 5s、卸 2 模块；吟唱紫蓄力，触发紫闪+抖动。
/// 金盾：冷却/吟唱各 3s；吟唱金蓄力，触发金闪；护盾有金环特效。
/// </summary>
public class EnemyCasterAbility : MonoBehaviour
{
    public const float ShieldIdleSeconds = 3f;
    public const float ShieldCastSeconds = 3f;
    public const float DisassemblerIdleSeconds = 5f;
    public const float DisassemblerCastSeconds = 5f;
    public const float ShieldDuration = 3f;
    public const int ModulesToYank = 2;

    static readonly Color PurpleHalo = new Color(0.55f, 0.15f, 0.9f, 0.22f);
    static readonly Color PurpleRing = new Color(0.85f, 0.35f, 1f, 0.55f);
    static readonly Color PurpleCore = new Color(0.95f, 0.7f, 1f, 0.7f);
    static readonly Color GoldHalo = new Color(0.95f, 0.7f, 0.15f, 0.22f);
    static readonly Color GoldRing = new Color(1f, 0.88f, 0.3f, 0.6f);
    static readonly Color GoldCore = new Color(1f, 0.95f, 0.65f, 0.75f);

    Enemy _enemy;
    EnemyGoldType _kind;
    float _idleLeft;
    float _castLeft;
    bool _casting;
    bool _spawnBurstDone;
    SpriteRenderer _barBg;
    SpriteRenderer _barFill;
    Transform _chargeRoot;
    SpriteRenderer _chargeRing;
    SpriteRenderer _chargeCore;
    SpriteRenderer _chargeHalo;
    EnemyGoldType _chargePaletteKind;

    float IdleSeconds =>
        _kind == EnemyGoldType.Disassembler ? DisassemblerIdleSeconds : ShieldIdleSeconds;

    float CastSeconds =>
        _kind == EnemyGoldType.Disassembler ? DisassemblerCastSeconds : ShieldCastSeconds;

    bool UsesChargeFx =>
        _kind == EnemyGoldType.Disassembler || _kind == EnemyGoldType.ShieldCaster;

    public void Initialize(Enemy enemy, EnemyGoldType kind)
    {
        _enemy = enemy;
        _kind = kind;
        _idleLeft = IdleSeconds;
        _casting = false;
        EnsureCastBar();
        SetBarVisible(false);
        HideChargeFx();

        if (kind == EnemyGoldType.ShieldCaster && !_spawnBurstDone)
        {
            _spawnBurstDone = true;
            Enemy.ApplyShieldToAllAlive(ShieldDuration);
            CasterImpactFx.PlayGold(shake: false);
        }
    }

    void Update()
    {
        if (_enemy == null || !_enemy.IsAlive)
        {
            return;
        }

        if (GameSession.Instance != null && !GameSession.Instance.IsCombatActive)
        {
            return;
        }

        if (_casting)
        {
            _castLeft -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_castLeft / CastSeconds);
            RefreshBar(t);
            if (UsesChargeFx)
            {
                RefreshChargeFx(t);
            }

            if (_castLeft <= 0f)
            {
                FinishCast();
            }

            return;
        }

        _idleLeft -= Time.deltaTime;
        if (_idleLeft <= 0f)
        {
            BeginCast();
        }
    }

    void BeginCast()
    {
        _casting = true;
        _castLeft = CastSeconds;
        _enemy.SetMoveLocked(true);
        SetBarVisible(true);
        RefreshBar(0f);
        if (UsesChargeFx)
        {
            EnsureChargeFx();
            RefreshChargeFx(0f);
        }
    }

    void FinishCast()
    {
        _casting = false;
        _idleLeft = IdleSeconds;
        _enemy.SetMoveLocked(false);
        SetBarVisible(false);
        HideChargeFx();

        if (_kind == EnemyGoldType.Disassembler)
        {
            YankModulesToHand(ModulesToYank);
            CasterImpactFx.PlayPurple();
        }
        else if (_kind == EnemyGoldType.ShieldCaster)
        {
            Enemy.ApplyShieldToAllAlive(ShieldDuration);
            CasterImpactFx.PlayGold();
        }
    }

    static void YankModulesToHand(int count)
    {
        GridBoard board = Object.FindObjectOfType<GridBoard>();
        HandController hand = Object.FindObjectOfType<HandController>();
        if (board == null || hand == null || count <= 0)
        {
            return;
        }

        var coords = new List<GridCoord>();
        for (int x = 0; x < GridBoard.Width; x++)
        {
            for (int y = 0; y < GridBoard.Height; y++)
            {
                var c = new GridCoord(x, y);
                ModuleBase mod = board.GetModule(c);
                if (mod == null || mod.IsPermanentlyLocked)
                {
                    continue;
                }

                coords.Add(c);
            }
        }

        for (int i = 0; i < coords.Count; i++)
        {
            int j = Random.Range(i, coords.Count);
            (coords[i], coords[j]) = (coords[j], coords[i]);
        }

        int yanked = 0;
        for (int i = 0; i < coords.Count && yanked < count; i++)
        {
            if (!board.TryRemoveModule(coords[i], out ModuleCardData card))
            {
                continue;
            }

            if (!hand.TryAddCard(card))
            {
                Debug.LogWarning("[EnemyCaster] 手牌已满，模块无法回手。");
            }

            yanked++;
        }
    }

    void EnsureCastBar()
    {
        if (_barFill != null)
        {
            return;
        }

        var root = new GameObject("CastBar");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, -0.85f, 0f);

        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(root.transform, false);
        bgGo.transform.localScale = new Vector3(1.1f, 0.14f, 1f);
        _barBg = bgGo.AddComponent<SpriteRenderer>();
        _barBg.sprite = PrototypeSprites.Square;
        _barBg.color = new Color(0.1f, 0.08f, 0.12f, 0.9f);
        _barBg.sortingOrder = 28;

        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(root.transform, false);
        _barFill = fillGo.AddComponent<SpriteRenderer>();
        _barFill.sprite = PrototypeSprites.Square;
        _barFill.sortingOrder = 29;
    }

    void SetBarVisible(bool visible)
    {
        if (_barBg != null)
        {
            _barBg.gameObject.SetActive(visible);
        }

        if (_barFill != null)
        {
            _barFill.gameObject.SetActive(visible);
        }
    }

    void RefreshBar(float t01)
    {
        if (_barFill == null)
        {
            return;
        }

        const float w = 1.05f;
        _barFill.transform.localScale = new Vector3(Mathf.Max(0.04f, w * t01), 0.1f, 1f);
        _barFill.transform.localPosition = new Vector3((-w + w * t01) * 0.5f, 0f, 0f);
        _barFill.color = _kind == EnemyGoldType.ShieldCaster
            ? new Color(1f, 0.85f, 0.25f, 1f)
            : new Color(0.75f, 0.3f, 1f, 1f);
    }

    void EnsureChargeFx()
    {
        bool needRebuild = _chargeRoot == null || _chargePaletteKind != _kind;
        if (needRebuild && _chargeRoot != null)
        {
            Destroy(_chargeRoot.gameObject);
            _chargeRoot = null;
            _chargeHalo = null;
            _chargeRing = null;
            _chargeCore = null;
        }

        if (_chargeRoot != null)
        {
            _chargeRoot.gameObject.SetActive(true);
            return;
        }

        bool gold = _kind == EnemyGoldType.ShieldCaster;
        _chargePaletteKind = _kind;
        var root = new GameObject(gold ? "GoldChargeFx" : "PurpleChargeFx");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.zero;
        _chargeRoot = root.transform;

        _chargeHalo = CreateChargeSprite("Halo", 22, gold ? GoldHalo : PurpleHalo);
        _chargeRing = CreateChargeSprite("Ring", 23, gold ? GoldRing : PurpleRing);
        _chargeCore = CreateChargeSprite("Core", 24, gold ? GoldCore : PurpleCore);
    }

    SpriteRenderer CreateChargeSprite(string name, int order, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_chargeRoot, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Circle;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    void RefreshChargeFx(float t01)
    {
        EnsureChargeFx();
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * (6f + t01 * 10f));
        float ring = Mathf.Lerp(0.9f, 2.4f, t01) * (0.92f + 0.08f * pulse);
        float halo = Mathf.Lerp(1.2f, 3.1f, t01);
        float core = Mathf.Lerp(0.35f, 0.85f, t01) * (0.85f + 0.15f * pulse);

        if (_chargeHalo != null)
        {
            _chargeHalo.transform.localScale = new Vector3(halo, halo, 1f);
            Color c = _chargeHalo.color;
            c.a = Mathf.Lerp(0.12f, 0.35f, t01) * (0.7f + 0.3f * pulse);
            _chargeHalo.color = c;
        }

        if (_chargeRing != null)
        {
            _chargeRing.transform.localScale = new Vector3(ring, ring, 1f);
            _chargeRing.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
            Color c = _chargeRing.color;
            c.a = Mathf.Lerp(0.35f, 0.85f, t01);
            _chargeRing.color = c;
        }

        if (_chargeCore != null)
        {
            _chargeCore.transform.localScale = new Vector3(core, core, 1f);
            Color c = _chargeCore.color;
            c.a = Mathf.Lerp(0.4f, 0.95f, t01);
            _chargeCore.color = c;
        }
    }

    void HideChargeFx()
    {
        if (_chargeRoot != null)
        {
            _chargeRoot.gameObject.SetActive(false);
        }
    }

    void OnDestroy()
    {
        HideChargeFx();
    }
}

/// <summary>施法触发：彩色全屏闪 + 可选镜头抖动。</summary>
public static class CasterImpactFx
{
    public static void PlayPurple(float flashDuration = 0.28f, float shakeDuration = 0.38f, float shakeAmp = 0.22f)
    {
        PlayFlash(new Color(0.72f, 0.2f, 1f, 0.42f), flashDuration, "DisassemblerFlash");
        ScreenShake.Punch(shakeAmp, shakeDuration);
    }

    public static void PlayGold(float flashDuration = 0.28f, bool shake = true, float shakeDuration = 0.32f, float shakeAmp = 0.16f)
    {
        PlayFlash(new Color(1f, 0.86f, 0.25f, 0.4f), flashDuration, "ShieldCasterFlash");
        if (shake)
        {
            ScreenShake.Punch(shakeAmp, shakeDuration);
        }
    }

    static void PlayFlash(Color color, float duration, string name)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = color;
        sr.sortingOrder = 85;
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            go.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            go.transform.localScale = new Vector3(w * 1.15f, h * 1.15f, 1f);
        }
        else
        {
            go.transform.localScale = new Vector3(40f, 24f, 1f);
        }

        var fade = go.AddComponent<CasterFlashFade>();
        fade.Begin(sr, duration);
    }
}

/// <summary>闪屏淡出。</summary>
public class CasterFlashFade : MonoBehaviour
{
    SpriteRenderer _sr;
    float _duration;
    float _left;
    float _startAlpha;

    public void Begin(SpriteRenderer sr, float duration)
    {
        _sr = sr;
        _duration = Mathf.Max(0.05f, duration);
        _left = _duration;
        _startAlpha = sr != null ? sr.color.a : 0.4f;
    }

    void Update()
    {
        _left -= Time.deltaTime;
        if (_sr != null)
        {
            Color c = _sr.color;
            c.a = _startAlpha * Mathf.Clamp01(_left / _duration);
            _sr.color = c;
        }

        if (_left <= 0f)
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>主相机短促抖动。</summary>
public static class ScreenShake
{
    public static void Punch(float amplitude, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        var driver = cam.GetComponent<ScreenShakeDriver>();
        if (driver == null)
        {
            driver = cam.gameObject.AddComponent<ScreenShakeDriver>();
        }

        driver.Punch(amplitude, duration);
    }
}

public class ScreenShakeDriver : MonoBehaviour
{
    Vector3 _anchor;
    float _timeLeft;
    float _duration;
    float _amplitude;
    bool _active;

    public void Punch(float amplitude, float duration)
    {
        if (!_active)
        {
            _anchor = transform.position;
        }

        _amplitude = Mathf.Max(_amplitude, amplitude);
        _duration = Mathf.Max(0.05f, duration);
        _timeLeft = _duration;
        _active = true;
    }

    void LateUpdate()
    {
        if (!_active)
        {
            return;
        }

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            transform.position = _anchor;
            _amplitude = 0f;
            _active = false;
            return;
        }

        float falloff = _timeLeft / _duration;
        Vector2 jitter = Random.insideUnitCircle * (_amplitude * falloff);
        transform.position = _anchor + new Vector3(jitter.x, jitter.y, 0f);
    }
}
