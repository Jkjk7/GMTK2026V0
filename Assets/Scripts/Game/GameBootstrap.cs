using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 原型启动器：组装画面分区、棋盘、发射器、手牌、商店、波次战斗与 HUD。
/// 布局目标：顶部战斗窗 + 中央棋盘窗 + 右侧手牌/商店栏，外框可透过透明区域看到世界对象。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] float cellSize = 1f;

    /// <summary>棋盘在棋盘窗内的视觉放大倍率（相机拉近）。</summary>
    const float BoardVisualScale = 1.2f;

    // 金币 / 分解区：适量缩小后同尺寸（宽×高 视口比例）
    const float GoldScrapX0 = 0.455f;
    const float GoldScrapX1 = 0.575f;
    const float GoldY0 = 0.075f;
    const float GoldY1 = 0.125f;
    const float ScrapTopY1 = 0.595f;

    GameSkin _skin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void HookSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!GameFlow.IsGameScene(scene.name))
        {
            return;
        }

        if (FindObjectOfType<GameBootstrap>() != null)
        {
            return;
        }

        var go = new GameObject("GameBootstrap");
        go.AddComponent<GameBootstrap>();
    }

    void Awake()
    {
        GameSettings.EnsureLoaded();
        BuildPrototype();
    }

    void BuildPrototype()
    {
        SetupCamera();
        EnsureEventSystem();
        _skin = GameSkin.LoadOrCreateRuntime();

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

        // 沙漏位置固定在视口 ~0.08；只把棋盘往左挪，使熔炉落在沙漏正下方同列
        const float sandViewportX = 0.08f;
        FitCameraToBoardWindow(board.GetWorldBounds());
        AlignBoardLeftUnderSand(boardRoot, board, worldCam, sandViewportX);
        FitCameraToBoardWindow(board.GetWorldBounds());
        AlignBoardLeftUnderSand(boardRoot, board, worldCam, sandViewportX);

        Bounds boardBounds = board.GetWorldBounds();

        var battleRoot = new GameObject("BattleRoot").transform;
        battleRoot.SetParent(worldRoot, false);
        layout.battleRoot = battleRoot;

        float laneY = ViewportToWorldY(worldCam, 0.82f);
        // 沙漏原位不变（不要跟着熔炉跑）
        float endX = ViewportToWorldX(worldCam, sandViewportX);
        float spawnX = ViewportToWorldX(worldCam, 0.72f);
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

        var sandClockGo = new GameObject("SandClock");
        sandClockGo.transform.SetParent(layoutGo.transform, false);
        var sandClock = sandClockGo.AddComponent<SandClock>();
        sandClock.Initialize(session);

        emitter.Initialize(board, ballManager, session, sandClock);

        var ballHudGo = new GameObject("BallCountHud");
        ballHudGo.transform.SetParent(worldRoot, false);
        var ballHud = ballHudGo.AddComponent<BallCountHud>();
        Vector3 ballHudPos = boardBounds.center;
        ballHudPos.y = boardBounds.max.y + cellSize * 0.55f;
        ballHud.Initialize(ballManager, ballHudPos);

        var lane = battleRoot.gameObject.AddComponent<BattleLane>();
        lane.Initialize(spawnAnchor, endAnchor);

        var mageGo = new GameObject("Mage");
        mageGo.transform.SetParent(mageAnchor, false);
        mageGo.transform.localPosition = Vector3.zero;
        var mage = mageGo.AddComponent<Mage>();

        var wavesGo = new GameObject("WaveManager");
        wavesGo.transform.SetParent(battleRoot, false);
        var waveManager = wavesGo.AddComponent<WaveManager>();

        mage.Initialize(mageAnchor.position);
        // 沙漏 UI 覆盖在法师位上，隐藏蓝块避免叠两套视觉
        var mageSr = mage.GetComponent<SpriteRenderer>();
        if (mageSr != null)
        {
            mageSr.enabled = false;
        }
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

        var audioGo = new GameObject("UIAudioFeedback");
        audioGo.transform.SetParent(layoutGo.transform, false);
        var uiAudio = audioGo.AddComponent<UIAudioFeedback>();
        uiAudio.EnsureSource();

        // 沙漏覆盖在 Mage（蓝块）正中：取世界坐标转视口
        Vector3 mageVp = worldCam != null
            ? worldCam.WorldToViewportPoint(mageAnchor.position)
            : new Vector3(0.08f, 0.82f, 0f);

        BuildHudAndWire(
            canvas.transform,
            font,
            layout,
            layoutGo.transform,
            sandClock,
            waveManager,
            session,
            tracker,
            uiAudio,
            mageVp);

        Transform sidebar = CreateSidebar(canvas.transform, font);

        // 经济必须在商店初始化前就绪（订阅金币变化）
        var economyGo = new GameObject("Economy");
        economyGo.transform.SetParent(layoutGo.transform, false);
        economyGo.AddComponent<Economy>();
        economyGo.AddComponent<WaveGoldBudget>();
        economyGo.AddComponent<RunModulePool>();
        economyGo.AddComponent<RunModifiers>();
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

        var emitterUpgrades = economyGo.AddComponent<EmitterRunUpgrades>();
        var emitterDraftGo = new GameObject("EmitterUpgradeDirector");
        emitterDraftGo.transform.SetParent(layoutGo.transform, false);
        var emitterDirector = emitterDraftGo.AddComponent<EmitterUpgradeDirector>();
        emitterDirector.Initialize(emitterUpgrades, draftUi);

        HandController hand = CreateHand(sidebar, font, _skin);

        var blessGo = new GameObject("BlessingCurseDirector");
        blessGo.transform.SetParent(layoutGo.transform, false);
        var blessDirector = blessGo.AddComponent<BlessingCurseDirector>();
        blessDirector.Initialize(draftUi, board, hand, boardExpand, RunModulePool.Instance);

        waveManager.Initialize(
            lane, mage, session, enemyRoot, ballManager, unlockDirector, emitterDirector, blessDirector);

        var shop = CreateShopPanel(sidebar, font, hand, session, waveManager, _skin);
        layout.handController = hand;
        layout.shopController = shop;

        var scrapGo = new GameObject("ScrapZone");
        scrapGo.transform.SetParent(worldRoot, false);
        var scrap = scrapGo.AddComponent<ScrapZone>();
        // 与金币区同比例：金币在棋盘窗右下 (0.42~0.575, 0.07~0.14)，分解区镜像到右上
        PlaceScrapMatchingGoldPanel(scrap, worldCam);

        ConfirmPromptView confirm = CreateConfirmPrompt(canvas.transform, font);
        ModuleTooltipView tooltip = CreateModuleTooltip(canvas.transform, font, _skin);
        PrepPhasePanel prepPanel = CreatePrepPhasePanel(canvas.transform, font, waveManager, session);
        CreateWaveCountdownHud(canvas.transform, font, waveManager);
        CreateRunStatsHud(canvas.transform, font, board);

        var placementGo = new GameObject("PlacementController");
        placementGo.transform.SetParent(layoutGo.transform, false);
        var placement = placementGo.AddComponent<PlacementController>();
        placement.Initialize(
            board, hand, board.ModulesRoot, session, worldCam, _skin, waveManager, scrap, confirm, tooltip,
            boardExpand);
        blessDirector.BindPlacement(placement);

        CreateHintLabel(canvas.transform, font);
        CreateBoardExpandHint(canvas.transform, font, boardExpand);

        ValidateRedirectorTable();
        Debug.Log("[GameBootstrap] Roguelike：稀有度 / 黑洞 / 祝福束缚 / 26 波（终局 Boss）。");
    }

    void BuildHudAndWire(
        Transform canvas,
        Font font,
        GameLayoutView layout,
        Transform layoutRoot,
        SandClock sandClock,
        WaveManager waveManager,
        GameSession session,
        DamageTracker tracker,
        UIAudioFeedback uiAudio,
        Vector3 mageViewport)
    {
        Text statusLabel = CreateCombatStatusLabel(canvas, font);
        SandClockPanel sandPanel = CreateSandClockPanel(canvas, font, sandClock, mageViewport);
        Text breachLabel = CreateBreachLabel(canvas, font);
        Image flash = CreateBreachFlash(canvas);
        ResultOverlayView overlay = CreateResultOverlay(canvas, font);
        layout.resultOverlay = overlay.GetComponent<CanvasGroup>();

        var sandVfxGo = new GameObject("SandVfxService");
        sandVfxGo.transform.SetParent(layoutRoot, false);
        var sandVfx = sandVfxGo.AddComponent<SandVfxService>();
        Emitter emitter = layout.emitterAnchor != null
            ? layout.emitterAnchor.GetComponent<Emitter>()
            : null;
        sandVfx.Initialize(sandPanel, emitter, layoutRoot);

        var hudGo = new GameObject("CombatHUD");
        hudGo.transform.SetParent(layoutRoot, false);
        var combatHud = hudGo.AddComponent<CombatHUD>();
        combatHud.Initialize(
            statusLabel,
            breachLabel,
            flash,
            overlay,
            sandClock,
            waveManager,
            session,
            tracker,
            uiAudio);
        layout.combatHud = combatHud;

        CreateDeveloperToolsHud(canvas, font, waveManager);
    }

    void CreateDeveloperToolsHud(Transform canvas, Font font, WaveManager waveManager)
    {
        if (!GameSettings.DeveloperMode)
        {
            return;
        }

        var root = new GameObject("DeveloperToolsHud");
        root.transform.SetParent(canvas, false);
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.62f, 0.94f);
        rootRt.anchorMax = new Vector2(0.985f, 0.985f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        var layout = root.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        Button skipBtn = CreateDevToolButton(
            root.transform,
            font,
            "SkipWaveButton",
            GameLocalization.Text("Skip Wave", "跳过本波"),
            new Color(0.28f, 0.14f, 0.12f, 0.95f),
            out Text skipLabel);

        Button goldBtn = CreateDevToolButton(
            root.transform,
            font,
            "GoldPlusButton",
            GameLocalization.Text("Gold +100", "金币+100"),
            new Color(0.22f, 0.2f, 0.1f, 0.95f),
            out _);

        var hud = root.AddComponent<DeveloperToolsHud>();
        hud.Initialize(waveManager, skipBtn, skipLabel, goldBtn);
    }

    static Button CreateDevToolButton(
        Transform parent,
        Font font,
        string name,
        string labelText,
        Color bgColor,
        out Text label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var image = go.AddComponent<Image>();
        image.color = bgColor;
        var button = go.AddComponent<Button>();
        button.targetGraphic = image;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        StretchFull(labelRt);
        label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 16;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.95f, 0.85f, 0.4f, 1f);
        label.raycastTarget = false;
        label.text = labelText;
        return button;
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
    /// 分解区与金币面板同视口宽高，摆在棋盘窗右上角（金币在右下的垂直镜像）。
    /// </summary>
    void PlaceScrapMatchingGoldPanel(ScrapZone scrap, Camera cam)
    {
        if (scrap == null || cam == null)
        {
            return;
        }

        // 与 CreateGoldPanel 共用常量（已适量缩小）
        float x0 = GoldScrapX0;
        float x1 = GoldScrapX1;
        float h = GoldY1 - GoldY0;
        float topY1 = ScrapTopY1;
        float topY0 = topY1 - h;

        float cx = ViewportToWorldX(cam, (x0 + x1) * 0.5f);
        float cy = ViewportToWorldY(cam, (topY0 + topY1) * 0.5f);
        float halfW = Mathf.Abs(ViewportToWorldX(cam, x1) - ViewportToWorldX(cam, x0)) * 0.5f;
        float halfH = Mathf.Abs(ViewportToWorldY(cam, topY1) - ViewportToWorldY(cam, topY0)) * 0.5f;
        scrap.Initialize(new Vector3(cx, cy, 0f), halfW, halfH);
    }

    /// <summary>
    /// 只平移棋盘向左/向右，使熔炉（入口左侧）落到沙漏列正下方。
    /// 沙漏/法师锚点本身不动；入口格仍为 (0,3)、向右发射。
    /// </summary>
    void AlignBoardLeftUnderSand(Transform boardRoot, GridBoard board, Camera cam, float sandViewportX)
    {
        if (boardRoot == null || board == null || cam == null)
        {
            return;
        }

        float sandWorldX = ViewportToWorldX(cam, sandViewportX);
        Vector3 entry = board.CellToWorld(new GridCoord(0, 3));
        float furnaceX = entry.x - 0.85f * cellSize;
        float dx = sandWorldX - furnaceX;
        if (Mathf.Abs(dx) < 0.001f)
        {
            return;
        }

        boardRoot.position += new Vector3(dx, 0f, 0f);
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

        // 下方分区约 60/40：棋盘窗到 0.58，侧栏从 0.60 起留缝
        const float viewX0 = 0.05f;
        const float viewX1 = 0.58f;
        const float viewY0 = 0.06f;
        const float viewY1 = 0.60f;
        float viewW = viewX1 - viewX0;
        float viewH = viewY1 - viewY0;

        float aspect = cam.aspect > 0.1f ? cam.aspect : 16f / 9f;
        float sizeFromWidth = contentW / (2f * viewW * aspect);
        float sizeFromHeight = contentH / (2f * viewH);
        // 先按窗口装下，再按 BoardVisualScale 拉近，使棋盘视觉放大
        float orthoSize = Mathf.Max(sizeFromWidth, sizeFromHeight) * 1.04f / BoardVisualScale;
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
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = CountdownArtResources.LoadSprite(
            CountdownArtResources.BattleBackdropPath,
            PrototypeSprites.Square);
        go.transform.localScale = CountdownArtResources.FitScale(sr.sprite, width, 2.4f);
        sr.color = sr.sprite == PrototypeSprites.Square
            ? new Color(0.11f, 0.13f, 0.17f, 1f)
            : Color.white;
        sr.sortingOrder = -6;
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
    /// 外框背景。旧 ui_game_frame 按 80/20 布局绘制，与 60/40 新布局错位并遮挡棋盘，
    /// 暂时禁用（useLegacyFrame=false），先用程序化纯色分区验证布局；新背景资源就绪后再开回。
    /// </summary>
    void CreateGameShell(Transform canvas)
    {
        const bool useLegacyFrame = false; // 新背景美术就绪前保持 false

#pragma warning disable CS0162
        if (useLegacyFrame)
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
        }

        Debug.Log("[GameBootstrap] 旧 ui_game_frame 已禁用（80/20 布局不匹配 60/40），使用程序化分区背景。");
        CreateProceduralShell(canvas);
#pragma warning restore CS0162
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
        Color sidebar = new Color(0.07f, 0.06f, 0.11f, 0.92f);

        CreateShellBar(root.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(0f, 0f), frame);
        CreateShellBar(root.transform, "BottomBar", new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 30f), frame);
        CreateShellBar(root.transform, "LeftBar", new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0f, 30f), new Vector2(30f, -30f), frame);
        CreateShellBar(root.transform, "RightBar", new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), new Vector2(-30f, 30f), new Vector2(0f, -30f), frame);

        // 不做局部蓝分隔线（只画一侧会显得怪）；侧栏用底板与棋盘区分即可
        Image sidebarPlate = CreateShellBar(
            root.transform,
            "SidebarPlate",
            new Vector2(0.60f, 0.03f),
            new Vector2(0.985f, 0.64f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            sidebar);
        ApplyPanelArt(sidebarPlate, 0.72f);
    }

    static Image CreateShellBar(
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
        return img;
    }

    static void ApplyPanelArt(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        if (!CountdownArtResources.UseFormalEnvironmentArt)
        {
            image.sprite = PrototypeSprites.Square;
            image.type = Image.Type.Simple;
            image.color = new Color(0.12f, 0.14f, 0.18f, alpha);
            return;
        }

        image.sprite = CountdownArtResources.LoadSprite(
            CountdownArtResources.PanelBackgroundPath,
            PrototypeSprites.Square);
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 1f, 1f, alpha);
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
        // 侧栏：与棋盘窗留缝，避免手牌穿进战斗/棋盘区
        rt.anchorMin = new Vector2(0.60f, 0.03f);
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
        headerText.fontSize = 20;
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.color = new Color(0.75f, 0.8f, 0.95f, 1f);
        headerText.text = GameLocalization.Text("MODULE STORAGE", "模块仓");

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
        text.text = GameLocalization.Text("Preparing...", "准备中…");

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

    SandClockPanel CreateSandClockPanel(Transform canvas, Font font, SandClock sandClock, Vector3 mageViewport)
    {
        // 沙漏 = 原法师：面板中心对齐蓝块（Mage）的视口坐标
        const float halfW = 0.055f;
        const float halfH = 0.09f;
        float cx = Mathf.Clamp(mageViewport.x, halfW + 0.01f, 1f - halfW - 0.01f);
        float cy = Mathf.Clamp(mageViewport.y, halfH + 0.02f, 1f - halfH - 0.02f);

        var go = new GameObject("SandClockPanel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(cx - halfW, cy - halfH);
        rt.anchorMax = new Vector2(cx + halfW, cy + halfH);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // 背板
        var bgGo = new GameObject("Bg");
        bgGo.transform.SetParent(go.transform, false);
        var bgRt = bgGo.AddComponent<RectTransform>();
        StretchFull(bgRt);
        var bg = bgGo.AddComponent<Image>();
        bg.color = Color.clear;
        bg.raycastTarget = false;

        var frameGo = new GameObject("HourglassFrame");
        frameGo.transform.SetParent(go.transform, false);
        var frameRt = frameGo.AddComponent<RectTransform>();
        frameRt.anchorMin = new Vector2(0.08f, 0.02f);
        frameRt.anchorMax = new Vector2(0.92f, 0.74f);
        frameRt.offsetMin = Vector2.zero;
        frameRt.offsetMax = Vector2.zero;
        var frame = frameGo.AddComponent<Image>();
        frame.sprite = CountdownArtResources.LoadSprite(
            CountdownArtResources.HourglassFramePath,
            PrototypeSprites.Square);
        frame.color = Color.white;
        frame.preserveAspect = true;
        frame.raycastTarget = false;

        // 沙漏上方：毫秒倒计时
        var cdGo = new GameObject("Countdown");
        cdGo.transform.SetParent(go.transform, false);
        var cdRt = cdGo.AddComponent<RectTransform>();
        cdRt.anchorMin = new Vector2(0.02f, 0.72f);
        cdRt.anchorMax = new Vector2(0.98f, 0.98f);
        cdRt.offsetMin = Vector2.zero;
        cdRt.offsetMax = Vector2.zero;
        var cd = cdGo.AddComponent<Text>();
        cd.font = font;
        cd.fontSize = 24;
        cd.fontStyle = FontStyle.Bold;
        cd.alignment = TextAnchor.MiddleCenter;
        cd.color = new Color(0.95f, 0.88f, 0.62f, 1f);
        cd.text = "01:40.000";
        cd.raycastTarget = false;
        cd.resizeTextForBestFit = true;
        cd.resizeTextMinSize = 10;
        cd.resizeTextMaxSize = 26;

        // 沙漏内部的动态沙量，叠在正式框体和玻璃之上。
        Image MakeGlass(string name, Vector2 aMin, Vector2 aMax)
        {
            var g = new GameObject(name);
            g.transform.SetParent(go.transform, false);
            var grt = g.AddComponent<RectTransform>();
            grt.anchorMin = aMin;
            grt.anchorMax = aMax;
            grt.offsetMin = Vector2.zero;
            grt.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            img.sprite = PrototypeSprites.Circle;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        Image glassTop = MakeGlass("GlassTop", new Vector2(0.39f, 0.44f), new Vector2(0.61f, 0.61f));
        Image glassBottom = MakeGlass("GlassBottom", new Vector2(0.39f, 0.15f), new Vector2(0.61f, 0.32f));

        // 罚沙/补沙浮动提示（面板右侧探出）
        var floatGo = new GameObject("FloatDelta");
        floatGo.transform.SetParent(go.transform, false);
        var floatRt = floatGo.AddComponent<RectTransform>();
        floatRt.anchorMin = new Vector2(0.66f, 0.36f);
        floatRt.anchorMax = new Vector2(1.6f, 0.70f);
        floatRt.offsetMin = Vector2.zero;
        floatRt.offsetMax = Vector2.zero;
        var floatText = floatGo.AddComponent<Text>();
        floatText.font = font;
        floatText.fontSize = 22;
        floatText.fontStyle = FontStyle.Bold;
        floatText.alignment = TextAnchor.MiddleLeft;
        floatText.raycastTarget = false;
        floatGo.SetActive(false);

        var panel = go.AddComponent<SandClockPanel>();
        panel.Bind(sandClock, cd, floatText, glassTop, glassBottom, frame);
        return panel;
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
        text.text = GameLocalization.Text(
            "Drag: move/merge/scrap | Click grey cells: expand | Hover: details | Space: ready | F: refresh",
            "拖棋盘可移动/合成/分解 | 点灰格扩展棋盘 | 悬停详情 | Space准备 | F刷新");
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
                text.text = GameLocalization.Text(
                    $"Board: {expand.UnlockedSize}×{expand.UnlockedSize} (MAX)",
                    $"棋盘：{expand.UnlockedSize}×{expand.UnlockedSize}（已满）");
            }
            else
            {
                text.text = GameLocalization.Text(
                    $"Board: {expand.UnlockedSize}×{expand.UnlockedSize} → {expand.GetNextSize()}×{expand.GetNextSize()} ({cost} gold; click grey cell)",
                    $"棋盘：{expand.UnlockedSize}×{expand.UnlockedSize} → {expand.GetNextSize()}×{expand.GetNextSize()}（{cost}金，点灰格）");
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
        var icons = new Image[3];
        var descs = new Text[3];
        float[] x0 = { 0.04f, 0.36f, 0.68f };
        float[] x1 = { 0.32f, 0.64f, 0.96f };
        for (int i = 0; i < 3; i++)
        {
            var btnGo = new GameObject($"Choice{i}");
            btnGo.transform.SetParent(root.transform, false);
            var brt = btnGo.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(x0[i], 0.08f);
            brt.anchorMax = new Vector2(x1[i], 0.78f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.18f, 0.22f, 0.28f, 1f);
            buttons[i] = btnGo.AddComponent<Button>();

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.2f, 0.62f);
            iconRt.anchorMax = new Vector2(0.8f, 0.92f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            icons[i] = iconGo.AddComponent<Image>();
            icons[i].preserveAspect = true;
            icons[i].raycastTarget = false;

            var lg = new GameObject("Label");
            lg.transform.SetParent(btnGo.transform, false);
            var lrt = lg.AddComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0.06f, 0.38f);
            lrt.anchorMax = new Vector2(0.94f, 0.62f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            labels[i] = lg.AddComponent<Text>();
            labels[i].font = font;
            labels[i].fontSize = 15;
            labels[i].fontStyle = FontStyle.Bold;
            labels[i].alignment = TextAnchor.MiddleCenter;
            labels[i].color = Color.white;
            labels[i].raycastTarget = false;
            labels[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            labels[i].verticalOverflow = VerticalWrapMode.Overflow;

            var dg = new GameObject("Desc");
            dg.transform.SetParent(btnGo.transform, false);
            var drt = dg.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.06f, 0.04f);
            drt.anchorMax = new Vector2(0.94f, 0.38f);
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
            descs[i] = dg.AddComponent<Text>();
            descs[i].font = font;
            descs[i].fontSize = 12;
            descs[i].alignment = TextAnchor.UpperCenter;
            descs[i].color = new Color(0.82f, 0.86f, 0.92f, 1f);
            descs[i].raycastTarget = false;
            descs[i].horizontalOverflow = HorizontalWrapMode.Wrap;
            descs[i].verticalOverflow = VerticalWrapMode.Overflow;
        }

        var view = root.AddComponent<DraftChoiceView>();
        view.Bind(group, title, buttons, labels, icons, descs);
        return view;
    }

    PrepPhasePanel CreatePrepPhasePanel(Transform canvas, Font font, WaveManager waves, GameSession session)
    {
        var root = new GameObject("PrepPhasePanel");
        root.transform.SetParent(canvas, false);
        var rootRt = root.AddComponent<RectTransform>();
        // 战斗区横向占满全屏（100%）；准备窗口保持原尺寸宽0.44×高0.20，整屏水平居中
        rootRt.anchorMin = new Vector2(0.28f, 0.78f);
        rootRt.anchorMax = new Vector2(0.72f, 0.98f);
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        var group = root.AddComponent<CanvasGroup>();

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.12f, 0.1f, 0.82f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(root.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.05f, 0.78f);
        titleRt.anchorMax = new Vector2(0.95f, 0.96f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.fontSize = 22;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.95f, 0.9f, 0.45f, 1f);
        title.text = GameLocalization.Text("PREPARATION", "准备阶段");

        var timerGo = new GameObject("Timer");
        timerGo.transform.SetParent(root.transform, false);
        var timerRt = timerGo.AddComponent<RectTransform>();
        timerRt.anchorMin = new Vector2(0.05f, 0.64f);
        timerRt.anchorMax = new Vector2(0.95f, 0.78f);
        timerRt.offsetMin = Vector2.zero;
        timerRt.offsetMax = Vector2.zero;
        var timer = timerGo.AddComponent<Text>();
        timer.font = font;
        timer.fontSize = 22;
        timer.fontStyle = FontStyle.Bold;
        timer.alignment = TextAnchor.MiddleCenter;
        timer.color = new Color(0.25f, 0.55f, 0.35f, 1f);
        timer.text = GameLocalization.Text("Waiting…", "等待中…");

        var hintGo = new GameObject("Hint");
        hintGo.transform.SetParent(root.transform, false);
        var hintRt = hintGo.AddComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0.05f, 0.52f);
        hintRt.anchorMax = new Vector2(0.95f, 0.64f);
        hintRt.offsetMin = Vector2.zero;
        hintRt.offsetMax = Vector2.zero;
        var hint = hintGo.AddComponent<Text>();
        hint.font = font;
        hint.fontSize = 14;
        hint.alignment = TextAnchor.MiddleCenter;
        hint.color = new Color(0.8f, 0.85f, 0.75f, 1f);
        hint.text = GameLocalization.Text("Buy, merge, and arrange modules", "购买、合成并调整模块");

        // 原进度条 + 敌人条区域：居中色圆预览下波敌人
        var previewGo = new GameObject("WavePreview");
        previewGo.transform.SetParent(root.transform, false);
        var previewRt = previewGo.AddComponent<RectTransform>();
        previewRt.anchorMin = new Vector2(0.06f, 0.04f);
        previewRt.anchorMax = new Vector2(0.94f, 0.50f);
        previewRt.offsetMin = Vector2.zero;
        previewRt.offsetMax = Vector2.zero;

        var tipGo = new GameObject("PreviewTip");
        tipGo.transform.SetParent(root.transform, false);
        var tipRt = tipGo.AddComponent<RectTransform>();
        tipRt.anchorMin = new Vector2(0.05f, -0.55f);
        tipRt.anchorMax = new Vector2(0.95f, -0.05f);
        tipRt.offsetMin = Vector2.zero;
        tipRt.offsetMax = Vector2.zero;
        var tip = tipGo.AddComponent<Text>();
        tip.font = font;
        tip.fontSize = 14;
        tip.alignment = TextAnchor.UpperCenter;
        tip.color = new Color(0.9f, 0.92f, 0.95f, 1f);
        tip.raycastTarget = false;
        tip.horizontalOverflow = HorizontalWrapMode.Wrap;
        tip.verticalOverflow = VerticalWrapMode.Overflow;
        tipGo.SetActive(false);

        var preview = previewGo.AddComponent<WavePreviewStrip>();
        preview.Bind(previewGo.transform, font, tip);

        var readyGo = new GameObject("ReadyButton");
        readyGo.transform.SetParent(canvas, false);
        var readyRt = readyGo.AddComponent<RectTransform>();
        // 准备完毕：战斗区（全宽）内、准备窗口右侧
        readyRt.anchorMin = new Vector2(0.735f, 0.82f);
        readyRt.anchorMax = new Vector2(0.88f, 0.94f);
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
        readyLabel.text = GameLocalization.Text("READY [Space]", "准备完毕 [Space]");
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
        panel.Bind(title, timer, hint, cd, readyBtn, readyLabel, group, waves, session, preview);
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
        rt.sizeDelta = new Vector2(440f, 560f);
        var group = root.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.08f, 0.11f, 0.96f);
        bg.raycastTarget = false;

        var outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.75f, 0.3f, 0.75f);
        outline.effectDistance = new Vector2(2f, -2f);

        Text MakeText(
            Transform parent,
            string name,
            Vector2 aMin,
            Vector2 aMax,
            int size,
            Color color,
            FontStyle style,
            TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
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
            t.resizeTextForBestFit = false;
            return t;
        }

        var moduleGo = new GameObject("ModuleBlock");
        moduleGo.transform.SetParent(root.transform, false);
        var moduleRt = moduleGo.AddComponent<RectTransform>();
        StretchFull(moduleRt);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(moduleGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.05f, 0.78f);
        iconRt.anchorMax = new Vector2(0.22f, 0.96f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        var icon = iconGo.AddComponent<Image>();
        icon.raycastTarget = false;
        icon.sprite = PrototypeSprites.Square;

        Text name = MakeText(
            moduleGo.transform,
            "Name",
            new Vector2(0.26f, 0.86f), new Vector2(0.96f, 0.96f),
            20, new Color(1f, 0.9f, 0.5f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft);

        Text rarity = MakeText(
            moduleGo.transform,
            "Rarity",
            new Vector2(0.26f, 0.78f), new Vector2(0.96f, 0.86f),
            13, new Color(0.75f, 0.4f, 1f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft);

        Text desc = MakeText(
            moduleGo.transform,
            "Description",
            new Vector2(0.05f, 0.64f), new Vector2(0.95f, 0.76f),
            13, new Color(0.92f, 0.94f, 0.96f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

        Text stats = MakeText(
            moduleGo.transform,
            "Stats",
            new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.62f),
            12, new Color(0.75f, 0.88f, 1f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);
        stats.supportRichText = true;
        stats.lineSpacing = 1.05f;

        Text flavor = MakeText(
            moduleGo.transform,
            "Flavor",
            new Vector2(0.05f, 0.015f), new Vector2(0.95f, 0.09f),
            11, new Color(0.55f, 0.58f, 0.62f, 0.95f), FontStyle.Italic, TextAnchor.UpperLeft);

        var enchantGo = new GameObject("EnchantBlock");
        enchantGo.transform.SetParent(root.transform, false);
        var enchantRt = enchantGo.AddComponent<RectTransform>();
        enchantRt.anchorMin = new Vector2(0.04f, 0.02f);
        enchantRt.anchorMax = new Vector2(0.96f, 0.22f);
        enchantRt.offsetMin = Vector2.zero;
        enchantRt.offsetMax = Vector2.zero;
        var enchantBg = enchantGo.AddComponent<Image>();
        enchantBg.color = new Color(0.12f, 0.13f, 0.18f, 0.95f);
        enchantBg.raycastTarget = false;

        Text enchantTitle = MakeText(
            enchantGo.transform,
            "EnchantTitle",
            new Vector2(0.04f, 0.55f), new Vector2(0.96f, 0.95f),
            14, new Color(1f, 0.65f, 0.3f, 1f), FontStyle.Bold, TextAnchor.MiddleLeft);

        Text enchantDesc = MakeText(
            enchantGo.transform,
            "EnchantDesc",
            new Vector2(0.04f, 0.08f), new Vector2(0.96f, 0.55f),
            12, new Color(0.88f, 0.9f, 0.94f, 1f), FontStyle.Normal, TextAnchor.UpperLeft);

        enchantGo.SetActive(false);

        var view = root.AddComponent<ModuleTooltipView>();
        view.Bind(
            group, rt, icon, name, desc, stats, flavor, skin, rarity,
            enchantTitle, enchantDesc, moduleGo, enchantGo);
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
        cancelLabel.text = GameLocalization.Text("Cancel", "取消");
        Button confirm = MakeBtn("Confirm", new Vector2(0.55f, 0.06f), new Vector2(0.92f, 0.24f),
            new Color(0.55f, 0.25f, 0.15f, 1f), out Text confirmLabel);
        confirmLabel.text = GameLocalization.Text("Confirm", "确认");

        var view = root.AddComponent<ConfirmPromptView>();
        view.Bind(group, title, body, warn, confirm, cancel, confirmLabel);
        return view;
    }

    GoldPanel CreateGoldPanel(Transform canvas, Font font)
    {
        var go = new GameObject("GoldPanel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        // 棋盘窗右下（略缩小，给棋盘放大让位）
        rt.anchorMin = new Vector2(GoldScrapX0, GoldY0);
        rt.anchorMax = new Vector2(GoldScrapX1, GoldY1);
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
        value.fontSize = 18;
        value.fontStyle = FontStyle.Bold;
        value.alignment = TextAnchor.MiddleCenter;
        value.color = new Color(0.35f, 0.2f, 0.08f, 1f);
        value.text = GameLocalization.Text("Gold ", "金币 ") + Economy.StartingGold;
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
        ApplyPanelArt(bg, 0.72f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0.38f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(8f, -4f);
        titleRt.sizeDelta = new Vector2(0f, 26f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.text = GameLocalization.Text("SHOP Lv1", "商店 Lv1");
        title.alignment = TextAnchor.MiddleLeft;
        title.color = new Color(0.7f, 0.7f, 0.8f);
        title.fontSize = 16;

        var lockBtnGo = new GameObject("LockButton");
        lockBtnGo.transform.SetParent(panelGo.transform, false);
        var lockRt = lockBtnGo.AddComponent<RectTransform>();
        lockRt.anchorMin = new Vector2(0.38f, 1f);
        lockRt.anchorMax = new Vector2(0.55f, 1f);
        lockRt.pivot = new Vector2(0.5f, 1f);
        lockRt.anchoredPosition = new Vector2(0f, -4f);
        lockRt.sizeDelta = new Vector2(0f, 26f);
        var lockBg = lockBtnGo.AddComponent<Image>();
        lockBg.color = new Color(0.18f, 0.2f, 0.26f, 0.95f);
        var lockBtn = lockBtnGo.AddComponent<Button>();
        lockBtn.targetGraphic = lockBg;
        var lockLabelGo = new GameObject("Label");
        lockLabelGo.transform.SetParent(lockBtnGo.transform, false);
        var lockLabelRt = lockLabelGo.AddComponent<RectTransform>();
        StretchFull(lockLabelRt);
        var lockLabel = lockLabelGo.AddComponent<Text>();
        lockLabel.font = font;
        lockLabel.fontSize = 14;
        lockLabel.alignment = TextAnchor.MiddleCenter;
        lockLabel.color = new Color(0.9f, 0.88f, 0.75f, 1f);
        lockLabel.text = GameLocalization.Text("Lock", "锁定");
        lockLabel.raycastTarget = false;

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
        refreshLabel.fontSize = 16;
        refreshLabel.alignment = TextAnchor.MiddleCenter;
        refreshLabel.color = new Color(0.95f, 0.85f, 0.4f, 1f);
        refreshLabel.text = GameLocalization.Text("Refresh 5", "刷新 5");
        refreshLabel.raycastTarget = false;

        var listGo = new GameObject("Slots");
        listGo.transform.SetParent(panelGo.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(8f, 8f);
        listRt.offsetMax = new Vector2(-8f, -32f);

        var grid = listGo.AddComponent<GridLayoutGroup>();
        // 侧栏 40% 后放大商品卡（3列×2行）
        grid.cellSize = new Vector2(210f, 112f);
        grid.spacing = new Vector2(12f, 10f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;

        var shop = panelGo.AddComponent<ShopController>();
        refreshBtn.onClick.AddListener(shop.TryRefreshPaid);
        lockBtn.onClick.AddListener(shop.ToggleLock);

        var slots = new ShopSlot[ShopController.SlotCount];
        for (int i = 0; i < ShopController.SlotCount; i++)
        {
            slots[i] = CreateShopSlot(listGo.transform, font, shop, i, skin);
        }

        shop.Initialize(hand, slots, session, waves, refreshLabel, title, lockLabel, lockBg);
        return shop;
    }

    ShopSlot CreateShopSlot(Transform parent, Font font, ShopController shop, int index, GameSkin skin)
    {
        var slotGo = new GameObject($"ShopSlot_{index}");
        slotGo.transform.SetParent(parent, false);
        var rt = slotGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(210f, 112f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.14f, 0.18f, 0.95f);

        var rarityGo = new GameObject("RarityBar");
        rarityGo.transform.SetParent(slotGo.transform, false);
        var rarityRt = rarityGo.AddComponent<RectTransform>();
        rarityRt.anchorMin = new Vector2(0f, 0.92f);
        rarityRt.anchorMax = new Vector2(1f, 1f);
        rarityRt.offsetMin = Vector2.zero;
        rarityRt.offsetMax = Vector2.zero;
        var rarityBar = rarityGo.AddComponent<Image>();
        rarityBar.color = ModuleCatalog.GetRarityColor(ModuleRarity.Common);
        rarityBar.raycastTarget = false;
        rarityBar.enabled = false;

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
        iconRt.anchorMin = new Vector2(0.5f, 0.62f);
        iconRt.anchorMax = new Vector2(0.5f, 0.62f);
        iconRt.sizeDelta = new Vector2(44f, 44f);
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
        label.text = GameLocalization.Text("Empty", "空");
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        label.fontSize = 17;
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
        price.fontSize = 16;
        price.raycastTarget = false;

        var view = slotGo.AddComponent<ModuleSlotView>();
        view.Bind(bg, icon, label, frame, skin, price, rarityBar);

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
        ApplyPanelArt(bg, 0.72f);

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
        title.text = GameLocalization.Text("HAND (8)", "手牌 (8)");
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.7f, 0.75f, 0.85f);
        title.fontSize = 18;
        title.raycastTarget = false;

        var row = new GameObject("Slots");
        row.transform.SetParent(handGo.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.offsetMin = new Vector2(6f, 6f);
        rowRt.offsetMax = new Vector2(-6f, -28f);

        var grid = row.AddComponent<GridLayoutGroup>();
        // 侧栏 40% 后放大手牌格（4列×2行）
        grid.cellSize = new Vector2(160f, 116f);
        grid.spacing = new Vector2(10f, 8f);
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
        rt.sizeDelta = new Vector2(160f, 116f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.16f, 0.2f, 0.95f);

        var rarityGo = new GameObject("RarityBar");
        rarityGo.transform.SetParent(slotGo.transform, false);
        var rarityRt = rarityGo.AddComponent<RectTransform>();
        rarityRt.anchorMin = new Vector2(0f, 0.92f);
        rarityRt.anchorMax = new Vector2(1f, 1f);
        rarityRt.offsetMin = Vector2.zero;
        rarityRt.offsetMax = Vector2.zero;
        var rarityBar = rarityGo.AddComponent<Image>();
        rarityBar.color = ModuleCatalog.GetRarityColor(ModuleRarity.Common);
        rarityBar.raycastTarget = false;
        rarityBar.enabled = false;

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
        iconRt.anchorMin = new Vector2(0.5f, 0.58f);
        iconRt.anchorMax = new Vector2(0.5f, 0.58f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
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
        labelRt.anchoredPosition = new Vector2(0f, 4f);
        labelRt.sizeDelta = new Vector2(0f, 24f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = GameLocalization.Text("Empty", "空");
        label.raycastTarget = false;

        var view = slotGo.AddComponent<ModuleSlotView>();
        view.Bind(bg, icon, label, frame, skin, null, rarityBar);

        var slot = slotGo.AddComponent<HandSlot>();
        slot.Setup(hand, index, view);
        return slot;
    }

    RunStatsHud CreateRunStatsHud(Transform canvas, Font font, GridBoard board)
    {
        // 分解区正下方：收起时仅按钮条；展开后同宽向下弹出详情
        float scrapH = GoldY1 - GoldY0;
        float scrapBottom = ScrapTopY1 - scrapH;
        float btnH = 0.04f;
        float gap = 0.008f;
        float btnTop = scrapBottom - gap;
        float btnBottom = btnTop - btnH;
        float panelH = 0.145f;
        float expandedBottom = btnBottom - gap - panelH;

        var collapsedMin = new Vector2(GoldScrapX0, btnBottom);
        var collapsedMax = new Vector2(GoldScrapX1, btnTop);
        var expandedMin = new Vector2(GoldScrapX0, expandedBottom);
        var expandedMax = new Vector2(GoldScrapX1, btnTop);

        var root = new GameObject("RunStatsHud");
        root.transform.SetParent(canvas, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = collapsedMin;
        rt.anchorMax = collapsedMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var panelGo = new GameObject("DetailPanel");
        panelGo.transform.SetParent(root.transform, false);
        var panelRt = panelGo.AddComponent<RectTransform>();
        StretchFull(panelRt);
        var panelBg = panelGo.AddComponent<Image>();
        panelBg.color = new Color(0.06f, 0.07f, 0.1f, 0.9f);
        panelBg.raycastTarget = true;

        var detailGo = new GameObject("Detail");
        detailGo.transform.SetParent(panelGo.transform, false);
        var drt = detailGo.AddComponent<RectTransform>();
        drt.anchorMin = new Vector2(0.06f, 0.06f);
        drt.anchorMax = new Vector2(0.94f, 0.94f);
        drt.offsetMin = Vector2.zero;
        drt.offsetMax = Vector2.zero;
        var detail = detailGo.AddComponent<Text>();
        detail.font = font;
        detail.fontSize = 13;
        detail.alignment = TextAnchor.UpperLeft;
        detail.color = new Color(0.88f, 0.9f, 0.94f, 0.95f);
        detail.raycastTarget = false;
        detail.horizontalOverflow = HorizontalWrapMode.Wrap;
        detail.verticalOverflow = VerticalWrapMode.Overflow;
        panelGo.SetActive(false);

        var btnGo = new GameObject("Toggle");
        btnGo.transform.SetParent(root.transform, false);
        var brt = btnGo.AddComponent<RectTransform>();
        StretchFull(brt);
        var btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.16f, 0.12f, 0.92f);
        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        var btnLabelGo = new GameObject("Label");
        btnLabelGo.transform.SetParent(btnGo.transform, false);
        var blrt = btnLabelGo.AddComponent<RectTransform>();
        StretchFull(blrt);
        var btnLabel = btnLabelGo.AddComponent<Text>();
        btnLabel.font = font;
        btnLabel.fontSize = 13;
        btnLabel.fontStyle = FontStyle.Bold;
        btnLabel.alignment = TextAnchor.MiddleCenter;
        btnLabel.color = new Color(0.95f, 0.85f, 0.55f, 1f);
        btnLabel.raycastTarget = false;
        btnLabel.text = GameLocalization.Text("View Upgrades", "查看已有增幅");

        var hud = root.AddComponent<RunStatsHud>();
        hud.Bind(
            rt, detail, btn, panelGo, panelRt, board,
            collapsedMin, collapsedMax, expandedMin, expandedMax);
        return hud;
    }

    WaveCountdownHud CreateWaveCountdownHud(Transform canvas, Font font, WaveManager waves)
    {
        var root = new GameObject("WaveCountdownHud");
        root.transform.SetParent(canvas, false);
        var rt = root.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.88f, 0.92f);
        rt.anchorMax = new Vector2(0.99f, 0.99f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Text MakeNum(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var nrt = go.AddComponent<RectTransform>();
            StretchFull(nrt);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 42;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            t.text = "26";
            return t;
        }

        Text label = MakeNum("Remaining");
        Text outgoing = MakeNum("Outgoing");
        outgoing.gameObject.SetActive(false);

        var captionGo = new GameObject("Caption");
        captionGo.transform.SetParent(root.transform, false);
        var crt = captionGo.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 0f);
        crt.anchorMax = new Vector2(1f, 0.35f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var caption = captionGo.AddComponent<Text>();
        caption.font = font;
        caption.fontSize = 14;
        caption.alignment = TextAnchor.UpperCenter;
        caption.color = new Color(0.75f, 0.78f, 0.85f, 0.9f);
        caption.raycastTarget = false;
        caption.text = GameLocalization.Text("WAVES LEFT", "剩余波数");

        var hud = root.AddComponent<WaveCountdownHud>();
        hud.Bind(label, outgoing, waves);
        return hud;
    }
}
