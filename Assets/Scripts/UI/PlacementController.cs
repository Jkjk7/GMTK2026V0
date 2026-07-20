using UnityEngine;

/// <summary>
/// 放置控制器。
/// 职责：左键点空格放置；R 旋转收束器；右键 / X 拆除模块回手牌。
/// 不处理 UI 点击（手牌由 EventSystem 处理）；只在非 UI 区域做棋盘射线。
/// </summary>
public class PlacementController : MonoBehaviour
{
    GridBoard _board;
    HandController _hand;
    Transform _moduleRoot;
    GameSession _session;
    Camera _gameplayCamera;
    GameSkin _skin;
    GridCoord? _hoveredCell;
    GridCellView _hoveredCellView;

    /// <summary>放置收束器时的预览朝向 0..3。</summary>
    int _previewOrientation;

    RedirectorModule _ghostRedirector;
    ProjectileModule _ghostProjectile;
    ModuleType? _ghostType;

    public void Initialize(
        GridBoard board,
        HandController hand,
        Transform moduleRoot,
        GameSession session,
        Camera gameplayCamera = null,
        GameSkin skin = null)
    {
        _board = board;
        _hand = hand;
        _moduleRoot = moduleRoot;
        _session = session;
        _gameplayCamera = gameplayCamera != null ? gameplayCamera : Camera.main;
        _skin = skin;
        _previewOrientation = 0;
    }

    void Update()
    {
        if (_board == null || _hand == null)
        {
            return;
        }

        if (_session != null && !_session.IsPlaying)
        {
            ClearGhost();
            return;
        }

        if (IsPointerOverUi())
        {
            ClearGhost();
            return;
        }

        HandleRotationInput();
        UpdateGhost();

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceAtMouse();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.X))
        {
            TryDismantleAtMouse();
        }
    }

    void HandleRotationInput()
    {
        if (!Input.GetKeyDown(KeyCode.R))
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
            ClearCellHover();
            return;
        }

        ModuleType type = _hand.SelectedModuleType;
        EnsureGhost(type);

        Vector3 mouseWorld = GetMouseWorld();
        if (_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            bool canPlace = _board.CanPlace(cell);
            Vector3 pos = _board.CellToWorld(cell);
            ShowGhostAt(pos, canPlace);
            UpdateCellHover(cell, canPlace);
        }
        else
        {
            HideGhost();
            ClearCellHover();
        }
    }

    void ShowGhostAt(Vector3 pos, bool valid)
    {
        Color tint = valid
            ? new Color(0.35f, 0.95f, 0.55f, 0.55f)
            : new Color(0.95f, 0.3f, 0.3f, 0.55f);

        if (_ghostType == ModuleType.Redirector && _ghostRedirector != null)
        {
            _ghostRedirector.gameObject.SetActive(true);
            _ghostRedirector.transform.position = pos;
            _ghostRedirector.SetOrientation(_previewOrientation);
            SetGhostTint(_ghostRedirector.gameObject, tint);
        }
        else if (_ghostType == ModuleType.Projectile && _ghostProjectile != null)
        {
            _ghostProjectile.gameObject.SetActive(true);
            _ghostProjectile.transform.position = pos;
            SetGhostTint(_ghostProjectile.gameObject, tint);
        }
    }

    void HideGhost()
    {
        if (_ghostRedirector != null)
        {
            _ghostRedirector.gameObject.SetActive(false);
        }

        if (_ghostProjectile != null)
        {
            _ghostProjectile.gameObject.SetActive(false);
        }
    }

    void UpdateCellHover(GridCoord cell, bool valid)
    {
        if (_hoveredCell.HasValue && _hoveredCell.Value.Equals(cell) && _hoveredCellView != null)
        {
            if (valid)
            {
                _hoveredCellView.SetValid();
            }
            else
            {
                _hoveredCellView.SetInvalid();
            }

            return;
        }

        ClearCellHover();
        ModuleBase module = _board.GetModule(cell);
        // 格子视觉在 Cells 子物体上
        Transform cells = _board.transform.Find("Cells");
        if (cells == null)
        {
            return;
        }

        Transform cellTf = cells.Find($"Cell_{cell.Col}_{cell.Row}");
        if (cellTf == null)
        {
            return;
        }

        _hoveredCellView = cellTf.GetComponent<GridCellView>();
        _hoveredCell = cell;
        if (_hoveredCellView == null)
        {
            return;
        }

        if (module != null || !valid)
        {
            _hoveredCellView.SetInvalid();
        }
        else
        {
            _hoveredCellView.SetValid();
        }
    }

    void ClearCellHover()
    {
        if (_hoveredCellView != null)
        {
            _hoveredCellView.SetNormal();
            _hoveredCellView = null;
        }

        _hoveredCell = null;
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

    void TryDismantleAtMouse()
    {
        // 预留：将来拆除扣金币
        if (_hand.IsFull)
        {
            Debug.Log("[Placement] 手牌已满，无法拆除。");
            return;
        }

        Vector3 mouseWorld = GetMouseWorld();
        if (!_board.TryWorldToCell(mouseWorld, out GridCoord cell))
        {
            return;
        }

        if (!_board.TryRemoveModule(cell, out ModuleType moduleType))
        {
            return;
        }

        if (!_hand.TryAddCard(moduleType))
        {
            Debug.LogWarning("[Placement] 拆除后无法回手牌，模块已移除。");
        }
    }

    Vector3 GetMouseWorld()
    {
        Camera cam = _gameplayCamera != null ? _gameplayCamera : Camera.main;
        if (cam == null)
        {
            return Vector3.zero;
        }

        Vector3 screen = Input.mousePosition;
        screen.z = -cam.transform.position.z;
        Vector3 world = cam.ScreenToWorldPoint(screen);
        world.z = 0f;
        return world;
    }

    static bool IsPointerOverUi()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
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
        ClearCellHover();
    }

    static void SetGhostTint(GameObject root, Color tint)
    {
        foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = tint;
        }
    }
}
