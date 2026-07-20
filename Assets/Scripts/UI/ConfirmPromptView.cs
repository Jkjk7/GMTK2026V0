using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用确认框：拆除 / 分解。
/// </summary>
public class ConfirmPromptView : MonoBehaviour
{
    [SerializeField] CanvasGroup group;
    [SerializeField] Text titleText;
    [SerializeField] Text bodyText;
    [SerializeField] Text warnText;
    [SerializeField] Button confirmButton;
    [SerializeField] Button cancelButton;
    [SerializeField] Text confirmLabel;

    Action _onConfirm;
    Action _onCancel;
    bool _open;

    public bool IsOpen => _open;

    public void Bind(
        CanvasGroup canvasGroup,
        Text title,
        Text body,
        Text warn,
        Button confirm,
        Button cancel,
        Text confirmText)
    {
        group = canvasGroup;
        titleText = title;
        bodyText = body;
        warnText = warn;
        confirmButton = confirm;
        cancelButton = cancel;
        confirmLabel = confirmText;

        confirmButton?.onClick.AddListener(() =>
        {
            Action a = _onConfirm;
            Close();
            a?.Invoke();
        });
        cancelButton?.onClick.AddListener(() =>
        {
            Action a = _onCancel;
            Close();
            a?.Invoke();
        });

        Close();
    }

    public void Show(
        string title,
        string body,
        string confirmText,
        bool canConfirm,
        string warn,
        Action onConfirm,
        Action onCancel = null)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _open = true;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (bodyText != null)
        {
            bodyText.text = body;
        }

        if (warnText != null)
        {
            warnText.text = warn ?? string.Empty;
            warnText.gameObject.SetActive(!string.IsNullOrEmpty(warn));
        }

        if (confirmLabel != null)
        {
            confirmLabel.text = confirmText;
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }

        if (group != null)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
        }

        gameObject.SetActive(true);
    }

    public void Close()
    {
        _open = false;
        _onConfirm = null;
        _onCancel = null;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }
}
