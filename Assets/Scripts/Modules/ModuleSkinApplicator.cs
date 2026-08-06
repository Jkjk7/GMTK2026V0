using UnityEngine;

/// <summary>Persistently replaces prototype module bodies with formal countdown art.</summary>
public sealed class ModuleSkinApplicator : MonoBehaviour
{
    const string LegacyDetailName = "CountdownClockwork";

    ModuleBase _module;
    SpriteRenderer _body;
    Sprite _formalSprite;

    public static bool HasStyle(ModuleType type) =>
        System.Enum.IsDefined(typeof(ModuleType), type);

    public static bool Apply(ModuleBase module)
    {
        if (module == null)
        {
            return false;
        }

        // 烈焰墙用红色三角原型，不套用正式贴图
        if (module.ModuleType == ModuleType.FlameWall)
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

        _formalSprite = CountdownArtResources.LoadModuleSprite(module.ModuleType);
        RemoveLegacyDecoration(module.transform.Find(LegacyDetailName));
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (_module == null || _body == null)
        {
            return;
        }

        if (_formalSprite == null)
        {
            _formalSprite = CountdownArtResources.LoadModuleSprite(_module.ModuleType);
        }

        _body.sprite = _formalSprite;
        _body.sortingOrder = 10;
        Color accent = ModuleCatalog.GetDisplayColor(_module.ModuleType);
        Color restrained = Color.Lerp(Color.white, accent, 0.18f);
        restrained.a = 1f;
        _body.color = restrained;
    }

    void LateUpdate()
    {
        if (_module == null || _body == null)
        {
            return;
        }

        Color accent = ModuleCatalog.GetDisplayColor(_module.ModuleType);
        Color expected = Color.Lerp(Color.white, accent, 0.18f);
        expected.a = 1f;
        if (_body.sprite != _formalSprite || _body.color != expected)
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
