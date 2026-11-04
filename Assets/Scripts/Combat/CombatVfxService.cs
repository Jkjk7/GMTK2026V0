using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural bounded combat accents; no gameplay state is read or changed.</summary>
public static class CombatVfxService
{
    static CombatAccentPool _pool;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPoolReference() => _pool = null;

    static CombatAccentPool Pool
    {
        get
        {
            if (_pool == null)
                _pool = Object.FindFirstObjectByType<CombatAccentPool>();
            if (_pool == null)
                _pool = new GameObject("CombatAccentPool").AddComponent<CombatAccentPool>();
            return _pool;
        }
    }

    public static TransientSpriteVfx SpawnHit(Vector3 position) =>
        Spawn("HitAccent", position, PixelVfxSprites.Burst, Color.white, 0.18f, 0.48f);

    public static TransientSpriteVfx SpawnMelt(Vector3 position) =>
        Spawn("MeltWhiteHotBurst", position, PixelVfxSprites.Ring,
            new Color(1f, 0.95f, 0.72f, 1f), 0.38f, 1.1f);

    public static TransientSpriteVfx SpawnDeath(Vector3 position) =>
        Spawn("DeathAccent", position, PixelVfxSprites.Chip,
            new Color(0.71f, 0.51f, 0.28f, 1f), 0.32f, 0.82f);

    /// <summary>金盾破碎：半透明碎块向外飞散。</summary>
    public static void SpawnShieldShatter(Vector3 position, float size = 1f)
    {
        ShieldShatterVfx fx = Pool.RentShield();
        fx.transform.position = position;
        fx.Initialize(size);
    }

    static TransientSpriteVfx Spawn(
        string name, Vector3 position, Sprite sprite, Color color, float lifetime, float size)
    {
        TransientSpriteVfx effect = Pool.RentAccent();
        effect.gameObject.name = name;
        effect.transform.position = position;
        effect.Initialize(sprite, color, lifetime, size);
        return effect;
    }
}

/// <summary>Scene-owned cosmetic cache. Overflow restarts the oldest effect; no gameplay work is dropped.</summary>
public sealed class CombatAccentPool : MonoBehaviour
{
    const int AccentLimit = 96;
    const int ShieldLimit = 12;
    readonly List<TransientSpriteVfx> _accents = new List<TransientSpriteVfx>(AccentLimit);
    readonly Stack<TransientSpriteVfx> _idleAccents = new Stack<TransientSpriteVfx>(AccentLimit);
    readonly List<ShieldShatterVfx> _shields = new List<ShieldShatterVfx>(ShieldLimit);
    readonly Stack<ShieldShatterVfx> _idleShields = new Stack<ShieldShatterVfx>(ShieldLimit);
    bool _destroying;

    public TransientSpriteVfx RentAccent()
    {
        _accents.RemoveAll(effect => effect == null);
        if (_accents.Count >= AccentLimit) _accents[0].Finish();
        TransientSpriteVfx effect = null;
        while (_idleAccents.Count > 0 && effect == null) effect = _idleAccents.Pop();
        if (effect == null)
        {
            var go = new GameObject("CombatAccent");
            go.transform.SetParent(transform, false);
            effect = go.AddComponent<TransientSpriteVfx>();
            effect.Owner = this;
        }
        _accents.Add(effect);
        effect.gameObject.SetActive(true);
        return effect;
    }

    public ShieldShatterVfx RentShield()
    {
        _shields.RemoveAll(effect => effect == null);
        if (_shields.Count >= ShieldLimit) _shields[0].Finish();
        ShieldShatterVfx effect = null;
        while (_idleShields.Count > 0 && effect == null) effect = _idleShields.Pop();
        if (effect == null)
        {
            var go = new GameObject("ShieldShatter");
            go.transform.SetParent(transform, false);
            effect = go.AddComponent<ShieldShatterVfx>();
            effect.Owner = this;
        }
        _shields.Add(effect);
        effect.gameObject.SetActive(true);
        return effect;
    }

    public void Return(TransientSpriteVfx effect)
    {
        if (_destroying || !_accents.Remove(effect)) return;
        effect.gameObject.SetActive(false);
        if (_idleAccents.Count < AccentLimit) _idleAccents.Push(effect);
        else Destroy(effect.gameObject);
    }

    public void Return(ShieldShatterVfx effect)
    {
        if (_destroying || !_shields.Remove(effect)) return;
        effect.gameObject.SetActive(false);
        if (_idleShields.Count < ShieldLimit) _idleShields.Push(effect);
        else Destroy(effect.gameObject);
    }

    void OnDestroy()
    {
        _destroying = true;
        _accents.Clear();
        _idleAccents.Clear();
        _shields.Clear();
        _idleShields.Clear();
    }
}
