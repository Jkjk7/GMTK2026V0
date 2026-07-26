using UnityEngine;

/// <summary>Adds clockwork readability without replacing module gameplay renderers.</summary>
public sealed class ModuleSkinApplicator : MonoBehaviour
{
    const string DetailName = "CountdownClockwork";
    static readonly Color Ink = Hex("#0B0910");
    static readonly Color Brass = Hex("#B58248");
    static readonly Color Gold = Hex("#FFD676");

    public static bool HasStyle(ModuleType type) =>
        System.Enum.IsDefined(typeof(ModuleType), type);

    public static void Apply(ModuleBase module)
    {
        if (module == null || module.transform.Find(DetailName) != null) return;

        Color typeColor = GetTypeColor(module.ModuleType);
        Transform root = new GameObject(DetailName).transform;
        root.SetParent(module.transform, false);
        Add(root, "Face", PrototypeSprites.Circle, Vector3.zero, Vector3.one * 0.56f, typeColor, 15);
        float angle = ((int)module.ModuleType * 37f) % 360f;
        Add(root, "Hand", PrototypeSprites.Square, new Vector3(0f, 0.11f, 0f),
            new Vector3(0.07f, 0.32f, 1f), Gold, 16).localRotation =
            Quaternion.Euler(0f, 0f, angle);
        Add(root, "Hub", PrototypeSprites.Circle, Vector3.zero, Vector3.one * 0.12f, Ink, 17);
        if (((int)module.ModuleType & 1) == 0)
        {
            Add(root, "Gear", PrototypeSprites.Circle, new Vector3(0.22f, -0.2f, 0f),
                Vector3.one * 0.18f, Gold, 17);
        }
    }

    static Transform Add(
        Transform parent, string name, Sprite sprite, Vector3 pos,
        Vector3 scale, Color color, int order)
    {
        Transform child = new GameObject(name).transform;
        child.SetParent(parent, false);
        child.localPosition = pos;
        child.localScale = scale;
        var sr = child.gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return child;
    }

    static Color Hex(string value)
    {
        ColorUtility.TryParseHtmlString(value, out Color color);
        return color;
    }

    static Color GetTypeColor(ModuleType type)
    {
        switch ((int)type % 3)
        {
            case 0: return Brass;
            case 1: return Gold;
            default: return Ink;
        }
    }
}
