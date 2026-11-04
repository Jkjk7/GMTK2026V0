using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual-only checks. Run with -executeMethod PixelIconRegressionChecks.Run.</summary>
public static class PixelIconRegressionChecks
{
    public static void Run()
    {
        try
        {
            Verify();
            Debug.Log("[Pixel Icon Regression] PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    public static void Verify()
    {
        bool original = CountdownArtResources.UseFormalArt;
        var go = new GameObject("PixelIconRegression");
        try
        {
            CountdownArtResources.UseFormalArt = true;
            Image image = go.AddComponent<Image>();
            var hashes = new HashSet<ulong>();
            Texture2D atlas = null;
            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
            {
                Sprite sprite = CountdownArtResources.LoadModuleSprite(type);
                Require(CountdownArtResources.IsFormalModuleSprite(sprite), type + " uses a prototype.");
                Require(sprite == CountdownArtResources.LoadModuleSprite(type), type + " is not cached.");
                Require(sprite.texture.filterMode == FilterMode.Point, type + " has blurred filtering.");
                Require(sprite.texture.mipmapCount == 1, type + " has mipmap blur.");
                Require(sprite.rect.width == 32 && sprite.rect.height == 32, type + " is not 32px.");
                Require(Mathf.Approximately(sprite.bounds.size.x, 1f)
                    && Mathf.Approximately(sprite.bounds.size.y, 1f), type + " has padded world bounds.");
                Require(sprite.pivot == new Vector2(16f, 16f), type + " has an off-center pivot.");
                if (atlas == null) atlas = sprite.texture;
                Require(sprite.texture == atlas, type + " is outside the shared atlas.");
                Require(hashes.Add(PixelHash(sprite)), type + " reuses another icon's pixels.");
                ModuleIconVisuals.Apply(image, type);
                Require(image.sprite == sprite && image.color == Color.white, type + " UI tint/art mismatch.");
                ModuleIconVisuals.Apply(image, type, true);
                Require(Mathf.Approximately(image.color.a, ModuleIconVisuals.DisabledAlpha), type + " disabled alpha mismatch.");
            }
            Require(hashes.Count == 27, "Expected 27 distinct module icons.");
            foreach (Color32 color in atlas.GetPixels32())
                Require(color.a == 0 || color.a == 255, "Atlas contains antialiased pixels.");

            CheckPlacedSkin<FlameWallModule>(ModuleType.FlameWall);
            CheckPlacedSkin<SparkModule>(ModuleType.Spark);
            CheckOrientationAndGhost();

            CountdownArtResources.UseFormalArt = false;
            foreach (ModuleType type in Enum.GetValues(typeof(ModuleType)))
                Require(CountdownArtResources.LoadModuleSprite(type) == PrototypeSprites.Square,
                    type + " does not respect explicit prototype mode.");
        }
        finally
        {
            CountdownArtResources.UseFormalArt = original;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    static void CheckPlacedSkin<T>(ModuleType type) where T : ModuleBase
    {
        var go = new GameObject("PixelSkin_" + type);
        try
        {
            T module = go.AddComponent<T>();
            module.ApplyCardData(ModuleCardData.Create(type, 1, 0));
            Require(ModuleSkinApplicator.Apply(module), type + " did not accept formal art.");
            module.RefreshVisual();
            go.GetComponent<ModuleSkinApplicator>().RefreshNow();
            Require(go.GetComponent<SpriteRenderer>().sprite == PixelArtSprites.Module(type),
                type + " gameplay refresh replaced its pixel skin.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static void CheckOrientationAndGhost()
    {
        var go = new GameObject("PixelDirectionalGhost");
        try
        {
            RedirectorModule module = go.AddComponent<RedirectorModule>();
            ModuleSkinApplicator.Apply(module);
            ModuleSkinApplicator skin = go.GetComponent<ModuleSkinApplicator>();
            SpriteRenderer body = go.GetComponent<SpriteRenderer>();
            for (int i = 0; i < 4; i++)
            {
                module.SetOrientationIndex(i);
                skin.RefreshNow();
                Require(body.sprite == PixelArtSprites.Module(ModuleType.Redirector, i), "Bend icon lost its orientation.");
            }
            module.enabled = false;
            Color ghostTint = new Color(1f, 0.2f, 0.2f, 0.45f);
            body.color = ghostTint;
            skin.RefreshNow();
            Require(body.color == ghostTint, "Formal skin overwrote placement validity/ghost alpha.");
            module.enabled = true;
            skin.RefreshNow();
            Require(body.color == ghostTint, "Enabled redirector ghost lost its validity tint.");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    static ulong PixelHash(Sprite sprite)
    {
        Color32[] pixels = sprite.texture.GetPixels32();
        Rect rect = sprite.rect;
        ulong hash = 14695981039346656037UL;
        unchecked
        {
            for (int y = (int)rect.y; y < rect.yMax; y++)
                for (int x = (int)rect.x; x < rect.xMax; x++)
                {
                    Color32 c = pixels[y * sprite.texture.width + x];
                    hash = (hash ^ c.r) * 1099511628211UL;
                    hash = (hash ^ c.g) * 1099511628211UL;
                    hash = (hash ^ c.b) * 1099511628211UL;
                    hash = (hash ^ c.a) * 1099511628211UL;
                }
        }
        return hash;
    }

    static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
