using System.Collections.Generic;
using UnityEngine;

/// <summary>Procedural bounded combat accents; no gameplay state is read or changed.</summary>
public static class CombatVfxService
{
    static CombatAccentPool _pool;
    static uint _sequence = 0x51a7d3u;

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

    public static TransientSpriteVfx SpawnHit(Vector3 position)
    {
        SpawnParticles(position, new Color(1f, .8f, .43f), .55f, PixelParticleBurst.Pattern.Impact);
        return Spawn("HitAccent", position, PixelVfxSprites.Burst, new Color(1f, .94f, .75f), .14f, .32f);
    }

    public static TransientSpriteVfx SpawnMelt(Vector3 position)
    {
        SpawnExplosion(position, new Color(1f, .36f, .08f), 1.65f);
        return Spawn("MeltWhiteHotBurst", position, PixelVfxSprites.Ring,
            new Color(1f, .8f, .35f, .85f), .28f, 1.1f);
    }

    public static TransientSpriteVfx SpawnDeath(Vector3 position)
    {
        SpawnParticles(position, new Color(.94f, .65f, .3f), .9f, PixelParticleBurst.Pattern.Disintegrate);
        return Spawn("DeathAccent", position, PixelVfxSprites.Chip,
            new Color(.71f, .51f, .28f, .8f), .22f, .38f);
    }

    /// <summary>Size is the visual diameter in world units, clamped to keep combat readable.</summary>
    public static PixelParticleBurst SpawnExplosion(Vector3 position, Color color, float size = 1f) =>
        SpawnParticles(position, color, size, PixelParticleBurst.Pattern.Explosion);

    public static PixelParticleBurst SpawnFrost(Vector3 position, float size = 1f) =>
        SpawnExplosion(position, new Color(.22f, .85f, 1f), size);

    public static PixelParticleBurst SpawnFire(Vector3 position, float size = 1f) =>
        SpawnExplosion(position, new Color(1f, .34f, .06f), size);

    public static PixelParticleBurst SpawnArcane(Vector3 position, float size = 1f) =>
        SpawnExplosion(position, new Color(.73f, .3f, 1f), size);

    public static PixelParticleBurst SpawnImplosion(Vector3 position, float size = 1f) =>
        SpawnParticles(position, new Color(.65f, .22f, 1f), size, PixelParticleBurst.Pattern.Implode);

    static PixelParticleBurst SpawnParticles(Vector3 position, Color color, float size, PixelParticleBurst.Pattern pattern)
    {
        PixelParticleBurst effect = Pool.RentParticles();
        effect.transform.position = position;
        effect.Initialize(color, size, pattern, PresentationSettings.ParticleIntensity, ++_sequence);
        return effect;
    }

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
    const int ParticleLimit = 24;
    readonly List<TransientSpriteVfx> _accents = new List<TransientSpriteVfx>(AccentLimit);
    readonly Stack<TransientSpriteVfx> _idleAccents = new Stack<TransientSpriteVfx>(AccentLimit);
    readonly List<ShieldShatterVfx> _shields = new List<ShieldShatterVfx>(ShieldLimit);
    readonly Stack<ShieldShatterVfx> _idleShields = new Stack<ShieldShatterVfx>(ShieldLimit);
    readonly List<PixelParticleBurst> _particles = new List<PixelParticleBurst>(ParticleLimit);
    readonly Stack<PixelParticleBurst> _idleParticles = new Stack<PixelParticleBurst>(ParticleLimit);
    bool _destroying;

    public PixelParticleBurst RentParticles()
    {
        _particles.RemoveAll(effect => effect == null);
        if (_particles.Count >= ParticleLimit) _particles[0].Finish();
        PixelParticleBurst effect = null;
        while (_idleParticles.Count > 0 && effect == null) effect = _idleParticles.Pop();
        if (effect == null)
        {
            var go = new GameObject("PixelParticleBurst");
            go.transform.SetParent(transform, false);
            effect = go.AddComponent<PixelParticleBurst>();
            effect.Owner = this;
        }
        _particles.Add(effect);
        effect.gameObject.SetActive(true);
        return effect;
    }

    public void Return(PixelParticleBurst effect)
    {
        if (_destroying || !_particles.Remove(effect)) return;
        effect.gameObject.SetActive(false);
        if (_idleParticles.Count < ParticleLimit) _idleParticles.Push(effect);
        else Destroy(effect.gameObject);
    }

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
        _particles.Clear();
        _idleParticles.Clear();
    }
}
