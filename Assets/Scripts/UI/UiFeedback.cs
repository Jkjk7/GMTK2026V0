using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Cached, non-interactive decoration. Never owns an action, transaction or game clock.</summary>
public sealed class UiFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    enum Kind { None, Reject, Receive, Success }
    Kind _kind;
    float _age, _hoverAmount, _pressAmount;
    bool _pointer, _selected, _pressed, _hoverEnabled, _captured;
    RectTransform _rect, _decoration;
    Vector2 _basePosition;
    Vector3 _baseScale;
    Vector2 _lastPosition;
    Vector3 _lastScale;
    Image _fill, _frame, _star, _messageBack;
    Text _message;
    readonly Image[] _sparks = new Image[8];
    static readonly Color Gold = new Color(1f, .82f, .35f);
    static readonly Color Red = new Color(1f, .26f, .18f);
    const float Duration = .58f;

    public static UiFeedback Attach(GameObject target, bool hover = true)
    {
        if (target == null || !(target.transform is RectTransform)) return null;
        var feedback = target.GetComponent<UiFeedback>();
        if (feedback == null) feedback = target.AddComponent<UiFeedback>();
        feedback._rect = (RectTransform)target.transform;
        feedback._hoverEnabled = hover;
        return feedback;
    }

    static UiFeedback For(Transform target)
    {
        if (target == null || !(target is RectTransform)) return null;
        return target.GetComponent<UiFeedback>() ?? Attach(target.gameObject, false);
    }

    public static void Reject(Transform target, string message = null) => For(target)?.StartCue(Kind.Reject, message);
    public static void Receive(Transform target) => For(target)?.StartCue(Kind.Receive, null);
    public static void Success(Transform target) => For(target)?.StartCue(Kind.Success, null);
    public static void Cancel(Transform target)
    {
        if (target != null) target.GetComponent<UiFeedback>()?.ResetFeedback();
    }

    void Capture()
    {
        if (_captured) return;
        if (_rect == null) _rect = transform as RectTransform;
        if (_rect == null) return;
        _basePosition = _rect.anchoredPosition;
        _baseScale = _rect.localScale;
        _lastPosition = _basePosition;
        _lastScale = _baseScale;
        _captured = true;
        EnsureDecoration();
    }

    void StartCue(Kind kind, string message)
    {
        if (!gameObject.activeInHierarchy) return;
        Capture();
        if (!_captured) return;
        _kind = kind;
        _age = 0f;
        _message.text = message ?? string.Empty;
        _decoration.SetAsLastSibling();
        _decoration.gameObject.SetActive(true);
        Draw();
    }

    bool CanRespond()
    {
        if (!_hoverEnabled || !isActiveAndEnabled) return false;
        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null && !selectable.IsInteractable()) return false;
        ShopSlot shop = GetComponent<ShopSlot>();
        if (shop != null && !shop.IsOccupied) return false;
        HandSlot hand = GetComponent<HandSlot>();
        return hand == null || hand.IsOccupied;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!CanRespond()) return;
        _pointer = true;
        Capture();
    }
    public void OnPointerExit(PointerEventData eventData) { _pointer = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !CanRespond()) return;
        _pressed = true;
        Capture();
    }
    public void OnPointerUp(PointerEventData eventData) { _pressed = false; }
    public void OnSelect(BaseEventData eventData)
    {
        if (!CanRespond()) return;
        _selected = true;
        Capture();
    }
    public void OnDeselect(BaseEventData eventData) { _selected = false; _pressed = false; }
    public void OnSubmit(BaseEventData eventData) { if (CanRespond()) Success(transform); }

    void LateUpdate() { if (_captured) Tick(Time.unscaledDeltaTime); }
    void Tick(float delta)
    {
        if (!_captured) return;
        delta = Mathf.Max(0f, delta);
        if ((_pointer || _selected || _pressed) && !CanRespond())
            _pointer = _selected = _pressed = false;
        _age += delta;
        float blend = 1f - Mathf.Exp(-delta * 22f);
        _hoverAmount = Mathf.Lerp(_hoverAmount, (_pointer || _selected) ? 1f : 0f, blend);
        _pressAmount = Mathf.Lerp(_pressAmount, _pressed ? 1f : 0f, blend);
        if (_age >= Duration) _kind = Kind.None;
        if (_kind == Kind.None && !_pointer && !_selected && !_pressed
            && _hoverAmount < .001f && _pressAmount < .001f)
        {
            ResetFeedback();
            return;
        }
        Draw();
    }

    void Draw()
    {
        ObserveLayout();
        bool pulse = _kind != Kind.None;
        bool motion = PresentationSettings.MotionEnabled;
        float t = Mathf.Clamp01(_age / Duration);
        float fade = pulse ? (1f - t) * (1f - t) : 0f;
        float pop = pulse ? Mathf.Sin(t * Mathf.PI) * (1f - t) : 0f;
        float x = _kind == Kind.Reject ? Mathf.Sin(_age * 68f) * 8f * fade : 0f;
        float y = _kind == Kind.Receive ? pop * 7f : 0f;
        float scale = 1f + _hoverAmount * .025f - _pressAmount * .045f;
        if (_kind == Kind.Receive) scale += pop * .13f;
        else if (_kind == Kind.Success) scale += pop * .055f;
        _rect.anchoredPosition = _basePosition + (motion ? new Vector2(x, y) : Vector2.zero);
        _rect.localScale = Vector3.Scale(_baseScale, Vector3.one * (motion ? scale : 1f));
        _lastPosition = _rect.anchoredPosition;
        _lastScale = _rect.localScale;
        _decoration.gameObject.SetActive(true);
        Color tint = _kind == Kind.Reject ? Red : Gold;
        _fill.color = WithAlpha(tint, pulse ? fade * (_kind == Kind.Reject ? .22f : .16f) : 0f);
        _frame.color = WithAlpha(tint, Mathf.Max(_hoverAmount * .5f, fade));
        _star.enabled = _kind == Kind.Receive;
        if (_star.enabled)
        {
            _star.color = WithAlpha(new Color(1f, .95f, .64f), fade);
            _star.rectTransform.anchoredPosition = new Vector2(0, _rect.rect.height * .48f + (motion ? t * 18f : 0));
            float starSize = motion ? 16f + pop * 28f : 22f;
            _star.rectTransform.sizeDelta = Vector2.one * starSize;
        }
        int count = Mathf.RoundToInt(Mathf.Lerp(2, 8, PresentationSettings.ParticleIntensity));
        for (int i = 0; i < _sparks.Length; i++)
        {
            Image spark = _sparks[i];
            spark.enabled = motion && _kind == Kind.Receive && i < count;
            if (!spark.enabled) continue;
            float angle = (25f + i * 130f) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            float radius = 12f + Mathf.Sin(t * Mathf.PI * .5f) * 38f;
            spark.rectTransform.anchoredPosition = new Vector2(direction.x * radius, _rect.rect.height * .35f + direction.y * radius);
            spark.rectTransform.sizeDelta = Vector2.one * Mathf.Lerp(i % 3 == 0 ? 8f : 5f, 2f, t);
            spark.color = WithAlpha(i % 2 == 0 ? Gold : new Color(.5f, 1f, .92f), fade);
        }
        _message.enabled = _kind == Kind.Reject && !string.IsNullOrEmpty(_message.text);
        _messageBack.enabled = _message.enabled;
        if (_message.enabled)
        {
            float opacity = Mathf.Clamp01((1f - t) * 2f);
            _message.color = WithAlpha(new Color(1f, .67f, .5f), opacity);
            _message.rectTransform.anchoredPosition = new Vector2(0, _rect.rect.height * .5f + 16f + (motion ? t * 9f : 0));
            _messageBack.rectTransform.anchoredPosition = _message.rectTransform.anchoredPosition;
            _messageBack.rectTransform.sizeDelta = new Vector2(Mathf.Clamp(_message.preferredWidth + 18f, 80f, 250f), 27f);
            _messageBack.color = WithAlpha(new Color(.045f, .055f, .085f), opacity * .96f);
        }
    }

    void EnsureDecoration()
    {
        if (_decoration != null) return;
        Font font = null;
        foreach (Text label in GetComponentsInChildren<Text>(true)) if (label.font != null) { font = label.font; break; }
        var root = new GameObject("FeedbackDecoration", typeof(RectTransform));
        _decoration = (RectTransform)root.transform;
        _decoration.SetParent(transform, false);
        _decoration.anchorMin = Vector2.zero; _decoration.anchorMax = Vector2.one;
        _decoration.offsetMin = _decoration.offsetMax = Vector2.zero;
        _fill = Image("Flash", PrototypeSprites.Square, true);
        _frame = Image("Edge", PixelUiSprites.Frame, true);
        _frame.type = UnityEngine.UI.Image.Type.Sliced;
        _frame.pixelsPerUnitMultiplier = 2f;
        _star = Image("ReceiptStar", PixelVfxSprites.Burst, false);
        for (int i = 0; i < _sparks.Length; i++) _sparks[i] = Image("Spark" + i, PixelVfxSprites.Pixel, false);
        _messageBack = Image("MessageBacking", PrototypeSprites.Square, false);
        var text = new GameObject("FeedbackMessage", typeof(RectTransform));
        text.transform.SetParent(_decoration, false);
        _message = text.AddComponent<Text>();
        _message.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _message.fontSize = 18;
        _message.alignment = TextAnchor.MiddleCenter;
        _message.raycastTarget = false;
        _message.horizontalOverflow = HorizontalWrapMode.Overflow;
        _message.verticalOverflow = VerticalWrapMode.Overflow;
        _message.rectTransform.sizeDelta = new Vector2(Mathf.Max(200, _rect.rect.width + 20), 28);
        root.SetActive(false);
    }

    Image Image(string name, Sprite sprite, bool stretch)
    {
        var root = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)root.transform;
        rect.SetParent(_decoration, false);
        if (stretch)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        var image = root.AddComponent<Image>();
        image.sprite = sprite; image.raycastTarget = false;
        image.color = Color.clear;
        return image;
    }

    static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }

    void ObserveLayout()
    {
        if (!_captured || _rect == null) return;
        // A GridLayoutGroup/fullscreen resize can replace the pose between feedback frames.
        // Treat that externally assigned pose as the new baseline, never restore an old grid cell.
        if (_rect.anchoredPosition != _lastPosition) _basePosition = _rect.anchoredPosition;
        if (_rect.localScale != _lastScale) _baseScale = _rect.localScale;
    }

    public void ResetFeedback()
    {
        ObserveLayout();
        if (_captured && _rect != null) { _rect.anchoredPosition = _basePosition; _rect.localScale = _baseScale; }
        _kind = Kind.None;
        _age = _hoverAmount = _pressAmount = 0f;
        _pointer = _selected = _pressed = _captured = false;
        if (_decoration != null) _decoration.gameObject.SetActive(false);
    }
    void OnDisable() => ResetFeedback();
}
