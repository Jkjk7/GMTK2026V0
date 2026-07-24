using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 棋盘窗右下金币栏：金框浅金底、深棕数字、+/- 反馈、不足抖动。
/// </summary>
public class GoldPanel : MonoBehaviour
{
    [SerializeField] Text goldText;
    [SerializeField] Text deltaText;
    [SerializeField] RectTransform root;
    [SerializeField] Image background;

    float _deltaTimer;
    float _shakeTimer;
    Vector2 _basePos;

    public RectTransform Rect => root != null ? root : transform as RectTransform;

    public void Bind(Text valueLabel, Text feedbackLabel, Image bg, RectTransform panelRoot)
    {
        goldText = valueLabel;
        deltaText = feedbackLabel;
        background = bg;
        root = panelRoot != null ? panelRoot : transform as RectTransform;
        if (root != null)
        {
            _basePos = root.anchoredPosition;
        }

        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged += OnGoldChanged;
            Economy.Instance.OnGoldGained += OnGained;
            Economy.Instance.OnGoldSpent += OnSpent;
            Economy.Instance.OnGoldInsufficient += OnInsufficient;
            OnGoldChanged(Economy.Instance.CurrentGold);
        }

        if (deltaText != null)
        {
            deltaText.text = string.Empty;
        }
    }

    void OnDestroy()
    {
        if (Economy.Instance != null)
        {
            Economy.Instance.OnGoldChanged -= OnGoldChanged;
            Economy.Instance.OnGoldGained -= OnGained;
            Economy.Instance.OnGoldSpent -= OnSpent;
            Economy.Instance.OnGoldInsufficient -= OnInsufficient;
        }
    }

    void Update()
    {
        if (_deltaTimer > 0f)
        {
            _deltaTimer -= Time.deltaTime;
            if (_deltaTimer <= 0f && deltaText != null)
            {
                deltaText.text = string.Empty;
            }
        }

        if (_shakeTimer > 0f && root != null)
        {
            _shakeTimer -= Time.deltaTime;
            float x = Mathf.Sin(Time.time * 60f) * 6f;
            root.anchoredPosition = _basePos + new Vector2(x, 0f);
            if (_shakeTimer <= 0f)
            {
                root.anchoredPosition = _basePos;
            }
        }
    }

    void OnGoldChanged(int gold)
    {
        if (goldText != null)
        {
            goldText.text = "金币 " + gold;
        }
    }

    void OnGained(int amount) => ShowDelta(amount, new Color(0.25f, 0.45f, 0.15f, 1f));

    void OnSpent(int amount) => ShowDelta(-amount, new Color(0.55f, 0.2f, 0.1f, 1f));

    void OnInsufficient()
    {
        _shakeTimer = 0.35f;
        ShowDelta(0, new Color(0.7f, 0.15f, 0.1f, 1f));
        if (deltaText != null)
        {
            deltaText.text = "不足";
            _deltaTimer = 1f;
        }
    }

    void ShowDelta(int amount, Color color)
    {
        if (deltaText == null || amount == 0 && deltaText.text == "不足")
        {
            if (amount != 0 && deltaText != null)
            {
                deltaText.color = color;
                deltaText.text = amount > 0 ? $"+{amount}" : amount.ToString();
                _deltaTimer = 1.1f;
            }

            return;
        }

        deltaText.color = color;
        deltaText.text = amount > 0 ? $"+{amount}" : amount.ToString();
        _deltaTimer = 1.1f;
    }

    /// <summary>飞币到达后入账。</summary>
    public void CommitFlyGold(int amount)
    {
        if (Economy.Instance != null)
        {
            Economy.Instance.AddGold(amount);
        }

        if (root != null)
        {
            StopAllCoroutines();
            StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        if (root == null)
        {
            yield break;
        }

        Vector3 s0 = Vector3.one;
        Vector3 s1 = Vector3.one * 1.12f;
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            root.localScale = Vector3.Lerp(s0, s1, t / 0.12f);
            yield return null;
        }

        t = 0f;
        while (t < 0.12f)
        {
            t += Time.deltaTime;
            root.localScale = Vector3.Lerp(s1, s0, t / 0.12f);
            yield return null;
        }

        root.localScale = s0;
    }

    public Vector3 GetWorldIconPosition()
    {
        if (root == null)
        {
            return Vector3.zero;
        }

        Canvas canvas = root.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera;
        }

        Vector3 screen = RectTransformUtility.WorldToScreenPoint(cam, root.position);
        Camera worldCam = Camera.main;
        if (worldCam == null)
        {
            return root.position;
        }

        screen.z = 10f;
        return worldCam.ScreenToWorldPoint(screen);
    }
}
