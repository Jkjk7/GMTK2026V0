using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 原型启动器：组装画面分区、棋盘、发射器、手牌、商店、波次战斗与 HUD。
/// 放到场景空物体上即可；若场景中不存在，也会在运行时自动创建一份。
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    [Header("Layout Ratios (logical)")]
    [SerializeField] float battleHeightRatio = 0.20f;
    [SerializeField] float handHeightRatio = 0.15f;

    [Header("Board")]
    [SerializeField] float cellSize = 1f;

    static bool s_autoSpawnChecked;

    /// <summary>
    /// 若场景里没有 GameBootstrap，则自动生成，避免空场景无法演示。
    /// </summary>
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

    /// <summary>
    /// 一次性搭建整个原型场景层级与引用关系。
    /// </summary>
    void BuildPrototype()
    {
        SetupCamera();
        EnsureEventSystem();

        float boardWorldWidth = GridBoard.Width * cellSize;
        Vector2 boardOrigin = new Vector2(-boardWorldWidth * 0.5f + cellSize * 0.5f, -3.2f);

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
        Text statusLabel = CreateCombatStatusLabel(canvas.transform, font);
        Text overlayLabel = CreateOverlayLabel(canvas.transform, font);

        var hudGo = new GameObject("CombatHUD");
        var combatHud = hudGo.AddComponent<CombatHUD>();
        combatHud.Initialize(statusLabel, overlayLabel, mage, waveManager, session, tracker);

        HandController hand = CreateHand(canvas.transform, font);
        CreateShopPanel(canvas.transform, font, hand, session);

        var placementGo = new GameObject("PlacementController");
        var placement = placementGo.AddComponent<PlacementController>();
        placement.Initialize(board, hand, modulesRoot, session);

        CreateHintLabel(canvas.transform, font);
        FitCameraToLayout(boardBounds, lane.LaneY);

        ValidateRedirectorTable();
        Debug.Log("[GameBootstrap] Prototype ready. 商店购入 → 手牌放置 | R旋转 | 右键/X拆除 | 守住魔法师！");
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
        cam.backgroundColor = new Color(0.08f, 0.09f, 0.11f, 1f);
        cam.rect = new Rect(0f, 0f, 1f, 1f);
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.orthographicSize = 8f;
    }

    void FitCameraToLayout(Bounds boardBounds, float enemyY)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        const float bottomUi = 0.20f;
        const float rightUi = 0.14f;
        cam.rect = new Rect(0f, bottomUi, 1f - rightUi, 1f - bottomUi);

        float top = Mathf.Max(boardBounds.max.y, enemyY) + 1.2f;
        float bottom = boardBounds.min.y - 1.0f;
        float height = Mathf.Max(0.1f, top - bottom);
        cam.orthographicSize = Mathf.Max(5.5f, height * 0.52f);
        cam.transform.position = new Vector3(
            boardBounds.center.x,
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
        go.transform.localScale = new Vector3(boardBounds.size.x + 2f, 2.4f, 1f);
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

    Text CreateCombatStatusLabel(Transform canvas, Font font)
    {
        var go = new GameObject("CombatStatusLabel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -16f);
        rt.sizeDelta = new Vector2(720f, 64f);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 30;
        text.color = Color.white;
        text.alignment = TextAnchor.UpperLeft;
        text.text = "准备中…";

        var bgGo = new GameObject("StatusBg");
        bgGo.transform.SetParent(go.transform, false);
        bgGo.transform.SetAsFirstSibling();
        var bgRt = bgGo.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = new Vector2(-12f, -8f);
        bgRt.offsetMax = new Vector2(12f, 8f);
        var bg = bgGo.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.55f);
        return text;
    }

    Text CreateOverlayLabel(Transform canvas, Font font)
    {
        var go = new GameObject("ResultOverlay");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(600f, 120f);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 72;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = string.Empty;
        go.SetActive(false);
        return text;
    }

    Text CreateHintLabel(Transform canvas, Font font)
    {
        var go = new GameObject("HintLabel");
        go.transform.SetParent(canvas, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(-80f, 210f);
        rt.sizeDelta = new Vector2(900f, 36f);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = 20;
        text.color = new Color(0.85f, 0.88f, 0.95f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "商店购入 | 左键放置 | R旋转 | 右键/X拆除 | F刷新商店";
        return text;
    }

    RectTransform CreateShopPanel(Transform canvas, Font font, HandController hand, GameSession session)
    {
        var panelGo = new GameObject("ShopPanel");
        panelGo.transform.SetParent(canvas, false);
        var panel = panelGo.AddComponent<RectTransform>();
        panel.anchorMin = new Vector2(1f, 0.2f);
        panel.anchorMax = new Vector2(1f, 0.95f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-8f, 0f);
        panel.sizeDelta = new Vector2(200f, 0f);

        var bg = panelGo.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.12f, 0.7f);

        var titleGo = new GameObject("Title");
        titleGo.transform.SetParent(panelGo.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -8f);
        titleRt.sizeDelta = new Vector2(0f, 28f);
        var title = titleGo.AddComponent<Text>();
        title.font = font;
        title.text = "商店 (F刷新)";
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.7f, 0.7f, 0.75f);
        title.fontSize = 16;

        var listGo = new GameObject("Slots");
        listGo.transform.SetParent(panelGo.transform, false);
        var listRt = listGo.AddComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 0f);
        listRt.anchorMax = new Vector2(1f, 1f);
        listRt.offsetMin = new Vector2(8f, 8f);
        listRt.offsetMax = new Vector2(-8f, -40f);
        var layout = listGo.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

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
        rt.sizeDelta = new Vector2(120f, 56f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.12f, 0.15f, 0.85f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(8f, 0f);
        iconRt.sizeDelta = new Vector2(28f, 28f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.gray;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(40f, 0f);
        labelRt.offsetMax = new Vector2(-4f, 0f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.text = "空";
        label.alignment = TextAnchor.MiddleLeft;
        label.color = new Color(0.55f, 0.55f, 0.6f, 1f);
        label.fontSize = 14;

        var slot = slotGo.AddComponent<ShopSlot>();
        slot.Setup(shop, index, bg, icon, label);
        return slot;
    }

    HandController CreateHand(Transform canvas, Font font)
    {
        var handGo = new GameObject("Hand");
        handGo.transform.SetParent(canvas, false);
        var handRt = handGo.AddComponent<RectTransform>();
        handRt.anchorMin = new Vector2(0f, 0f);
        handRt.anchorMax = new Vector2(1f, 0f);
        handRt.pivot = new Vector2(0.5f, 0f);
        handRt.anchoredPosition = Vector2.zero;
        handRt.sizeDelta = new Vector2(-210f, 200f);

        var bg = handGo.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.09f, 0.92f);

        var row = new GameObject("Slots");
        row.transform.SetParent(handGo.transform, false);
        var rowRt = row.AddComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.5f, 0.5f);
        rowRt.anchorMax = new Vector2(0.5f, 0.5f);
        rowRt.sizeDelta = new Vector2(780f, 150f);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;

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
        rt.sizeDelta = new Vector2(140f, 150f);

        var bg = slotGo.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.55f);
        iconRt.anchorMax = new Vector2(0.5f, 0.55f);
        iconRt.sizeDelta = new Vector2(48f, 48f);
        var icon = iconGo.AddComponent<Image>();
        icon.color = Color.gray;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(slotGo.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 0f);
        labelRt.pivot = new Vector2(0.5f, 0f);
        labelRt.anchoredPosition = new Vector2(0f, 6f);
        labelRt.sizeDelta = new Vector2(0f, 24f);
        var label = labelGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 16;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "空";

        var slot = slotGo.AddComponent<HandSlot>();
        slot.Setup(hand, index, bg, icon, label);
        return slot;
    }
}
