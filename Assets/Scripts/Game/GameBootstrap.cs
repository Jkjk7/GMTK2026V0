using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 原型启动器：组装画面分区、棋盘、发射器、手牌、商店、波次战斗与 HUD。
/// 布局目标：顶部战斗窗 + 中央棋盘窗 + 右侧手牌/商店栏，外框可透过透明区域看到世界对象。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] float cellSize = 1f;

    static bool s_autoSpawnChecked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawnIfMissing()
    {
        if (s_autoSpawnChecked)
        {
            return;
        }

        s_autoSpawnChecked = true;
        if (FindObjectOfType<GameBootstrap>() != null)
        {
            return;
        }

        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }

    void Awake()
    {
        BuildPrototype();
    }

    void BuildPrototype()
    {
        SetupCamera();
        EnsureEventSystem();

        float boardWorldWidth = GridBoard.Width * cellSize;
        // 棋盘略偏左，给右侧栏留出世界空间余量（相机全屏后由 FitCamera 处理）
        Vector2 boardOrigin = new Vector2(-boardWorldWidth * 0.5f + cellSize * 0.5f - 0.6f, -2.6f);

        var boardGo = new GameObject("GridBoard");
        var board = boardGo.AddComponent<GridBoard>();
        board.Initialize(boardOrigin, cellSize);

        var modulesRoot = new GameObject("Modules").transform;
        modulesRoot.SetParent(boardGo.transform, false);

        var ballMgrGo = new GameObject("EnergyBallManager");
        var ballManager = ballMgrGo.AddComponent<EnergyBallManager>();
        ballManager.Initialize(board);

        var emitterGo = new GameObject("Emitter");
        var emitter = emitterGo.AddComponent<Emitter>();

        Bounds boardBounds = board.GetWorldBounds();

        var sessionGo = new GameObject("GameSession");
        var session = sessionGo.AddComponent<GameSession>();

        emitter.Initialize(board, ballManager, session);

        var laneGo = new GameObject("BattleLane");
        var lane = laneGo.AddComponent<BattleLane>();
        lane.Initialize(boardBounds, cellSize);

        var mageGo = new GameObject("Mage");
        var mage = mageGo.AddComponent<Mage>();

        var enemyRoot = new GameObject("Enemies").transform;

        var wavesGo = new GameObject("WaveManager");
        var waveManager = wavesGo.AddComponent<WaveManager>();

        mage.Initialize(lane.GetEndPosition(), waveManager, session);
        waveManager.Initialize(lane, mage, session, enemyRoot);

        CreateBattleBackdrop(boardBounds, lane.LaneY);

        var trackerGo = new GameObject("DamageTracker");
        var tracker = trackerGo.AddComponent<DamageTracker>();

        Font font = ResolveUiFont();
        Canvas canvas = CreateCanvas();

        CreateGameShell(canvas.transform);

        Text statusLabel = CreateCombatStatusLabel(canvas.transform, font);
        Text overlayLabel = CreateOverlayLabel(canvas.transform, font);

        var hudGo = new GameObject("CombatHUD");
        var combatHud = hudGo.AddComponent<CombatHUD>();
        combatHud.Initialize(statusLabel, overlayLabel, mage, waveManager, session, tracker);

        // 右侧栏：上手牌、下商店
        Transform sidebar = CreateSidebar(canvas.transform, font);
        HandController hand = CreateHand(sidebar, font);
        CreateShopPanel(sidebar, font, hand, session);

        var placementGo = new GameObject("PlacementController");
        var placement = placementGo.AddComponent<PlacementController>();
        placement.Initialize(board, hand, modulesRoot, session);

        CreateHintLabel(canvas.transform, font);
        FitCameraToLayout(boardBounds, lane.LaneY);

        ValidateRedirectorTable();
        Debug.Log("[GameBootstrap] UI 布局：顶部战斗窗 + 棋盘窗 + 右侧手牌/商店。");
    }

    static void ValidateRedirectorTable()
    {
        bool ok = RedirectorModule.TryGetExitDirection(0, GridDirection.Left, out GridDirection exit)
                  && exit == GridDirection.Up;
        ok &= RedirectorModule.TryGetExitDirection(0, GridDirection.Up, out exit)
              && exit == GridDirection.Left;
        ok &= !RedirectorModule.TryGetExitDirection(0, GridDirection.Right, out _);
        ok &= RedirectorModule.TryGetExitDirection(1, GridDirection.Up, out exit) && exit == GridDirection.Right;
        ok &= RedirectorModule.TryGetExitDirection(2, GridDirection.Right, out exit) && exit == GridDirection.Down;
        ok &= RedirectorModule.TryGetExitDirection(3, GridDirection.Down, out exit) && exit == GridDirection.Left;

        if (!ok)
        {
            Debug.LogError("[GameBootstrap] Redirector orientation table validation FAILED.");
        }
        else
        {
            Debug.Log("[GameBootstrap] Redirector orientation table OK.");
        }
    }

    static Font ResolveUiFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        }

        return font;
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
        }

        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.06f, 0.07f, 0.1f, 1f);
        // 文档要求：主 Camera 保持全屏，不要用 Camera.rect 裁掉上方战斗区宽度
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographicSize = 8f;
    }

    /// <summary>
    /// 全屏相机；棋盘+战斗放在左侧可视区，右侧约 22% 留给 UI 侧栏。
    /// </summary>
    void FitCameraToLayout(Bounds boardBounds, float enemyY)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        cam.rect = new Rect(0f, 0f, 1f, 1f);

        float top = Mathf.Max(boardBounds.max.y, enemyY) + 1.4f;
        float bottom = boardBounds.min.y - 1.1f;
        float height = Mathf.Max(0.1f, top - bottom);
        cam.orthographicSize = Mathf.Max(5.8f, height * 0.55f);

        // 视口中心略偏左，让右侧留给侧栏
        float worldHalfWidth = cam.orthographicSize * cam.aspect;
        float targetCenterX = boardBounds.center.x - worldHalfWidth * 0.12f;
        cam.transform.position = new Vector3(
            targetCenterX,
            (top + bottom) * 0.5f,
            -10f);
    }

    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void CreateBattleBackdrop(Bounds boardBounds, float enemyY)
    {
        var go = new GameObject("BattleBackdrop");
        float y = (boardBounds.max.y + enemyY) * 0.5f + 0.3f;
        go.transform.position = new Vector3(boardBounds.center.x, y, 0f);
        go.transform.localScale = new Vector3(boardBounds.size.x + 3.5f, 2.4f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        sr.sortingOrder = -2;
    }

    Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    /// <summary>
    /// 加载透明窗口外框；缺失时用程序化边框兜底，避免不透明图挡住棋盘。
    /// </summary>
    void CreateGameShell(Transform canvas)
    {
        Sprite frameSprite = Resources.Load<Sprite>("UI/ui_game_frame");
        if (frameSprite != null)
        {
            var shellGo = new GameObject("GameShell");
            shellGo.transform.SetParent(canvas, false);

            var rectTransform = shellGo.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;

            var image = shellGo.AddComponent<Image>();
            image.sprite = frameSprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.raycastTarget = false;

            shellGo.transform.SetAsFirstSibling();
            Debug.Log("[GameBootstrap] UI 外框加载成功：Resources/UI/ui_game_frame");
            return;
        }

        Debug.LogWarning(
            "[GameBootstrap] 未找到 Assets/Resources/UI/ui_game_frame.png，使用程序化边框。" +
            "请使用带透明窗口的 PNG（不要用 JPG）。");
        CreateProceduralShell(canvas);
    }

    /// <summary>
    /// 无美术资源时：只画边框与右侧栏底板，中间完全镂空。
    /// </summary>
    void CreateProceduralShell(Transform canvas)
    {
        var root = new GameObject("GameShell");
        root.transform.SetParent(canvas, false);
        var rootRt = root.AddComponent<RectTransform>();
        StretchFull(rootRt);
        root.transform.SetAsFirstSibling();

        Color frame = new Color(0.22f, 0.18f, 0.35f, 0.92f);
        Color accent = new Color(0.55f, 0.4f, 0.85f, 0.55f);
        Color sidebar = new Color(0.08f, 0.07f, 0.12f, 0.88f);

        // 外边框条
        CreateShellBar(root.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, -28f), frame);
        CreateShellBar(root.transform, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 28f), frame);
        CreateShellBar(root.transform, "LeftBar", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(36f, -56f), frame);

        // 战斗区上下分隔线（约顶部 34%）
        CreateShellBar(root.transform, "BattleDivider", new Vector2(0f, 0.66f), new Vector2(0.78f, 0.66f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(0f, 4f), accent);

        // 右侧栏底板（不透明，放手牌/商店）
        CreateShellBar(root.transform, "SidebarPlate", new Vector2(0.78f, 0f), new Vector2(1f, 0.66f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, sidebar);
    }

    static void CreateShellBar(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    /// <summary>
    /// 右侧栏容器：下半屏右侧，上手牌、下商店。
    /// </summary>
    Transform CreateSidebar(Transform canvas, Font font)
    {
        var go = new GameObject("Sidebar");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        // 对应文档：侧边栏约右下 22% × 下 66%
        rt.anchorMin = new Vector2(0.78f, 0.02f);
        rt.anchorMax = new Vector2(0.985f, 0.64f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var header = new GameObject("Header");
        header.transform.SetParent(go.transform, false);
        var headerRt = header.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 1f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.pivot = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -4f);
        headerRt.sizeDelta = new Vector2(0f, 28f);
        var headerText = header.AddComponent<Text>();
        headerText.font = font;
        headerText.fontSize = 18;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.75f, 0.8f, 0.95f, 1f);
        headerText.text = "模块仓";

        return go.transform;
    }

    Text CreateCombatStatusLabel(Transform canvas, Font font)
    {
        var go = new GameObject("CombatStatusLabel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.02f, 0.92f);
        rt.anchorMax = new Vector2(0.55f, 0.99f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 28;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = "准备中…";

        var bgGo = new GameObject("StatusBg");
        bgGo.transform.SetParent(go.transform, false);
        bgGo.transform.SetAsFirstSibling();
        var bgRt = bgGo.AddComponent<RectTransform>();
        StretchFull(bgRt);
        bgRt.offsetMin = new Vector2(-8f, -4f);
        bgRt.offsetMax = new Vector2(8f, 4f);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;
        return text;
    }

    Text CreateOverlayLabel(Transform canvas, Font font)
    {
        var go = new GameObject("ResultOverlay");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(go.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        StretchFull(dimRt);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);
        dim.raycastTarget = true;

        // Text 挂在根节点，CombatHUD SetActive 时整块遮罩一起显示
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = string.Empty;
        text.raycastTarget = false;
        go.SetActive(false);
        return text;
    }

    Text CreateHintLabel(Transform canvas, Font font)
    {
        var go = new GameObject("HintLabel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.01f);
        rt.anchorMax = new Vector2(0.75f, 0.045f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 18;
        text.color = new Color(0.85f, 0.88f, 0.95f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = "商店购入 | 左键放置 | R旋转 | 右键/X拆除 | F刷新";
        return text;
    }

    RectTransform CreateShopPanel(Transform sidebar, Font font, HandController hand, GameSession session)
    {
        var panelGo = new GameObject("ShopPanel");
        panelGo.transform.SetParent(sidebar, false);
        var panel = panelGo.AddComponent<RectTransform>();
        // 侧栏下半：商店
        panel.anchorMin = new Vector2(0.04f, 0.02f);
        panel.anchorMax = new Vector2(0.96f, 0.46f);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = Vector2.zero;

        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.14f, 0.75f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -4f);
        titleRt.sizeDelta = new Vector2(0f, 26f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.text = "商店 (F刷新)";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.7f, 0.7f, 0.8f);
        title.fontSize = 15;

        var listGo = new GameObject("Slots");
        listGo.transform.SetParent(panelGo.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(8f, 8f);
        listRt.offsetMax = new Vector2(-8f, -32f);

        var grid = listGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 72f);
        grid.spacing = new Vector2(8f, 8f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        var shop = panelGo.AddComponent<ShopController>();
        var slots = new ShopSlot[ShopController.SlotCount];
        for (int i = 0; i < ShopController.SlotCount; i++)
        {
            slots[i] = CreateShopSlot(listGo.transform, font, shop, i);
        }

        shop.Initialize(hand, slots, session);
        return panel;
    }

    ShopSlot CreateShopSlot(Transform parent, Font font, ShopController shop, int index)
    {
        var slotGo = new GameObject($"ShopSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        var rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(96f, 72f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.55f);
        iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.sizeDelta = new Vector2(28f, 28f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.gray;
        icon.raycastTarget = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 4f);
        labelRt.sizeDelta = new Vector2(0f, 20f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.text = "空";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        label.fontSize = 12;
        label.raycastTarget = false;

        var slot = slotGo.AddComponent<ShopSlot>();
        slot.Setup(shop, index, bg, icon, label);
        return slot;
    }

    HandController CreateHand(Transform sidebar, Font font)
    {
        var handGo = new GameObject("Hand");
        handGo.transform.SetParent(sidebar, false);
        var handRt = handGo.AddComponent<RectTransform>();
        // 侧栏上半：手牌
        handRt.anchorMin = new Vector2(0.04f, 0.48f);
        handRt.anchorMax = new Vector2(0.96f, 0.94f);
        handRt.offsetMin = Vector2.zero;
        handRt.offsetMax = Vector2.zero;

        var bg = handGo.AddComponent<Image>();
        bg.color = new Color(0.09f, 0.09f, 0.12f, 0.8f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(handGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -4f);
        titleRt.sizeDelta = new Vector2(0f, 24f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.text = "手牌";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.7f, 0.75f, 0.85f);
        title.fontSize = 15;
        title.raycastTarget = false;

        var row = new GameObject("Slots");
        row.transform.SetParent(handGo.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.offsetMin = new Vector2(8f, 8f);
        rowRt.offsetMax = new Vector2(-8f, -30f);

        var grid = row.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96f, 88f);
        grid.spacing = new Vector2(8f, 8f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        var hand = handGo.AddComponent<HandController>();
        var slots = new HandSlot[HandController.SlotCount];
        for (int i = 0; i < HandController.SlotCount; i++)
        {
            slots[i] = CreateHandSlot(row.transform, font, hand, i);
        }

        hand.BindSlots(slots);
        hand.ClearHand();
        return hand;
    }

    HandSlot CreateHandSlot(Transform parent, Font font, HandController hand, int index)
    {
        var slotGo = new GameObject($"HandSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        var rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(96f, 88f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.2f, 0.95f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.55f);
        iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.sizeDelta = new Vector2(36f, 36f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.gray;
        icon.raycastTarget = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 4f);
        labelRt.sizeDelta = new Vector2(0f, 20f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 13;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "空";
        label.raycastTarget = false;

        var slot = slotGo.AddComponent<HandSlot>();
        slot.Setup(hand, index, bg, icon, label);
        return slot;
    }
}
