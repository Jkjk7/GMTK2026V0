using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 胜负全屏遮罩：CanvasGroup 淡入，并拦截下层点击；可返回主菜单。
/// </summary>
public sealed class ResultOverlayView : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Text titleText;
    [SerializeField] Button menuButton;
    [SerializeField] Text menuButtonLabel;
    [SerializeField] float fadeSeconds = 0.35f;

    Coroutine _fadeRoutine;
    Action _onReturnToMenu;

    public void Bind(CanvasGroup group, Text title, Button menu = null, Text menuLabel = null)
    {
        canvasGroup = group;
        titleText = title;
        menuButton = menu;
        menuButtonLabel = menuLabel;

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
            menuButton.onClick.AddListener(OnMenuClicked);
        }

        HideImmediate();
    }

    public void SetReturnToMenuHandler(Action onReturnToMenu)
    {
        _onReturnToMenu = onReturnToMenu;
    }

    public void Show(string message, Color color)
    {
        if (titleText != null)
        {
            titleText.text = message;
            titleText.color = color;
        }

        if (menuButtonLabel != null)
        {
            menuButtonLabel.text = GameLocalization.Text("Main Menu", "返回主菜单");
        }

        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(true);
        }

        transform.SetAsLastSibling();
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeTo(1f));
    }

    public void HideImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (titleText != null)
        {
            titleText.text = string.Empty;
        }

        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    void OnMenuClicked()
    {
        if (_onReturnToMenu != null)
        {
            _onReturnToMenu.Invoke();
            return;
        }

        Time.timeScale = 1f;
        GameFlow.LoadMainMenu();
    }

    IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float start = canvasGroup.alpha;
        float t = 0f;
        while (t < fadeSeconds)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, t / fadeSeconds);
            yield return null;
        }

        canvasGroup.alpha = target;
    }
}
