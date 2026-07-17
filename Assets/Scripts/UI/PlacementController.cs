using UnityEngine;

/// <summary>
/// 放置控制器。
/// 职责：在选中手牌时，左键点空格放置；R / 右键旋转收束器预览朝向。
/// 不处理 UI 点击（手牌由 EventSystem 处理）；只在非 UI 区域做棋盘射线。
/// </summary>
public class PlacementController : MonoBehaviour
{
    GridBoard _board;
    HandController _hand;
    Transform _moduleRoot;

    /// <summary>放置收束器时的预览朝向 0..3。</summary>
    int _previewOrientation;

    /// <summary>可选：跟随鼠标的半透明预览物体。</summary>
    RedirectorModule _ghostRedirector;
    ProjectileModule _ghostProjectile;
    ModuleType? _ghostType;

    /// <summary>
    /// 注入依赖。
    /// </summary>
    public void Initialize(GridBoard board, HandController hand, Transform moduleRoot)
    {
        _board = board;
        _hand = hand;
        _moduleRoot = moduleRoot;
        _previewOrientation = 0;
    }

    void Update()
    {
        if (_board == null || _hand == null)
        {
            return;
        }

        HandleRotationInput();
        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
        {
            // 点在 UI 上时不放置
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            TryPlaceAtMouse();
        }
    }

    void HandleRotationInput()
    {
        bool rotate = Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1);
        if (!rotate)
        {
            return;
        }

        _previewOrientation = (_previewOrientation + 1) % 4;
        if (_ghostRedirector != null && _ghostType == ModuleType.Redirector)
        {
            _ghostRedirector.SetOrientation(_previewOrientation);
        }
    }

    void UpdateGhost()
    {
        if (!_hand.HasSelection)
        {
            ClearGhost();
            return;
        }

        ModuleType type = _hand.SelectedModuleType;
        EnsureGhost(type);

        Vector3 mouseWorld = GetMouseWorld();
        if (_board.TryWorldToCell(mouseWorld, out GridCoord cell) && _board.CanPlace(cell))
        {
            Vector3 pos = _board.CellToWorld(cell);
            if (_ghostType == ModuleType.Redirector && _ghostRedirector != null)
            {
                _ghostRedirector.gameObject.SetActive(true);
                _ghostRedirector.transform.position = pos;
                _ghostRedirector.SetOrientation(_previewOrientation);
                SetGhostAlpha(_ghostRedirector.gameObject, 0.45f);
            }
            else if (_ghostType == ModuleType.Projectile && _ghostProjectile != null)
            {
                _ghostProjectile.gameObject.SetActive(true);
                _ghostProjectile.transform.position = pos;
                SetGhostAlpha(_ghostProjectile.gameObject, 0.45f);
            }
        }
        else
        {
            if (_ghostRedirector != null) _ghostRedirector.gameObject.SetActive(false);
            if (_ghostProjectile != null) _ghostProjectile.gameObject.SetActive(false);
        }
    }

    void TryPlaceAtMouse()
    {
        if (!_hand.HasSelection)
        {
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            return;
        }

        if (!_board.CanPlace(cell))
        {
            return;
        }

        ModuleType type = _hand.SelectedModuleType;
        ModuleBase module = CreateModule(type);
        if (module == null)
        {
            return;
        }

        if (module is RedirectorModule redirector)
        {
            redirector.SetOrientation(_previewOrientation);
        }

        module.transform.SetParent(_moduleRoot, true);
        if (!_board.TryPlaceModule(cell, module))
        {
            Destroy(module.gameObject);
            return;
        }

        _hand.ConsumeSelected();
        ClearGhost();
    }

    ModuleBase CreateModule(ModuleType type)
    {
        switch (type)
        {
            case ModuleType.Redirector:
            {
                var go = new GameObject("Redirector");
                return go.AddComponent<RedirectorModule>();
            }
            case ModuleType.Projectile:
            {
                var go = new GameObject("ProjectileTurret");
                return go.AddComponent<ProjectileModule>();
            }
            default:
                return null;
        }
    }

    void EnsureGhost(ModuleType type)
    {
        if (_ghostType == type)
        {
            return;
        }

        ClearGhost();
        _ghostType = type;
        if (type == ModuleType.Redirector)
        {
            var go = new GameObject("GhostRedirector");
            go.transform.SetParent(transform, false);
            _ghostRedirector = go.AddComponent<RedirectorModule>();
            // 禁用逻辑：预览不应吸能；Redirector 无 Update 逻辑，仅视觉即可
            go.SetActive(false);
        }
        else
        {
            var go = new GameObject("GhostProjectile");
            go.transform.SetParent(transform, false);
            _ghostProjectile = go.AddComponent<ProjectileModule>();
            // 关闭开火：禁用组件 Update
            _ghostProjectile.enabled = false;
            go.SetActive(false);
        }
    }

    void ClearGhost()
    {
        if (_ghostRedirector != null)
        {
            Destroy(_ghostRedirector.gameObject);
            _ghostRedirector = null;
        }

        if (_ghostProjectile != null)
        {
            Destroy(_ghostProjectile.gameObject);
            _ghostProjectile = null;
        }

        _ghostType = null;
    }

    static Vector3 GetMouseWorld()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(screen);
    }

    static void SetGhostAlpha(GameObject root, float alpha)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }
}
