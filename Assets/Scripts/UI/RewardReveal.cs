using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable reward ceremony. No gameplay callbacks, random numbers, materials or input locks.</summary>
[DisallowMultipleComponent]
public sealed class RewardReveal : MonoBehaviour
{
    const int MaxMotes = 12;
    const float CardDuration = 0.33f;
    const float Stagger = 0.055f;
    static readonly Color Violet = new Color(0.66f, 0.46f, 0.96f);
    static readonly Color Cyan = new Color(0.43f, 0.91f, 0.96f);
    static readonly Color Brass = new Color(0.94f, 0.75f, 0.43f);
    static Sprite _sigil, _upgradeEmblem;

    // Cached, one-pixel geometry stays delicate when enlarged behind the rewards.
    static Sprite Sigil => _sigil != null ? _sigil : (_sigil = BuildSigil(false));
    public static Sprite UpgradeEmblem => _upgradeEmblem != null ? _upgradeEmblem : (_upgradeEmblem = BuildSigil(true));
    public static Color CustomAccent(int index) => index % 3 == 1 ? Cyan : index % 3 == 2 ? Violet : Brass;

    sealed class Card
    {
        public Button Button;
        public RectTransform Motion;
        public Vector3 Position, Scale;
        public Quaternion Rotation;
        public Image Frame, Glint, Seal;
        public Color Tint;
    }

    RectTransform _root;
    Card[] _cards;
    RectTransform _decoration;
    Image _ring, _innerRing;
    readonly Image[] _motes = new Image[MaxMotes];
    readonly Vector2[] _motePositions = new Vector2[MaxMotes];
    float _age;
    bool _showing;

    public static RewardReveal Attach(RectTransform root, Button[] buttons)
    {
        if (root == null) return null;
        var reveal = root.GetComponent<RewardReveal>();
        if (reveal == null) reveal = root.gameObject.AddComponent<RewardReveal>();
        reveal.Initialize(root, buttons);
        return reveal;
    }

    void Initialize(RectTransform root, Button[] buttons)
    {
        if (_cards != null) return;
        _root = root;
        _cards = new Card[buttons != null ? buttons.Length : 0];
        for (int i = 0; i < _cards.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;
            RectTransform original = button.transform as RectTransform;
            var holder = new GameObject(button.name + "_Reveal", typeof(RectTransform));
            RectTransform motion = holder.GetComponent<RectTransform>();
            motion.SetParent(original.parent, false);
            motion.SetSiblingIndex(original.GetSiblingIndex());
            motion.anchorMin = original.anchorMin;
            motion.anchorMax = original.anchorMax;
            motion.pivot = original.pivot;
            motion.sizeDelta = original.sizeDelta;
            motion.anchoredPosition3D = original.anchoredPosition3D;
            motion.localScale = original.localScale;
            motion.localRotation = original.localRotation;
            original.SetParent(motion, false);
            original.anchorMin = Vector2.zero;
            original.anchorMax = Vector2.one;
            original.sizeDelta = Vector2.zero;
            original.anchoredPosition3D = Vector3.zero;
            original.localScale = Vector3.one;
            original.localRotation = Quaternion.identity;
            Image surface = button.GetComponent<Image>();
            if (surface != null)
            {
                surface.sprite = PixelUiSprites.Panel;
                surface.type = Image.Type.Sliced;
                surface.pixelsPerUnitMultiplier = 2;
                surface.color = Color.white;
                button.targetGraphic = surface;
            }
            Image frame = CreateImage("RarityFrame", original, PixelUiSprites.Frame);
            Full(frame.rectTransform, 0);
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 2;
            Image glint = CreateImage("RarityGlint", original, PixelUiSprites.Frame);
            Full(glint.rectTransform, 2);
            glint.type = Image.Type.Sliced;
            glint.pixelsPerUnitMultiplier = 2;
            Image seal = CreateImage("RaritySeal", original, PixelVfxSprites.Ring);
            seal.rectTransform.anchorMin = seal.rectTransform.anchorMax = new Vector2(0.5f, 1);
            seal.rectTransform.sizeDelta = new Vector2(22, 22);
            seal.rectTransform.anchoredPosition = new Vector2(0, -2);
            _cards[i] = new Card
            {
                Button = button, Motion = motion, Position = motion.anchoredPosition3D,
                Scale = motion.localScale, Rotation = motion.localRotation,
                Frame = frame, Glint = glint, Seal = seal, Tint = Brass
            };
        }
        var decoration = new GameObject("RewardRunes", typeof(RectTransform));
        _decoration = decoration.GetComponent<RectTransform>();
        _decoration.SetParent(root, false);
        _decoration.SetAsFirstSibling();
        Full(_decoration, 0);
        _ring = CreateImage("VioletCircle", _decoration, Sigil);
        _innerRing = CreateImage("CyanCircle", _decoration, Sigil);
        for (int i = 0; i < MaxMotes; i++)
        {
            _motes[i] = CreateImage("Rune" + i, _decoration, i % 3 == 0 ? PixelVfxSprites.Burst : PixelVfxSprites.Chip);
            float size = i % 3 == 0 ? 15 : 10;
            _motes[i].rectTransform.sizeDelta = new Vector2(size, size);
        }
        Hide();
    }

    public void Show(Color[] rarities = null)
    {
        if (_cards == null) return;
        RestoreCards();
        _age = 0;
        _showing = true;
        for (int i = 0; i < _cards.Length; i++)
        {
            Card card = _cards[i];
            if (card == null) continue;
            card.Tint = rarities != null && i < rarities.Length ? rarities[i] : CustomAccent(i);
            card.Frame.color = Alpha(card.Tint, 0.72f);
            card.Seal.color = card.Tint;
        }
        LayoutRunes();
        _decoration.gameObject.SetActive(true);
        enabled = true;
        Tick(0);
    }

    public void Hide()
    {
        _showing = false;
        RestoreCards();
        if (_decoration != null) _decoration.gameObject.SetActive(false);
        enabled = false;
    }

    void LayoutRunes()
    {
        float width = Mathf.Max(240, _root.rect.width);
        float height = Mathf.Max(200, _root.rect.height);
        float diameter = Mathf.Round(Mathf.Min(height * 1.03f, width * 0.76f));
        _ring.rectTransform.sizeDelta = new Vector2(diameter, diameter);
        _innerRing.rectTransform.sizeDelta = new Vector2(diameter * 0.81f, diameter * 0.81f);
        _ring.rectTransform.anchoredPosition = _innerRing.rectTransform.anchoredPosition = new Vector2(0, -height * 0.02f);
        int count = Mathf.Clamp(Mathf.RoundToInt(MaxMotes * PresentationSettings.ParticleIntensity), 0, MaxMotes);
        for (int i = 0; i < MaxMotes; i++)
        {
            // Fixed, evenly distributed runes: reward presentation never advances gameplay RNG.
            float angle = i * Mathf.PI * 2 / MaxMotes + Mathf.PI / 12;
            Vector2 position = new Vector2(Mathf.Cos(angle) * width * 0.46f, Mathf.Sin(angle) * height * 0.39f - height * 0.02f);
            _motePositions[i] = new Vector2(Mathf.Round(position.x), Mathf.Round(position.y));
            _motes[i].rectTransform.anchoredPosition = _motePositions[i];
            _motes[i].rectTransform.localScale = Vector3.one;
            _motes[i].rectTransform.localRotation = Quaternion.identity;
            // Spread reduced budgets around the ring instead of filling only its first quadrant.
            _motes[i].gameObject.SetActive(MoteEnabled(i, count));
        }
    }

    void Update() => Tick(Time.unscaledDeltaTime);

    void Tick(float unscaledDelta)
    {
        if (!_showing) return;
        _age += Mathf.Max(0, unscaledDelta);
        bool motion = PresentationSettings.MotionEnabled;
        for (int i = 0; i < _cards.Length; i++)
        {
            Card card = _cards[i];
            if (card == null) continue;
            float t = Mathf.Clamp01((_age - i * Stagger) / CardDuration);
            if (motion && card.Button.gameObject.activeSelf)
            {
                float spring = ModalReveal.Spring(t);
                float scale = Mathf.LerpUnclamped(0.84f, 1, spring);
                card.Motion.localScale = Vector3.Scale(card.Scale, new Vector3(scale, scale, 1));
                card.Motion.anchoredPosition3D = card.Position + Vector3.up * Mathf.Round(Mathf.LerpUnclamped(-24, 0, spring));
            }
            else Restore(card);
            float glint = Mathf.Sin(Mathf.Clamp01((t - 0.28f) / 0.72f) * Mathf.PI);
            card.Glint.color = Alpha(Color.Lerp(card.Tint, Color.white, 0.7f), glint * 0.86f);
            if (t >= 1) Restore(card);
        }
        float ceremony = Mathf.Clamp01(_age / 0.58f);
        float pulse = Mathf.Sin(ceremony * Mathf.PI);
        _ring.color = Alpha(Violet, 0.10f + pulse * 0.18f);
        _innerRing.color = Alpha(Cyan, 0.07f + pulse * 0.13f);
        float ringScale = motion ? Mathf.Lerp(0.91f, 1, Mathf.Clamp01(_age / 0.35f)) : 1;
        _ring.rectTransform.localScale = Vector3.one * ringScale;
        _innerRing.rectTransform.localScale = Vector3.one * ringScale;
        int moteBudget = Mathf.Clamp(Mathf.RoundToInt(MaxMotes * PresentationSettings.ParticleIntensity), 0, MaxMotes);
        for (int i = 0; i < MaxMotes; i++)
        {
            Image mote = _motes[i];
            bool active = MoteEnabled(i, moteBudget);
            if (mote.gameObject.activeSelf != active) mote.gameObject.SetActive(active);
            if (!active) continue;
            Color tint = i % 3 == 0 ? Brass : i % 3 == 1 ? Violet : Cyan;
            mote.color = Alpha(tint, 0.25f + pulse * 0.6f);
            Vector2 offset = motion ? _motePositions[i].normalized * Mathf.Round(pulse * 10) : Vector2.zero;
            mote.rectTransform.anchoredPosition = _motePositions[i] + offset;
        }
        if (ceremony >= 1)
        {
            RestoreCards();
            // Leave the quiet static sigil behind the cards; stop all per-frame work.
            _showing = false;
            enabled = false;
        }
    }

    void RestoreCards()
    {
        if (_cards == null) return;
        foreach (Card card in _cards)
        {
            if (card == null) continue;
            Restore(card);
            if (card.Glint != null) card.Glint.color = Color.clear;
        }
    }

    static void Restore(Card card)
    {
        if (card.Motion == null) return;
        card.Motion.anchoredPosition3D = card.Position;
        card.Motion.localScale = card.Scale;
        card.Motion.localRotation = card.Rotation;
    }

    void OnDisable()
    {
        RestoreCards();
        if (_showing && _decoration != null) _decoration.gameObject.SetActive(false);
        _showing = false;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var child = new GameObject(name, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(parent, false);
        var image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    static void Full(RectTransform rect, float outset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * -outset;
        rect.offsetMax = Vector2.one * outset;
    }

    static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }

    static bool MoteEnabled(int index, int budget) =>
        budget > 0 && (index == 0 || index * budget / MaxMotes != (index - 1) * budget / MaxMotes);

    static Sprite BuildSigil(bool emblem)
    {
        const int size = 96;
        var pixels = new Color32[size * size];
        var white = new Color32(255, 255, 255, 255);
        void Dot(int x, int y)
        {
            if (x >= 0 && x < size && y >= 0 && y < size) pixels[y * size + x] = white;
        }
        void Line(int x0, int y0, int x1, int y1)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                Dot(x0, y0);
                if (x0 == x1 && y0 == y1) break;
                int twice = error * 2;
                if (twice >= dy) { error += dy; x0 += sx; }
                if (twice <= dx) { error += dx; y0 += sy; }
            }
        }
        Vector2Int Point(float radius, float angle) => new Vector2Int(
            Mathf.RoundToInt(47.5f + Mathf.Cos(angle) * radius),
            Mathf.RoundToInt(47.5f + Mathf.Sin(angle) * radius));
        void Polygon(float radius, int sides, float angleOffset)
        {
            for (int i = 0; i < sides; i++)
            {
                Vector2Int from = Point(radius, angleOffset + i * Mathf.PI * 2 / sides);
                Vector2Int to = Point(radius, angleOffset + (i + 1) * Mathf.PI * 2 / sides);
                Line(from.x, from.y, to.x, to.y);
            }
        }
        Polygon(emblem ? 36 : 43, 12, Mathf.PI / 12);
        Polygon(emblem ? 28 : 33, 8, Mathf.PI / 8);
        Polygon(emblem ? 19 : 25, 4, 0);
        if (emblem)
        {
            Polygon(12, 4, 0);
            // An abstract rising diamond denotes a boon without impersonating a module.
            Line(41, 48, 48, 56); Line(48, 56, 55, 48);
            Line(48, 38, 48, 55);
            for (int y = 46; y <= 49; y++)
                for (int x = 46; x <= 49; x++) Dot(x, y);
        }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            Vector2Int from = Point(emblem ? 31 : 36, angle);
            Vector2Int to = Point(emblem ? 42 : 46, angle);
            Line(from.x, from.y, to.x, to.y);
            if (!emblem && i % 2 == 0)
            {
                Vector2Int inner = Point(15, angle);
                Vector2Int outer = Point(23, angle);
                Line(inner.x, inner.y, outer.x, outer.y);
            }
        }
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = emblem ? "PixelUpgradeEmblem" : "PixelRewardSigil";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        sprite.name = texture.name;
        return sprite;
    }
}
