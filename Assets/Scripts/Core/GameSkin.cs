using UnityEngine;

/// <summary>
/// 统一美术配置。正式 Sprite 存在则使用，否则回退 PrototypeSprites。
/// </summary>
[CreateAssetMenu(menuName = "Game/Game Skin", fileName = "GameSkin")]
public sealed class GameSkin : ScriptableObject
{
    [Header("Board")]
    public Sprite boardBackground;
    public Sprite normalCell;
    public Sprite hoveredCell;
    public Sprite validCell;
    public Sprite invalidCell;

    [Header("Modules")]
    public Sprite redirectorIcon;
    public Sprite projectileIcon;

    [Header("UI")]
    public Sprite panelBackground;
    public Sprite slotNormal;
    public Sprite slotSelected;
    public Sprite slotEmpty;
    public Sprite lifeActive;
    public Sprite lifeLost;

    [Header("World")]
    public Sprite energyBall;
    public Sprite emitterBody;

    public static GameSkin LoadOrCreateRuntime()
    {
        GameSkin skin = Resources.Load<GameSkin>("Game/GameSkin");
        if (skin != null)
        {
            return skin;
        }

        skin = CreateInstance<GameSkin>();
        skin.name = "GameSkin_RuntimeFallback";
        return skin;
    }

    public Sprite ResolveSquare(Sprite preferred) =>
        preferred != null ? preferred : PrototypeSprites.Square;

    public Sprite ResolveCircle(Sprite preferred) =>
        preferred != null ? preferred : PrototypeSprites.Circle;

    public Sprite GetModuleIcon(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
                return ResolveSquare(redirectorIcon);
            case ModuleType.Projectile:
                return ResolveSquare(projectileIcon);
            default:
                return PrototypeSprites.Square;
        }
    }

    public Sprite GetSlotBackground(bool selected, bool empty)
    {
        if (selected && slotSelected != null)
        {
            return slotSelected;
        }

        if (empty && slotEmpty != null)
        {
            return slotEmpty;
        }

        return ResolveSquare(slotNormal);
    }
}
