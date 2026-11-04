using UnityEngine;
using UnityEngine.UI;

/// <summary>Applies the shared formal module art to all UI icon surfaces.</summary>
public static class ModuleIconVisuals
{
    public const float DisabledAlpha = 0.42f;

    public static void Apply(Image image, ModuleType type, bool disabled = false)
    {
        if (image == null)
        {
            return;
        }

        if (!CountdownArtResources.UseFormalArt)
        {
            image.sprite =
                (type == ModuleType.LaserCannon
                 || type == ModuleType.ArcaneMissile
                 || type == ModuleType.Purify
                 || type == ModuleType.FrostMushroom
                 || type == ModuleType.FrostCannon)
                ? PrototypeSprites.Circle
                : PrototypeSprites.Square;
            Color c = ModuleCatalog.GetDisplayColor(type);
            c.a = disabled ? DisabledAlpha : 1f;
            image.color = c;
        }
        else
        {
            Sprite sprite = CountdownArtResources.LoadModuleSprite(type);
            bool prototypeSprite = sprite == null
                || sprite == PrototypeSprites.Square
                || sprite == PrototypeSprites.Circle
                || sprite == PrototypeSprites.Triangle;
            if (prototypeSprite)
            {
                image.sprite = sprite != null
                    ? sprite
                    : (type == ModuleType.LaserCannon || type == ModuleType.ArcaneMissile
                        ? PrototypeSprites.Circle
                        : PrototypeSprites.Square);
                Color c = ModuleCatalog.GetDisplayColor(type);
                c.a = disabled ? DisabledAlpha : 1f;
                image.color = c;
            }
            else
            {
                image.sprite = sprite;
                image.color = new Color(1f, 1f, 1f, disabled ? DisabledAlpha : 1f);
            }
        }

        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}
