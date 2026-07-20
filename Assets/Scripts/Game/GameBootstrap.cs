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
        waveManager.Initialize(lane, mage, session, enemyRoot);

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
        HandController hand = CreateHand(sidebar, font, _skin);
        var shop = CreateShopPanel(sidebar, font, hand, session, _skin);
        layout.handController = hand;
        layout.shopController = shop;

        var placementGo = new GameObject("PlacementController");
        placementGo.transform.SetParent(layoutGo.transform, false);
        var placement = placementGo.AddComponent<PlacementController>();
        placement.Initialize(board, hand, board.ModulesRoot, session, worldCam, _skin);

        CreateHintLabel(canvas.transform, font);

        ValidateRedirectorTable();
        Debug.Log("[GameBootstrap] 文档步骤 4-7：槽位状态 / HUD 机会图标 / GameSkin / 放置高亮。");
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
        text.text = "商店购入 | 左键放置 | R旋转 | 右键/X拆除 | F刷新";
        return text;
    }

    ShopController CreateShopPanel(Transform sidebar, Font font, HandController hand, GameSession session, GameSkin skin)
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
            slots[i] = CreateShopSlot(listGo.transform, font, shop, i, skin);
        }

        shop.Initialize(hand, slots, session);
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
        labelRt.anchoredPosition = new Vector2(0f, 4f);
        labelRt.sizeDelta = new Vector2(0f, 20f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.text = "空";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        label.fontSize = 12;
        label.raycastTarget = false;

        var view = slotGo.AddComponent<ModuleSlotView>();
        view.Bind(bg, icon, label, frame, skin);

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
        rt.sizeDelta = new Vector2(96f, 88f);

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
        iconRt.sizeDelta = new Vector2(36f, 36f);
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
        labelRt.sizeDelta = new Vector2(0f, 20f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 13;
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
