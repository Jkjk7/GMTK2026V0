using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Optional, scene-local field guide. Never changes the run or consumes gameplay randomness.</summary>
public sealed class TutorialOverlay : MonoBehaviour
{
    static TutorialOverlay s_instance;
    static int s_closedFrame = -1;
    public static bool IsOpen => s_instance != null && s_instance._paused;
    // A closing Escape/click must not fall through to another controller in this same frame.
    public static bool BlocksInput => IsOpen || s_closedFrame == Time.frameCount;

    static readonly Color Gold = new Color(0.91f, 0.73f, 0.42f);
    static readonly Color Ink = new Color(0.055f, 0.085f, 0.135f);
    static readonly Color Paper = new Color(0.88f, 0.91f, 0.93f);
    Font _font;
    float _savedScale, _age;
    bool _paused;
    int _section;
    Transform _content;
    RectTransform _panel;
    CanvasGroup _fade;
    Text _progress;
    Button _previous, _next;
    readonly Button[] _lessons = new Button[6];
    readonly Button[] _tabs = new Button[3];
    GameObject _previousSelection;

    public static void Show(Font font, int section = 0)
    {
        if (IsOpen) { s_instance.SelectLesson(section); return; }
        var root = new GameObject("FieldGuide", typeof(RectTransform));
        var guide = root.AddComponent<TutorialOverlay>();
        s_instance = guide;
        guide._font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        guide._savedScale = Time.timeScale;
        guide._paused = true;
        Time.timeScale = 0f;
        guide._previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        ModuleTooltipView.HideAll();
        guide.Build();
        guide.SelectLesson(section);
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(guide._next.gameObject);
    }

    public static void Close()
    {
        if (s_instance == null) return;
        TutorialOverlay guide = s_instance;
        guide.gameObject.SetActive(false); // Releases the pause synchronously, before deferred destruction.
        if (Application.isPlaying) Destroy(guide.gameObject);
        else DestroyImmediate(guide.gameObject);
    }

    void OnDisable()
    {
        if (_paused)
        {
            Time.timeScale = _savedScale;
            _paused = false;
            s_closedFrame = Time.frameCount;
            if (EventSystem.current != null && _previousSelection != null && _previousSelection.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(_previousSelection);
        }
        if (s_instance == this) s_instance = null;
    }

    void Update()
    {
        _age += Time.unscaledDeltaTime;
        float t = PresentationSettings.MotionEnabled ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / 0.18f)) : 1f;
        _fade.alpha = t;
        _panel.localScale = Vector3.one * Mathf.Lerp(0.985f, 1f, t);
        if (Input.GetKeyDown(KeyCode.Escape)) { Close(); return; }
        if (Input.GetKeyDown(KeyCode.RightArrow)) SelectLesson(_section + 1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) SelectLesson(_section - 1);
    }

    void Build()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        _fade = gameObject.AddComponent<CanvasGroup>();
        _fade.alpha = PresentationSettings.MotionEnabled ? 0f : 1f;
        Image shade = Box(transform, "Shade", 0, 0, 1, 1, new Color(0.015f, 0.025f, 0.045f, 0.9f));
        Image panel = Surface(shade.transform, "GuidePanel", 0.08f, 0.07f, 0.92f, 0.93f);
        _panel = panel.rectTransform;
        Label(panel.transform, "Eyebrow", 0.035f, 0.91f, 0.75f, 0.956f,
            T("TIMESANDS  /  EXPEDITION NOTES", "时之沙  /  探索手记"), 18, Gold);
        Label(panel.transform, "Title", 0.035f, 0.833f, 0.8f, 0.91f,
            T("Field Guide", "新手指南"), 38, Paper, true);
        ButtonAt(panel.transform, "Close", 0.85f, 0.875f, 0.965f, 0.952f, T("Close [Esc]", "关闭 [Esc]"), Close);
        Box(panel.transform, "Rule", 0.035f, 0.818f, 0.965f, 0.821f, new Color(0.53f, 0.41f, 0.25f));

        string[] tabNames = { T("Basics", "基础"), T("Controls", "操作"), T("Build Tips", "搭建技巧") };
        int[] tabSections = { 0, 1, 5 };
        for (int i = 0; i < 3; i++)
        {
            int target = tabSections[i];
            float x = 0.035f + i * 0.155f;
            _tabs[i] = ButtonAt(panel.transform, "Tab" + i, x, 0.742f, x + 0.145f, 0.8f, tabNames[i], () => SelectLesson(target));
        }
        Label(panel.transform, "PauseHint", 0.55f, 0.742f, 0.965f, 0.8f,
            T("Take your time. Your run is paused.", "慢慢阅读，游戏已暂停。"), 18, new Color(0.51f, 0.76f, 0.75f));

        Image lessons = Box(panel.transform, "Lessons", 0.035f, 0.16f, 0.265f, 0.72f, Ink);
        for (int i = 0; i < 6; i++)
        {
            int index = i;
            float top = 0.98f - i * 0.164f;
            _lessons[i] = ButtonAt(lessons.transform, "Lesson" + i, 0.035f, top - 0.142f, 0.965f, top,
                (i + 1).ToString("00") + "   " + LessonTitle(i), () => SelectLesson(index));
            _lessons[i].GetComponentInChildren<Text>().fontSize = 20;
        }
        _content = Box(panel.transform, "LessonContent", 0.285f, 0.16f, 0.965f, 0.72f, Ink).transform;
        _progress = Label(panel.transform, "Progress", 0.035f, 0.055f, 0.48f, 0.12f, "", 19, Gold);
        _previous = ButtonAt(panel.transform, "Previous", 0.61f, 0.05f, 0.755f, 0.125f,
            T("Previous", "上一页"), () => SelectLesson(_section - 1));
        _next = ButtonAt(panel.transform, "Next", 0.77f, 0.05f, 0.965f, 0.125f,
            T("Next lesson  >", "下一课  >"), () => { if (_section == 5) Close(); else SelectLesson(_section + 1); });
        Image frame = Box(panel.transform, "BrassFrame", 0, 0, 1, 1, Color.white);
        frame.sprite = PixelUiSprites.Frame;
        frame.type = Image.Type.Sliced;
        frame.pixelsPerUnitMultiplier = 1.5f;
        frame.raycastTarget = false;
        // Explicit navigation keeps keyboard/controller focus inside this modal.
        Button[] buttons = GetComponentsInChildren<Button>();
        for (int i = 0; i < buttons.Length; i++)
        {
            Navigation navigation = buttons[i].navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length];
            navigation.selectOnDown = buttons[(i + 1) % buttons.Length];
            // Left/right page lessons in Update; only up/down move button focus.
            navigation.selectOnLeft = null;
            navigation.selectOnRight = null;
            buttons[i].navigation = navigation;
        }
    }

    public void SelectLesson(int section)
    {
        _section = Mathf.Clamp(section, 0, 5);
        for (int i = _content.childCount - 1; i >= 0; i--)
        {
            GameObject child = _content.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        _progress.text = T("LESSON ", "课程 ") + (_section + 1) + " / 6    ·    " + T("←  →  to browse", "←  →  切换课程");
        _previous.interactable = _section > 0;
        _next.GetComponentInChildren<Text>().text = _section == 5 ? T("Close guide", "关闭指南") : T("Next lesson  >", "下一课  >");
        for (int i = 0; i < 6; i++) Style(_lessons[i], i == _section);
        for (int i = 0; i < 3; i++) Style(_tabs[i], i == (_section == 1 ? 1 : _section == 5 ? 2 : 0));
        Label(_content, "LessonTitle", 0.035f, 0.855f, 0.96f, 0.975f, LessonTitle(_section), 30, Gold, true);
        DrawLesson();
    }

    void DrawLesson()
    {
        switch (_section)
        {
            case 0:
                RouteDiagram();
                Body(T("BUY  →  PLACE  →  CONNECT", "购买  →  放置  →  连通"),
                    T("Buy a module with coins, then drag it from your hand onto an empty board cell. You can also drag directly from the shop.\n\nWeapons need energy balls from the emitter. A disconnected weapon cannot fire; trace a continuous route into it. Hover over any module to read its role and price.",
                    "使用金币购买模块，再从手牌拖到棋盘空格；也可以直接从商店拖到棋盘。\n\n武器需要发射器提供的能量球。未接通的武器无法开火，请沿路径检查能量球能否到达。悬停在模块上可查看作用与价格。"));
                Note(T("Your first goal: connect one weapon before adding more.", "第一个目标：先让一座武器接通能量，再继续扩建。"));
                break;
            case 1:
                RouteDiagram();
                Body(T("TURN THE PATH. KEEP THE CONNECTION.", "旋转路径，保持连通。"),
                    T("R rotates a selected or carried path module, including a board module while dragging. Match its openings to the incoming ball.\n\nMouse: buy, select, drag, move and merge.\nF: refresh the shop (costs coins unless free).\nSpace / Ready: start the wave after preparation.\nRight click / X over a module: return it to your hand.\nEsc: pause settings; closes this guide first.",
                    "按 R 旋转选中或拿起的路径模块，拖动棋盘模块时也可以旋转。让路径开口对准来球。\n\n鼠标：购买、选择、拖动、移动与合成。\nF：刷新商店（有免费次数时不花金币）。\nSpace / 准备完毕：结束准备并开始倒计时。\n右键 / 悬停时按 X：将棋盘模块拆回手牌。\nEsc：暂停设置；本指南打开时先关闭指南。"), 21);
                Note(T("Dismantling needs a free hand slot. Combat may require a cost and confirmation.", "拆回手牌需要空位；战斗中可能需要付费与确认。"));
                break;
            case 2:
                Modules(ModuleType.Projectile, ModuleType.Splitter, ModuleType.Miner);
                Body(T("SAND IS YOUR HEALTH — AND YOUR FUEL", "沙既是生命，也是燃料"),
                    T("During combat, the emitter spends hourglass sand to create energy balls. Routing modules guide those balls; attack modules turn energy into attacks. Support and economy modules have different jobs.\n\nFor example, the Miner turns energy into coins. Read descriptions before committing energy to a new branch. Running out of sand ends the run.",
                    "战斗时，发射器消耗沙漏中的沙来生成能量球。路径模块引导球前进，攻击模块把能量转为攻击；辅助与经济模块则各有用途。\n\n例如，采矿机将能量转换为金币。为新支路分配能量前，先阅读模块说明。沙耗尽时，本局结束。"));
                Note(T("Shop coins buy modules; hourglass sand sustains the run. Watch both.", "金币用于购买模块，沙漏中的沙维持本局。两者都需要留意。"));
                break;
            case 3:
                EnemyIllustration(EnemyGoldType.Normal, EnemyGoldType.Tank, EnemyGoldType.Boss);
                Body(T("PREPARE WITHOUT A TIMER", "准备阶段没有时间限制"),
                    T("Buy, arrange, merge and expand while preparing. Check the next-wave preview, then click Ready or press Space to begin the countdown.\n\nEnemies approach from the right. Breaches cost extra sand. Sand-buffed enemies and wave-clear rewards can replenish sand.\n\nSurvive 25 regular waves, then the Boss on wave 26. A Boss breach ends the run.",
                    "在准备阶段购买、搭建、合成并扩展棋盘。查看下一波敌人预览后，点击“准备完毕”或按 Space 开始倒计时。\n\n敌人从右侧逼近，漏怪会额外损失沙。带沙增益的敌人与过关奖励可以补充沙。\n\n撑过 25 个普通波次，再迎战第 26 波首领。首领突破防线时，本局结束。"));
                Note(T("Placement and shop actions pause during countdowns and draft choices.", "倒计时与选牌阶段会暂时禁止放置和商店操作。"));
                break;
            case 4:
                Modules(ModuleType.Spark, ModuleType.IceLaser, ModuleType.FlameAmp);
                Body(T("FIRE + CHILL = MELT", "灼烧 + 寒冷 = 融化"),
                    T("Burn damages enemies over time. Chill slows their movement. Hitting a burning enemy with a chill attack — or a chilled enemy with a burn attack — triggers Melt.\n\nMelt increases that hit's damage and clears both statuses. Cell enchantments and support modules can affect your setup; hover for details.\n\nEnemy types have different roles. Use the wave preview to see what comes next.",
                    "灼烧持续造成伤害，寒冷减慢敌人移动。用寒冷攻击命中灼烧中的敌人，或用灼烧攻击命中寒冷中的敌人，会触发融化。\n\n融化提高本次伤害，并清除两种状态。格子附魔与辅助模块也会影响搭建，请悬停查看详情。\n\n不同敌人各有特点，通过波次预览了解下一波。"));
                Note(T("Combine effects deliberately: Melt consumes the existing burn and chill.", "有意识地组合状态：融化会消耗已有的灼烧与寒冷。"));
                break;
            default:
                Modules(ModuleType.Redirector, ModuleType.Projectile, ModuleType.Purify);
                Body(T("THREE HABITS FOR A STRONGER CIRCUIT", "搭建电路的三个实用习惯"),
                    T("01  TRACE THE ROUTE\nFollow the ball from the emitter to each weapon; rotate path pieces before adding another branch.\n\n02  USE YOUR PREPARATION\nMove and merge compatible modules, inspect hover details and expand when useful. Preparation dismantling is free.\n\n03  PLAN YOUR SPENDING\nRead prices before buying or refreshing. Scrap sells a module for coins; dismantling returns it to your hand. Drafts, emitter upgrades, blessings and bindings add choices as the run progresses.",
                    "01  沿着能量球检查路径\n从发射器逐段检查到武器，先旋转路径接通，再增加支路。\n\n02  用好准备阶段\n移动与合成兼容模块、悬停查看详情，并按需扩展棋盘。准备阶段拆回手牌免费。\n\n03  规划金币支出\n购买或刷新前先查看价格。分解出售模块换金币；拆除则返回手牌。随着进程推进，还会出现选牌、发射器升级、祝福与束缚。"), 21);
                Note(T("You can reopen this guide any time from Settings → Field Guide.", "随时可从“设置 → 新手指南”重新阅读。"));
                break;
        }
    }

    void Body(string heading, string copy, int size = 22)
    {
        Label(_content, "Heading", 0.035f, 0.59f, 0.965f, 0.655f, heading, 18, new Color(0.45f, 0.81f, 0.79f), true);
        Label(_content, "Body", 0.035f, 0.13f, 0.965f, 0.585f, copy, size, Paper);
    }

    void Note(string copy)
    {
        Box(_content, "NoteBackground", 0.025f, 0.025f, 0.975f, 0.115f, new Color(0.12f, 0.15f, 0.19f));
        Label(_content, "Note", 0.045f, 0.03f, 0.955f, 0.11f, copy, 18, Gold);
    }

    void RouteDiagram()
    {
        Color line = new Color(0.35f, 0.72f, 0.74f);
        // The two bends use the actual Left↔Up / Right↔Down port orientations.
        Box(_content, "EnergyLineIn", 0.1f, 0.702f, 0.42f, 0.708f, line);
        Box(_content, "EnergyLineTurn", 0.418f, 0.705f, 0.422f, 0.81f, line);
        Box(_content, "EnergyLineOut", 0.42f, 0.807f, 0.84f, 0.813f, line);
        SpriteIcon(_content, "Emitter", 0.05f, 0.66f, 0.14f, 0.75f, PixelVfxSprites.Ring);
        SpriteIcon(_content, "Path", 0.375f, 0.66f, 0.465f, 0.75f, PixelArtSprites.Module(ModuleType.Redirector));
        SpriteIcon(_content, "PathTurn", 0.375f, 0.765f, 0.465f, 0.855f, PixelArtSprites.Module(ModuleType.Redirector, 2));
        SpriteIcon(_content, "Weapon", 0.795f, 0.765f, 0.885f, 0.855f, PixelArtSprites.Module(ModuleType.Projectile));
        Label(_content, "Flow1", 0.25f, 0.67f, 0.32f, 0.74f, ">", 26, Gold);
        Label(_content, "Flow2", 0.64f, 0.775f, 0.71f, 0.845f, ">", 26, Gold);
        Label(_content, "EmitterLabel", 0.035f, 0.755f, 0.3f, 0.805f, T("EMITTER", "发射器"), 16, Gold);
        Label(_content, "RouteLabel", 0.48f, 0.68f, 0.7f, 0.74f, T("ROUTE  /  R", "路径  /  R"), 16, Gold);
        Label(_content, "WeaponLabel", 0.76f, 0.68f, 0.965f, 0.74f, T("WEAPON", "武器"), 16, Gold);
    }

    void Modules(ModuleType first, ModuleType second, ModuleType third)
    {
        ModuleType[] types = { first, second, third };
        for (int i = 0; i < 3; i++)
        {
            float x = 0.06f + i * 0.31f;
            SpriteIcon(_content, "Module" + i, x, 0.67f, x + 0.14f, 0.835f, PixelArtSprites.Module(types[i]));
            Label(_content, "ModuleName" + i, x + 0.135f, 0.69f, x + 0.3f, 0.81f,
                ModuleCatalog.GetDisplayName(types[i]), 16, Gold);
        }
    }

    void EnemyIllustration(EnemyGoldType a, EnemyGoldType b, EnemyGoldType c)
    {
        EnemyGoldType[] types = { a, b, c };
        for (int i = 0; i < 3; i++)
        {
            float x = 0.12f + i * 0.3f;
            SpriteIcon(_content, "Enemy" + i, x, 0.67f, x + 0.14f, 0.835f, PixelEnemySprites.Get(types[i]));
        }
        Label(_content, "WaveIllustration", 0.65f, 0.675f, 0.965f, 0.73f, T("WAVE 26  /  BOSS", "第 26 波  /  首领"), 16, Gold);
    }

    static string LessonTitle(int section)
    {
        string[] english = { "Your first circuit", "Route & controls", "Modules & energy", "Ready for battle", "Elemental effects", "Build & upgrade" };
        string[] chinese = { "第一条电路", "路径与操作", "模块与能量", "准备迎战", "元素效果", "搭建与升级" };
        return GameLocalization.IsChinese ? chinese[section] : english[section];
    }
    static string T(string en, string zh) => GameLocalization.Text(en, zh);

    public static string EffectsQualityLabel()
    {
        string quality = PresentationSettings.Quality == PresentationSettings.EffectsQuality.Cinematic ? T("Cinematic", "华丽")
            : PresentationSettings.Quality == PresentationSettings.EffectsQuality.Balanced ? T("Balanced", "均衡") : T("Reduced", "精简");
        return T("Effects: ", "特效：") + quality;
    }

    public static string MotionLabel() => T("Motion: ", "动态：")
        + (PresentationSettings.MotionEnabled ? T("Full", "完整") : T("Reduced", "精简"));

    static void Style(Button button, bool selected)
    {
        button.GetComponent<Image>().color = selected ? new Color(0.49f, 0.57f, 0.61f) : new Color(0.72f, 0.78f, 0.84f);
        button.GetComponentInChildren<Text>().color = selected ? new Color(1f, 0.82f, 0.49f) : Paper;
    }

    Image Surface(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        Image image = Box(parent, name, x0, y0, x1, y1, Color.white);
        image.sprite = PixelUiSprites.Panel;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1.5f;
        return image;
    }

    Button ButtonAt(Transform parent, string name, float x0, float y0, float x1, float y1, string copy, UnityEngine.Events.UnityAction action)
    {
        Image image = Surface(parent, name, x0, y0, x1, y1);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.91f, 0.73f);
        colors.pressedColor = new Color(0.64f, 0.73f, 0.78f);
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.onClick.AddListener(action);
        Text text = Label(image.transform, "Label", 0.045f, 0.06f, 0.955f, 0.94f, copy, 21, Paper, true);
        text.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    static Image Box(Transform parent, string name, float x0, float y0, float x1, float y1, Color color)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = root.AddComponent<Image>(); image.color = color;
        return image;
    }

    static void SpriteIcon(Transform parent, string name, float x0, float y0, float x1, float y1, Sprite sprite)
    {
        Image image = Box(parent, name, x0, y0, x1, y1, Color.white);
        image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false;
    }

    Text Label(Transform parent, string name, float x0, float y0, float x1, float y1, string copy, int size, Color color, bool bold = false)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(x0, y0); rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var text = root.AddComponent<Text>();
        text.font = _font; text.fontSize = size; text.color = color;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.text = copy; text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = size - 3; text.resizeTextMaxSize = size;
        return text;
    }
}
