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

        image.sprite = CountdownArtResources.LoadModuleSprite(type);
        image.color = new Color(1f, 1f, 1f, disabled ? DisabledAlpha : 1f);
        image.preserveAspect = true;
        image.raycastTarget = false;
    }
}
