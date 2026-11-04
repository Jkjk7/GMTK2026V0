using UnityEngine;

/// <summary>Persistently replaces prototype module bodies with formal countdown art.</summary>
public sealed class ModuleSkinApplicator : MonoBehaviour
{
    const string LegacyDetailName = "CountdownClockwork";

    ModuleBase _module;
    SpriteRenderer _body;
    Sprite _formalSprite;
    int _orientation;

    public static bool HasStyle(ModuleType type) =>
        PixelArtSprites.HasModule(type);

    public static bool Apply(ModuleBase module)
    {
        if (module == null)
        {
            return false;
        }

        // 原型模式：去掉覆盖组件，让各模块自己的方块/色块视觉生效
        if (!CountdownArtResources.UseFormalArt)
        {
            ModuleSkinApplicator existing = module.GetComponent<ModuleSkinApplicator>();
            if (existing != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(existing);
                }
                else
                {
                    Object.DestroyImmediate(existing);
                }
            }

            module.RefreshVisual();
            return false;
        }

        ModuleSkinApplicator controller = module.GetComponent<ModuleSkinApplicator>();
        if (controller == null)
        {
            controller = module.gameObject.AddComponent<ModuleSkinApplicator>();
        }

        controller.Bind(module);
        return CountdownArtResources.IsFormalModuleSprite(controller._formalSprite);
    }

    void Bind(ModuleBase module)
    {
        _module = module;
        _body = module.GetComponent<SpriteRenderer>();
        if (_body == null)
        {
            _body = module.gameObject.AddComponent<SpriteRenderer>();
        }

        _orientation = GetVisualOrientation();
        _formalSprite = PixelArtSprites.Module(module.ModuleType, _orientation);
        _body.color = Color.white;
        RemoveLegacyDecoration(module.transform.Find(LegacyDetailName));
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (_module == null || _body == null)
        {
            return;
        }

        int orientation = GetVisualOrientation();
        if (_formalSprite == null || _orientation != orientation)
        {
            _orientation = orientation;
            _formalSprite = PixelArtSprites.Module(_module.ModuleType, orientation);
        }

        _body.sprite = _formalSprite;
        _body.sortingOrder = 10;
        // PlacementController owns translucent green/red validity tints. Its
        // redirector ghost remains enabled, unlike the other ghost modules.
        if (OwnsBodyTint()) _body.color = Color.white;
    }

    bool OwnsBodyTint() => _module.enabled && _body.color.a >= 0.999f;

    int GetVisualOrientation()
    {
        // Keep path arms visible: they also communicate bent card connectivity.
        return _module.ModuleType == ModuleType.Redirector || _module.ModuleType == ModuleType.Splitter
            ? _module.OrientationIndex : 0;
    }

    void LateUpdate()
    {
        if (_module == null || _body == null) return;
        if (_body.sprite != _formalSprite || GetVisualOrientation() != _orientation
            || (OwnsBodyTint() && _body.color != Color.white))
        {
            RefreshNow();
        }
    }
    static void RemoveLegacyDecoration(Transform legacy)
    {
        if (legacy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(legacy.gameObject);
        }
        else
        {
            DestroyImmediate(legacy.gameObject);
        }
    }
}
