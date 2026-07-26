using UnityEngine;

/// <summary>Procedural bounded combat accents; no gameplay state is read or changed.</summary>
public static class CombatVfxService
{
    public static TransientSpriteVfx SpawnHit(Vector3 position) =>
        Spawn("HitAccent", position, PrototypeSprites.Circle, Color.white, 0.18f, 0.48f);

    public static TransientSpriteVfx SpawnMelt(Vector3 position) =>
        Spawn("MeltWhiteHotBurst", position, PrototypeSprites.Circle,
            new Color(1f, 0.95f, 0.72f, 1f), 0.38f, 1.1f);

    public static TransientSpriteVfx SpawnDeath(Vector3 position) =>
        Spawn("DeathAccent", position, PrototypeSprites.Square,
            new Color(0.71f, 0.51f, 0.28f, 1f), 0.32f, 0.82f);

    static TransientSpriteVfx Spawn(
        string name, Vector3 position, Sprite sprite, Color color, float lifetime, float size)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        var effect = go.AddComponent<TransientSpriteVfx>();
        effect.Initialize(sprite, color, lifetime, size);
        return effect;
    }
}
