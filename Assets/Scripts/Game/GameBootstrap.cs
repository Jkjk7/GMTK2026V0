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

    GameSkin _skin;

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

        Camera worldCam = Camera.main;

        // —— 世界层级（文档 GameLayout 结构）——
        var layoutGo = new GameObject("GameLayout");
        var layout = layoutGo.AddComponent<GameLayoutView>();
        layout.worldCamera = worldCam;

        var worldRoot = new GameObject("WorldRoot").transform;
        worldRoot.SetParent(layoutGo.transform, false);
        layout.worldRoot = worldRoot;

        var boardRoot = new GameObject("BoardRoot").transform;
        boardRoot.SetParent(worldRoot, false);
        // BoardRoot 放在棋盘左下角世界位置；格子用本地坐标
        boardRoot.position = new Vector3(-GridBoard.Width * cellSize * 0.5f, -3.2f, 0f);
        layout.boardRoot = boardRoot;
        layout.gridAnchor = boardRoot;

        var board = boardRoot.gameObject.AddComponent<GridBoard>();
        board.Initialize(cellSize, Vector2.zero);

        layout.moduleRoot = board.ModulesRoot;

        var ballMgrGo = new GameObject("EnergyBallManager");
        ballMgrGo.transform.SetParent(worldRoot, false);
        var ballManager = ballMgrGo.AddComponent<EnergyBallManager>();
        ballManager.Initialize(board);

        var emitterGo = new GameObject("Emitter");
        emitterGo.transform.SetParent(boardRoot, false);
        layout.emitterAnchor = emitterGo.transform;
        var emitter = emitterGo.AddComponent<Emitter>();

        Bounds boardBounds = board.GetWorldBounds();

        // 先按「棋盘窗」对齐相机，再算战斗锚点世界坐标（避免棋盘顶穿进战斗窗）
        FitCameraToBoardWindow(boardBounds);

        var battleRoot = new GameObject("BattleRoot").transform;
        battleRoot.SetParent(worldRoot, false);
        layout.battleRoot = battleRoot;

        float laneY = ViewportToWorldY(worldCam, 0.82f);
        float endX = ViewportToWorldX(worldCam, 0.08f);
        float spawnX = ViewportToWorldX(worldCam, 0.72f);
        // 保证路线覆盖棋盘左右外延
        endX = Mathf.Min(endX, boardBounds.min.x - cellSize * 0.4f);
        spawnX = Mathf.Max(spawnX, boardBounds.max.x + cellSize * 1.2f);

        var spawnAnchor = CreateAnchor(battleRoot, "EnemySpawnAnchor", new Vector3(spawnX, laneY, 0f));
        var endAnchor = CreateAnchor(battleRoot, "EnemyEndAnchor", new Vector3(endX, laneY, 0f));
        var mageAnchor = CreateAnchor(battleRoot, "MageAnchor", new Vector3(endX, laneY, 0f));
        layout.enemySpawnAnchor = spawnAnchor;
        layout.enemyEndAnchor = endAnchor;
        layout.mageAnchor = mageAnchor;

        var enemyRoot = new GameObject("Enemies").transform;
        enemyRoot.SetParent(battleRoot, false);
        layout.enemyRoot = enemyRoot;

        var sessionGo = new GameObject("GameSession");
        sessionGo.transform.SetParent(layoutGo.transform, false);
        var session = sessionGo.AddComponent<GameSession>();

        emitter.Initialize(board, ballManager, session);

        var lane = battleRoot.gameObject.AddComponent<BattleLane>();
        lane.Initialize(spawnAnchor, endAnchor);

        var mageGo = new GameObject("Mage");
        mageGo.transform.SetParent(mageAnchor, false);
        mageGo.transform.localPosition = Vector3.zero;
        var mage = mageGo.AddComponent<Mage>();

        var wavesGo = new GameObject("WaveManager");
        wavesGo.transform.SetParent(battleRoot, false);
        var waveManager = wavesGo.AddComponent<WaveManager>();

        mage.Initialize(mageAnchor.position, waveManager, session);
        // WaveManager.Initialize 延后到解锁池/草稿 UI 就绪后

        CreateBattleBackdrop(battleRoot, boardBounds, laneY, worldCam);

        var trackerGo = new GameObject("DamageTracker");
        trackerGo.transform.SetParent(layoutGo.transform, false);
        var tracker = trackerGo.AddComponent<DamageTracker>();

        Font font = ResolveUiFont();
        Canvas canvas = CreateCanvas();
        layout.canvas = canvas;
        canvas.transform.SetParent(layoutGo.transform, false);

        CreateGameShell(canvas.transform);

        _skin = GameSkin.LoadOrCreateRuntime();

        var audioGo = new GameObject("UIAudioFeedback");
        audioGo.transform.SetParent(layoutGo.transform, false);
        var uiAudio = audioGo.AddComponent<UIAudioFeedback>();
        uiAudio.EnsureSource();

        BuildHudAndWire(
            canvas.transform,
            font,
            layout,
            layoutGo.transform,
            mage,
            waveManager,
            session,
            tracker,
            uiAudio);

        Transform sidebar = CreateSidebar(canvas.transform, font);

        // 经济必须在商店初始化前就绪（订阅金币变化）
        var economyGo = new GameObject("Economy");
        economyGo.transform.SetParent(layoutGo.transform, false);
        economyGo.AddComponent<Economy>();
        economyGo.AddComponent<WaveGoldBudget>();
        economyGo.AddComponent<RunModulePool>();
        var dropService = economyGo.AddComponent<GoldDropService>();
        GoldPanel goldPanel = CreateGoldPanel(canvas.transform, font);
        dropService.Initialize(goldPanel, layoutGo.transform);

        var expandGo = new GameObject("BoardExpand");
        expandGo.transform.SetParent(layoutGo.transform, false);
        var boardExpand = expandGo.AddComponent<BoardExpandService>();
        boardExpand.Initialize(board, BoardExpandService.Size3);
        board.BindExpandService(boardExpand);

        DraftChoiceView draftUi = CreateDraftChoiceView(canvas.transform, font);
        var unlockGo = new GameObject("ModuleUnlockDirector");
        unlockGo.transform.SetParent(layoutGo.transform, false);
        var unlockDirector = unlockGo.AddComponent<ModuleUnlockDirector>();
        unlockDirector.Initialize(RunModulePool.Instance, draftUi);

        waveManager.Initialize(lane, mage, session, enemyRoot, ballManager, unlockDirector);

        HandController hand = CreateHand(sidebar, font, _skin);
        var shop = CreateShopPanel(sidebar, font, hand, session, waveManager, _skin);
        layout.handController = hand;
        layout.shopController = shop;

        Bounds scrapBounds = board.GetWorldBounds();
        var scrapGo = new GameObject("ScrapZone");
        scrapGo.transform.SetParent(worldRoot, false);
        var scrap = scrapGo.AddComponent<ScrapZone>();
        scrap.Initialize(new Vector3(
            scrapBounds.min.x - cellSize * 1.15f,
            scrapBounds.min.y - cellSize * 0.15f,
            0f));

        ConfirmPromptView confirm = CreateConfirmPrompt(canvas.transform, font);
        ModuleTooltipView tooltip = CreateModuleTooltip(canvas.transform, font, _skin);
        PrepPhasePanel prepPanel = CreatePrepPhasePanel(canvas.transform, font, waveManager, session);

        var placementGo = new GameObject("PlacementController");
        placementGo.transform.SetParent(layoutGo.transform, false);
        var placement = placementGo.AddComponent<PlacementController>();
        placement.Initialize(
            board, hand, board.ModulesRoot, session, worldCam, _skin, waveManager, scrap, confirm, tooltip,
            boardExpand);

        CreateHintLabel(canvas.transform, font);
        CreateBoardExpandHint(canvas.transform, font, boardExpand);

        ValidateRedirectorTable();
        Debug.Log("[GameBootstrap] Roguelike15：解锁池 / 点数刷怪 / 棋盘扩展 / 新模块。");
    }

    void BuildHudAndWire(
        Transform canvas,
        Font font,
        GameLayoutView layout,
        Transform layoutRoot,
        Mage mage,
        WaveManager waveManager,
        GameSession session,
        DamageTracker tracker,
        UIAudioFeedback uiAudio)
    {
        Text statusLabel = CreateCombatStatusLabel(canvas, font);
        Text livesLabel = CreateLivesLabel(canvas, font);
        Text breachLabel = CreateBreachLabel(canvas, font);
        Image flash = CreateBreachFlash(canvas);
        ResultOverlayView overlay = CreateResultOverlay(canvas, font);
        layout.resultOverlay = overlay.GetComponent<CanvasGroup>();

        var hudGo = new GameObject("CombatHUD");
        hudGo.transform.SetParent(layoutRoot, false);
        var combatHud = hudGo.AddComponent<CombatHUD>();
        combatHud.Initialize(
            statusLabel,
            livesLabel,
            breachLabel,
            flash,
            overlay,
            mage,
            waveManager,
            session,
            tracker,
            uiAudio);
        layout.combatHud = combatHud;
    }

    static Transform CreateAnchor(Transform parent, string name, Vector3 worldPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        return go.transform;
    }

    static float ViewportToWorldX(Camera cam, float vx)
    {
        if (cam == null)
        {
            return 0f;
        }

        float aspect = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
        return cam.transform.position.x + (vx - 0.5f) * 2f * cam.orthographicSize * aspect;
    }

    static float ViewportToWorldY(Camera cam, float vy)
    {
        if (cam == null)
        {
            return 0f;
        }

        return cam.transform.position.y + (vy - 0.5f) * 2f * cam.orthographicSize;
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
    /// 只把棋盘（含发射器余量）装进棋盘透明窗，顶边不超过分隔线（约 y=0.64）。
    /// 战斗区另用视口锚点放置，避免棋盘顶穿框。
    /// </summary>
    void FitCameraToBoardWindow(Bounds boardBounds)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        cam.rect = new Rect(0f, 0f, 1f, 1f);

        float contentMinX = boardBounds.min.x - cellSize * 1.6f;
        float contentMaxX = boardBounds.max.x + cellSize * 0.5f;
        float contentMinY = boardBounds.min.y - cellSize * 0.45f;
        float contentMaxY = boardBounds.max.y + cellSize * 0.35f;
        float contentW = Mathf.Max(0.1f, contentMaxX - contentMinX);
        float contentH = Mathf.Max(0.1f, contentMaxY - contentMinY);
        float contentCX = (contentMinX + contentMaxX) * 0.5f;
        float contentCY = (contentMinY + contentMaxY) * 0.5f;

        // 与 ui_game_frame 棋盘窗对齐（底栏上沿约 0.03，分隔线下沿约 0.64）
        const float viewX0 = 0.05f;
        const float viewX1 = 0.75f;
        const float viewY0 = 0.06f;
        const float viewY1 = 0.60f;
        float viewW = viewX1 - viewX0;
        float viewH = viewY1 - viewY0;

        float aspect = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
        float sizeFromWidth = contentW / (2f * viewW * aspect);
        float sizeFromHeight = contentH / (2f * viewH);
        float orthoSize = Mathf.Max(sizeFromWidth, sizeFromHeight) * 1.04f;
        cam.orthographicSize = orthoSize;

        float targetVx = (viewX0 + viewX1) * 0.5f;
        float targetVy = (viewY0 + viewY1) * 0.5f;
        float camX = contentCX - (targetVx - 0.5f) * 2f * orthoSize * aspect;
        float camY = contentCY - (targetVy - 0.5f) * 2f * orthoSize;
        cam.transform.position = new Vector3(camX, camY, -10f);
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

    void CreateBattleBackdrop(Transform battleRoot, Bounds boardBounds, float laneY, Camera cam)
    {
        var go = new GameObject("BattleBackdrop");
        go.transform.SetParent(battleRoot, false);
        float left = ViewportToWorldX(cam, 0.04f);
        float right = ViewportToWorldX(cam, 0.96f);
        float width = Mathf.Max(boardBounds.size.x + cellSize * 6f, right - left);
        go.transform.position = new Vector3((left + right) * 0.5f, laneY, 0f);
        go.transform.localScale = new Vector3(width, 2.4f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = PrototypeSprites.Square;
        sr.color = new Color(0.11f, 0.13f, 0.17f, 1f);
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

        Color frame = new Color(0.14f, 0.12f, 0.22f, 0.95f);
        Color accent = new Color(0.35f, 0.65f, 0.9f, 0.7f);
        Color sidebar = new Color(0.07f, 0.06f, 0.11f, 0.92f);

        CreateShellBar(root.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 0f), frame);
        CreateShellBar(root.transform, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f), frame);
        CreateShellBar(root.transform, "LeftBar", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0f, 30f), new Vector2(30f, -30f), frame);
        CreateShellBar(root.transform, "RightBar", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), new Vector2(-30f, 30f), new Vector2(0f, -30f), frame);

        // 战斗 / 棋盘分隔（仅左侧，侧栏上方战斗窗保持贯通）
        CreateShellBar(root.transform, "BattleDivider", new Vector2(0.02f, 0.65f), new Vector2(0.78f, 0.66f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, accent);

        // 右下侧栏底板
        CreateShellBar(root.transform, "SidebarPlate", new Vector2(0.78f, 0.03f), new Vector2(0.985f, 0.64f),
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
        // 与 ui_game_frame 右下侧栏镂空/底板对齐
        rt.anchorMin = new Vector2(0.78f, 0.03f);
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

    Text CreateLivesLabel(Transform canvas, Font font)
    {
        var go = new GameObject("LivesLabel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.56f, 0.92f);
        rt.anchorMax = new Vector2(0.78f, 0.99f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = new Color(0.7f, 0.85f, 1f, 1f);
        text.text = "机会 ◆◆◆";
        return text;
    }

    Text CreateBreachLabel(Transform canvas, Font font)
    {
        var go = new GameObject("BreachWarning");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.72f);
        rt.anchorMax = new Vector2(0.8f, 0.82f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 36;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 0.45f, 0.4f, 1f);
        text.text = string.Empty;
        go.SetActive(false);
        return text;
    }

    Image CreateBreachFlash(Transform canvas)
    {
        var go = new GameObject("BreachFlash");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.8f, 0.1f, 0.1f, 0f);
        img.raycastTarget = false;
        return img;
    }

    ResultOverlayView CreateResultOverlay(Transform canvas, Font font)
    {
        var go = new GameObject("ResultOverlay");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFull(rt);

        var group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var dimGo = new GameObject("Dim");
        dimGo.transform.SetParent(go.transform, false);
        var dimRt = dimGo.AddComponent<RectTransform>();
        StretchFull(dimRt);
        var dim = dimGo.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.6f);
        dim.raycastTarget = true;

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(go.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 0.5f);
        titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.pivot = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(700f, 140f);
        var text = titleGo.AddComponent<Text>();
        text.font = font;
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        var view = go.AddComponent<ResultOverlayView>();
        view.Bind(group, text);
        return view;
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
        text.text = "拖棋盘可移动/合成/分解 | 点灰格扩展棋盘 | 悬停详情 | Space准备 | F刷新";
        return text;
    }

    Text CreateBoardExpandHint(Transform canvas, Font font, BoardExpandService expand)
    {
        var go = new GameObject("BoardExpandHint");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.045f);
        rt.anchorMax = new Vector2(0.45f, 0.075f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 16;
        text.color = new Color(0.7f, 0.85f, 0.75f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;

        void Refresh()
        {
            if (expand == null)
            {
                return;
            }

            int cost = expand.GetNextExpandCost();
            if (cost <= 0)
            {
                text.text = $"棋盘：{expand.UnlockedSize}×{expand.UnlockedSize}（已满）";
            }
            else
            {
                text.text = $"棋盘：{expand.UnlockedSize}×{expand.UnlockedSize} → {expand.GetNextSize()}×{expand.GetNextSize()}（{cost}金，点灰格）";
            }
        }

        Refresh();
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged += _ => Refresh();
        }

        return text;
    }

    DraftChoiceView CreateDraftChoiceView(Transform canvas, Font font)
    {
        var root = new GameObject("DraftChoice");
        root.transform.SetParent(canvas, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.18f, 0.28f);
        rt.anchorMax = new Vector2(0.72f, 0.72f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var group = root.AddComponent<CanvasGroup>();

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.12f, 0.96f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.82f);
        titleRt.anchorMax = new Vector2(0.95f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 22;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(1f, 0.9f, 0.5f, 1f);
        title.raycastTarget = false;

        var buttons = new Button[3];
        var labels = new Text[3];
        float[] x0 = { 0.04f, 0.36f, 0.68f };
        float[] x1 = { 0.32f, 0.64f, 0.96f };
        for (int i = 0; i < 3; i++)
        {
            var btnGo = new GameObject($"Choice{i}");
            btnGo.transform.SetParent(root.transform, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(x0[i], 0.12f);
            brt.anchorMax = new Vector2(x1[i], 0.78f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            buttons[i] = btnGo.AddComponent<Button>();

            var lg = new GameObject("Label");
            lg.transform.SetParent(btnGo.transform, false);
            var lrt = lg.AddComponent<RectTransform>();
            StretchFull(lrt);
            labels[i] = lg.AddComponent<Text>();
            labels[i].font = font;
            labels[i].fontSize = 16;
            labels[i].alignment = TextAnchor.MiddleCenter;
            labels[i].color = Color.white;
            labels[i].raycastTarget = false;
            labels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            labels[i].verticalOverflow = VerticalWrapMode.Overflow;
        }

        var view = root.AddComponent<DraftChoiceView>();
        view.Bind(group, title, buttons, labels);
        return view;
    }

    PrepPhasePanel CreatePrepPhasePanel(Transform canvas, Font font, WaveManager waves, GameSession session)
    {
        var root = new GameObject("PrepPhasePanel");
        root.transform.SetParent(canvas, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.18f, 0.78f);
        rootRt.anchorMax = new Vector2(0.62f, 0.98f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        var group = root.AddComponent<CanvasGroup>();

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.1f, 0.82f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.62f);
        titleRt.anchorMax = new Vector2(0.95f, 0.95f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 26;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.9f, 0.45f, 1f);
        title.text = "准备阶段";

        var timerGo = new GameObject("Timer");
        timerGo.transform.SetParent(root.transform, false);
        var timerRt = timerGo.AddComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0.05f, 0.38f);
        timerRt.anchorMax = new Vector2(0.95f, 0.65f);
        timerRt.offsetMin = Vector2.zero;
        timerRt.offsetMax = Vector2.zero;
        var timer = timerGo.AddComponent<Text>();
        timer.font = font;
        timer.fontSize = 36;
        timer.fontStyle = FontStyle.Bold;
        timer.alignment = TextAnchor.MiddleCenter;
        timer.color = new Color(0.25f, 0.55f, 0.35f, 1f);
        timer.text = "00:20";

        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(root.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.05f, 0.22f);
        hintRt.anchorMax = new Vector2(0.95f, 0.4f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;
        var hint = hintGo.AddComponent<Text>();
        hint.font = font;
        hint.fontSize = 16;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.8f, 0.85f, 0.75f, 1f);
        hint.text = "购买、合成并调整模块";

        var barBgGo = new GameObject("ProgressBg");
        barBgGo.transform.SetParent(root.transform, false);
        var barBgRt = barBgGo.AddComponent<RectTransform>();
        barBgRt.anchorMin = new Vector2(0.1f, 0.12f);
        barBgRt.anchorMax = new Vector2(0.9f, 0.2f);
        barBgRt.offsetMin = Vector2.zero;
        barBgRt.offsetMax = Vector2.zero;
        var barBg = barBgGo.AddComponent<Image>();
        barBg.color = new Color(0.15f, 0.18f, 0.16f, 0.9f);

        var barFillGo = new GameObject("ProgressFill");
        barFillGo.transform.SetParent(barBgGo.transform, false);
        var barFillRt = barFillGo.AddComponent<RectTransform>();
        StretchFull(barFillRt);
        var barFill = barFillGo.AddComponent<Image>();
        barFill.sprite = PrototypeSprites.Square;
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFill.fillAmount = 1f;
        barFill.color = new Color(0.35f, 0.85f, 0.55f, 0.95f);

        var readyGo = new GameObject("ReadyButton");
        readyGo.transform.SetParent(canvas, false);
        var readyRt = readyGo.AddComponent<RectTransform>();
        // 棋盘窗右上角（与 FitCameraToBoardWindow 棋盘区对齐）
        readyRt.anchorMin = new Vector2(0.58f, 0.52f);
        readyRt.anchorMax = new Vector2(0.74f, 0.595f);
        readyRt.offsetMin = Vector2.zero;
        readyRt.offsetMax = Vector2.zero;
        var readyBg = readyGo.AddComponent<Image>();
        readyBg.color = new Color(0.2f, 0.45f, 0.28f, 0.95f);
        var readyBtn = readyGo.AddComponent<Button>();
        var readyLabelGo = new GameObject("Label");
        readyLabelGo.transform.SetParent(readyGo.transform, false);
        var readyLabelRt = readyLabelGo.AddComponent<RectTransform>();
        StretchFull(readyLabelRt);
        var readyLabel = readyLabelGo.AddComponent<Text>();
        readyLabel.font = font;
        readyLabel.fontSize = 16;
        readyLabel.alignment = TextAnchor.MiddleCenter;
        readyLabel.color = Color.white;
        readyLabel.text = "准备完毕 [Space]";
        readyLabel.raycastTarget = false;
        readyGo.SetActive(false);

        // 中央大号倒计时
        var cdGo = new GameObject("CountdownBig");
        cdGo.transform.SetParent(canvas, false);
        var cdRt = cdGo.AddComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(0.3f, 0.45f);
        cdRt.anchorMax = new Vector2(0.55f, 0.7f);
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;
        var cd = cdGo.AddComponent<Text>();
        cd.font = font;
        cd.fontSize = 72;
        cd.fontStyle = FontStyle.Bold;
        cd.alignment = TextAnchor.MiddleCenter;
        cd.color = new Color(1f, 0.85f, 0.3f, 1f);
        cd.text = string.Empty;
        cd.raycastTarget = false;

        var panel = root.AddComponent<PrepPhasePanel>();
        panel.Bind(title, timer, hint, cd, barFill, readyBtn, readyLabel, group, waves, session);
        return panel;
    }

    ModuleTooltipView CreateModuleTooltip(Transform canvas, Font font, GameSkin skin)
    {
        var root = new GameObject("ModuleTooltip");
        root.transform.SetParent(canvas, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(340f, 300f);
        var group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.11f, 0.96f);
        bg.raycastTarget = false;

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.75f, 0.3f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text MakeText(string name, Vector2 aMin, Vector2 aMax, int size, Color color, FontStyle style, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var trt = go.AddComponent<RectTransform>();
            trt.anchorMin = aMin;
            trt.anchorMax = aMax;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(root.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.05f, 0.72f);
        iconRt.anchorMax = new Vector2(0.26f, 0.95f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.sprite = PrototypeSprites.Square;

        Text name = MakeText(
            "Name",
            new Vector2(0.3f, 0.74f), new Vector2(0.95f, 0.95f),
            22, new Color(1f, 0.9f, 0.5f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft);

        // 描述占主要空间
        Text desc = MakeText(
            "Description",
            new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.7f),
            17, new Color(0.92f, 0.94f, 0.96f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

        Text stats = MakeText(
            "Stats",
            new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.4f),
            15, new Color(0.75f, 0.88f, 1f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

        // 留言次要、偏淡
        Text flavor = MakeText(
            "Flavor",
            new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.16f),
            13, new Color(0.55f, 0.58f, 0.62f, 0.95f), FontStyle.Italic, TextAnchor.UpperLeft);

        var view = root.AddComponent<ModuleTooltipView>();
        view.Bind(group, rt, icon, name, desc, stats, flavor, skin);
        return view;
    }

    ConfirmPromptView CreateConfirmPrompt(Transform canvas, Font font)
    {
        var root = new GameObject("ConfirmPrompt");
        root.transform.SetParent(canvas, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.28f, 0.35f);
        rt.anchorMax = new Vector2(0.55f, 0.62f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var group = root.AddComponent<CanvasGroup>();

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        Text MakeLabel(string name, Vector2 aMin, Vector2 aMax, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var lrt = go.AddComponent<RectTransform>();
            lrt.anchorMin = aMin;
            lrt.anchorMax = aMax;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        Text title = MakeLabel("Title", new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f), 20,
            new Color(1f, 0.85f, 0.4f, 1f));
        Text body = MakeLabel("Body", new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.72f), 16,
            new Color(0.9f, 0.9f, 0.92f, 1f));
        Text warn = MakeLabel("Warn", new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.4f), 15,
            new Color(1f, 0.4f, 0.35f, 1f));

        Button MakeBtn(string name, Vector2 aMin, Vector2 aMax, Color bgColor, out Text label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var brt = go.AddComponent<RectTransform>();
            brt.anchorMin = aMin;
            brt.anchorMax = aMax;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            var lg = new GameObject("Label");
            lg.transform.SetParent(go.transform, false);
            var lrt = lg.AddComponent<RectTransform>();
            StretchFull(lrt);
            label = lg.AddComponent<Text>();
            label.font = font;
            label.fontSize = 15;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            return btn;
        }

        Button cancel = MakeBtn("Cancel", new Vector2(0.08f, 0.06f), new Vector2(0.45f, 0.24f),
            new Color(0.3f, 0.3f, 0.35f, 1f), out Text cancelLabel);
        cancelLabel.text = "取消";
        Button confirm = MakeBtn("Confirm", new Vector2(0.55f, 0.06f), new Vector2(0.92f, 0.24f),
            new Color(0.55f, 0.25f, 0.15f, 1f), out Text confirmLabel);
        confirmLabel.text = "确认";

        var view = root.AddComponent<ConfirmPromptView>();
        view.Bind(group, title, body, warn, confirm, cancel, confirmLabel);
        return view;
    }

    GoldPanel CreateGoldPanel(Transform canvas, Font font)
    {
        var go = new GameObject("GoldPanel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        // 棋盘窗右下
        rt.anchorMin = new Vector2(0.58f, 0.07f);
        rt.anchorMax = new Vector2(0.74f, 0.14f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.92f, 0.82f, 0.45f, 0.92f);
        bg.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.75f, 0.55f, 0.12f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        var valueGo = new GameObject("Value");
        valueGo.transform.SetParent(go.transform, false);
        var valueRt = valueGo.AddComponent<RectTransform>();
        valueRt.anchorMin = new Vector2(0.08f, 0.15f);
        valueRt.anchorMax = new Vector2(0.92f, 0.95f);
        valueRt.offsetMin = Vector2.zero;
        valueRt.offsetMax = Vector2.zero;
        var value = valueGo.AddComponent<Text>();
        value.font = font;
        value.fontSize = 28;
        value.fontStyle = FontStyle.Bold;
        value.alignment = TextAnchor.MiddleCenter;
        value.color = new Color(0.35f, 0.2f, 0.08f, 1f);
        value.text = Economy.StartingGold.ToString();
        value.raycastTarget = false;

        var deltaGo = new GameObject("Delta");
        deltaGo.transform.SetParent(go.transform, false);
        var deltaRt = deltaGo.AddComponent<RectTransform>();
        deltaRt.anchorMin = new Vector2(0.05f, 0.75f);
        deltaRt.anchorMax = new Vector2(0.95f, 1.35f);
        deltaRt.offsetMin = Vector2.zero;
        deltaRt.offsetMax = Vector2.zero;
        var delta = deltaGo.AddComponent<Text>();
        delta.font = font;
        delta.fontSize = 18;
        delta.alignment = TextAnchor.MiddleCenter;
        delta.color = new Color(0.25f, 0.45f, 0.15f, 1f);
        delta.text = string.Empty;
        delta.raycastTarget = false;

        var panel = go.AddComponent<GoldPanel>();
        panel.Bind(value, delta, bg, rt);
        return panel;
    }

    ShopController CreateShopPanel(
        Transform sidebar,
        Font font,
        HandController hand,
        GameSession session,
        WaveManager waves,
        GameSkin skin)
    {
        var panelGo = new GameObject("ShopPanel");
        panelGo.transform.SetParent(sidebar, false);
        var panel = panelGo.AddComponent<RectTransform>();
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
        titleRt.anchorMax = new Vector2(0.55f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(8f, -4f);
        titleRt.sizeDelta = new Vector2(0f, 26f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.text = "商店";
        title.alignment = TextAnchor.MiddleLeft;
        title.color = new Color(0.7f, 0.7f, 0.8f);
        title.fontSize = 15;

        var refreshBtnGo = new GameObject("RefreshButton");
        refreshBtnGo.transform.SetParent(panelGo.transform, false);
        var refreshRt = refreshBtnGo.AddComponent<RectTransform>();
        refreshRt.anchorMin = new Vector2(0.55f, 1f);
        refreshRt.anchorMax = new Vector2(1f, 1f);
        refreshRt.pivot = new Vector2(1f, 1f);
        refreshRt.anchoredPosition = new Vector2(-6f, -4f);
        refreshRt.sizeDelta = new Vector2(0f, 26f);
        var refreshBg = refreshBtnGo.AddComponent<Image>();
        refreshBg.color = new Color(0.22f, 0.2f, 0.14f, 0.95f);
        var refreshBtn = refreshBtnGo.AddComponent<Button>();
        var refreshLabelGo = new GameObject("Label");
        refreshLabelGo.transform.SetParent(refreshBtnGo.transform, false);
        var refreshLabelRt = refreshLabelGo.AddComponent<RectTransform>();
        StretchFull(refreshLabelRt);
        var refreshLabel = refreshLabelGo.AddComponent<Text>();
        refreshLabel.font = font;
        refreshLabel.fontSize = 14;
        refreshLabel.alignment = TextAnchor.MiddleCenter;
        refreshLabel.color = new Color(0.95f, 0.85f, 0.4f, 1f);
        refreshLabel.text = "刷新 3";
        refreshLabel.raycastTarget = false;

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
        refreshBtn.onClick.AddListener(shop.TryRefreshPaid);

        var slots = new ShopSlot[ShopController.SlotCount];
        for (int i = 0; i < ShopController.SlotCount; i++)
        {
            slots[i] = CreateShopSlot(listGo.transform, font, shop, i, skin);
        }

        shop.Initialize(hand, slots, session, waves, refreshLabel);
        return shop;
    }

    ShopSlot CreateShopSlot(Transform parent, Font font, ShopController shop, int index, GameSkin skin)
    {
        var slotGo = new GameObject($"ShopSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        var rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(96f, 72f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

        var frameGo = new GameObject("SelectionFrame");
        frameGo.transform.SetParent(slotGo.transform, false);
        var frameRt = frameGo.AddComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = new Vector2(-2f, -2f);
        frameRt.offsetMax = new Vector2(2f, 2f);
        var frame = frameGo.AddComponent<Image>();
        frame.color = new Color(0.55f, 0.75f, 1f, 0f);
        frame.raycastTarget = false;
        frame.enabled = false;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.58f);
        iconRt.anchorMax = new Vector2(0.5f, 0.58f);
        iconRt.sizeDelta = new Vector2(24f, 24f);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = skin != null ? skin.ResolveSquare(null) : PrototypeSprites.Square;
        icon.color = Color.gray;
        icon.raycastTarget = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.22f);
        labelRt.anchorMax = new Vector2(1f, 0.45f);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.text = "空";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        label.fontSize = 11;
        label.raycastTarget = false;

        var priceGo = new GameObject("Price");
        priceGo.transform.SetParent(slotGo.transform, false);
        var priceRt = priceGo.AddComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0f, 0f);
        priceRt.anchorMax = new Vector2(1f, 0.22f);
        priceRt.offsetMin = Vector2.zero;
        priceRt.offsetMax = Vector2.zero;
        var price = priceGo.AddComponent<Text>();
        price.font = font;
        price.text = string.Empty;
        price.alignment = TextAnchor.MiddleCenter;
        price.color = new Color(0.95f, 0.82f, 0.25f, 1f);
        price.fontSize = 13;
        price.raycastTarget = false;

        var view = slotGo.AddComponent<ModuleSlotView>();
        view.Bind(bg, icon, label, frame, skin, price);

        var slot = slotGo.AddComponent<ShopSlot>();
        slot.Setup(shop, index, view);
        return slot;
    }

    HandController CreateHand(Transform sidebar, Font font, GameSkin skin)
    {
        var handGo = new GameObject("Hand");
        handGo.transform.SetParent(sidebar, false);
        var handRt = handGo.AddComponent<RectTransform>();
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
        title.text = "手牌 (8)";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.7f, 0.75f, 0.85f);
        title.fontSize = 15;
        title.raycastTarget = false;

        var row = new GameObject("Slots");
        row.transform.SetParent(handGo.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.offsetMin = new Vector2(6f, 6f);
        rowRt.offsetMax = new Vector2(-6f, -28f);

        var grid = row.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(72f, 68f);
        grid.spacing = new Vector2(6f, 6f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        var hand = handGo.AddComponent<HandController>();
        var slots = new HandSlot[HandController.SlotCount];
        for (int i = 0; i < HandController.SlotCount; i++)
        {
            slots[i] = CreateHandSlot(row.transform, font, hand, i, skin);
        }

        hand.BindSlots(slots);
        hand.ClearHand();
        return hand;
    }

    HandSlot CreateHandSlot(Transform parent, Font font, HandController hand, int index, GameSkin skin)
    {
        var slotGo = new GameObject($"HandSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        var rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(72f, 68f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.2f, 0.95f);

        var frameGo = new GameObject("SelectionFrame");
        frameGo.transform.SetParent(slotGo.transform, false);
        var frameRt = frameGo.AddComponent<RectTransform>();
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = new Vector2(-2f, -2f);
        frameRt.offsetMax = new Vector2(2f, 2f);
        var frame = frameGo.AddComponent<Image>();
        frame.color = new Color(0.55f, 0.95f, 0.45f, 0f);
        frame.raycastTarget = false;
        frame.enabled = false;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.55f);
        iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.sizeDelta = new Vector2(28f, 28f);
        var icon = iconGo.AddComponent<Image>();
        icon.sprite = skin != null ? skin.ResolveSquare(null) : PrototypeSprites.Square;
        icon.color = Color.gray;
        icon.raycastTarget = false;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 3f);
        labelRt.sizeDelta = new Vector2(0f, 18f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 11;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "空";
        label.raycastTarget = false;

        var view = slotGo.AddComponent<ModuleSlotView>();
        view.Bind(bg, icon, label, frame, skin);

        var slot = slotGo.AddComponent<HandSlot>();
        slot.Setup(hand, index, view);
        return slot;
    }
}
