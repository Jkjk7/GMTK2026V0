using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 胜负全屏遮罩：CanvasGroup 淡入，并拦截下层点击。
/// </summary>
public sealed class ResultOverlayView : MonoBehaviour
{
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Text titleText;
    [SerializeField] float fadeSeconds = 0.35f;

    Coroutine _fadeRoutine;

    public void Bind(CanvasGroup group, Text title)
    {
        canvasGroup = group;
        titleText = title;
        HideImmediate();
    }

    public void Show(string message, Color color)
    {
        if (titleText != null)
        {
            titleText.text = message;
            titleText.color = color;
        }

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

        gameObject.SetActive(false);
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
