using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 准备阶段顶部面板：等待玩家就绪、下波敌人色圆、准备完毕、3-2-1。
/// </summary>
public class PrepPhasePanel : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text timerText;
    [SerializeField] Text hintText;
    [SerializeField] Text countdownText;
    [SerializeField] Button readyButton;
    [SerializeField] Text readyButtonLabel;
    [SerializeField] CanvasGroup rootGroup;
    [SerializeField] WavePreviewStrip wavePreview;

    WaveManager _waves;
    GameSession _session;

    public void Bind(
        Text title,
        Text timer,
        Text hint,
        Text countdown,
        Button ready,
        Text readyLabel,
        CanvasGroup group,
        WaveManager waves,
        GameSession session,
        WavePreviewStrip preview = null)
    {
        titleText = title;
        timerText = timer;
        hintText = hint;
        countdownText = countdown;
        readyButton = ready;
        readyButtonLabel = readyLabel;
        rootGroup = group;
        _waves = waves;
        _session = session;
        wavePreview = preview;

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted += OnPrepStarted;
            _waves.OnCountdownDigit += OnCountdownDigit;
            _waves.OnCombatStarted += OnCombatStarted;
        }

        if (readyButtonLabel != null)
        {
            readyButtonLabel.text = GameLocalization.Text("Ready [Space]", "准备完毕 [Space]");
        }

        SetPrepVisible(false);
        if (countdownText != null)
        {
            countdownText.text = string.Empty;
        }

        // WaveManager 可能已在 Bind 前进入准备（首波），补一次同步
        SyncIfPreparing();
    }

    public void SyncIfPreparing()
    {
        if (_waves == null || !_waves.IsPreparingPhase)
        {
            return;
        }

        OnPrepStarted(_waves.CurrentWaveDisplay, _waves.PrepDuration);
    }

    void OnDestroy()
    {
        if (readyButton != null)
        {
            readyButton.onClick.RemoveListener(OnReadyClicked);
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted -= OnPrepStarted;
            _waves.OnCountdownDigit -= OnCountdownDigit;
            _waves.OnCombatStarted -= OnCombatStarted;
        }
    }

    void Update()
    {
        if (_session != null && _session.IsPreparing && Input.GetKeyDown(KeyCode.Space))
        {
            OnReadyClicked();
        }
    }

    void OnReadyClicked()
    {
        _waves?.RequestReady();
    }

    void OnPrepStarted(int wave, float duration)
    {
        SetPrepVisible(true);
        if (titleText != null)
        {
            titleText.text = GameLocalization.Text(
                $"Wave {wave} Preparation",
                $"第 {wave} 波准备阶段");
        }

        if (timerText != null)
        {
            timerText.text = GameLocalization.Text("Waiting…", "等待中…");
            timerText.color = new Color(0.2f, 0.45f, 0.25f, 1f);
        }

        if (hintText != null)
        {
            hintText.text = GameLocalization.Text(
                "Buy, merge, and arrange — press Ready when done",
                "购买、合成并调整模块 — 完成后按准备完毕");
        }

        if (countdownText != null)
        {
            countdownText.text = string.Empty;
        }

        wavePreview?.ShowForWave(wave);
    }

    void OnCountdownDigit(int digit)
    {
        SetPrepVisible(false);
        if (countdownText == null)
        {
            return;
        }

        if (digit <= 0)
        {
            countdownText.text = GameLocalization.Text("START", "开始");
            countdownText.fontSize = 64;
            countdownText.color = new Color(1f, 0.35f, 0.35f, 1f);
            CancelInvoke(nameof(ClearCountdown));
            Invoke(nameof(ClearCountdown), 0.45f);
            return;
        }

        countdownText.text = digit.ToString();
        countdownText.fontSize = 72;
        countdownText.color = new Color(1f, 0.85f, 0.3f, 1f);
    }

    void ClearCountdown()
    {
        if (countdownText != null)
        {
            countdownText.text = string.Empty;
        }
    }

    void OnCombatStarted()
    {
        SetPrepVisible(false);
        wavePreview?.Hide();
    }

    void SetPrepVisible(bool visible)
    {
        if (rootGroup != null)
        {
            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.blocksRaycasts = visible;
            rootGroup.interactable = visible;
            if (rootGroup.gameObject != null)
            {
                rootGroup.gameObject.SetActive(true);
            }
        }

        if (readyButton != null)
        {
            readyButton.gameObject.SetActive(visible);
        }
    }
}
