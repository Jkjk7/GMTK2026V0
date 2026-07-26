using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 准备阶段顶部面板：倒计时、进度条、准备完毕、3-2-1。
/// </summary>
public class PrepPhasePanel : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text timerText;
    [SerializeField] Text hintText;
    [SerializeField] Text countdownText;
    [SerializeField] Image progressFill;
    [SerializeField] Button readyButton;
    [SerializeField] Text readyButtonLabel;
    [SerializeField] CanvasGroup rootGroup;

    WaveManager _waves;
    GameSession _session;
    bool _visible;

    public void Bind(
        Text title,
        Text timer,
        Text hint,
        Text countdown,
        Image fill,
        Button ready,
        Text readyLabel,
        CanvasGroup group,
        WaveManager waves,
        GameSession session)
    {
        titleText = title;
        timerText = timer;
        hintText = hint;
        countdownText = countdown;
        progressFill = fill;
        readyButton = ready;
        readyButtonLabel = readyLabel;
        rootGroup = group;
        _waves = waves;
        _session = session;

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        if (_waves != null)
        {
            _waves.OnPrepStarted += OnPrepStarted;
            _waves.OnPrepTick += OnPrepTick;
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
        OnPrepTick(_waves.PrepRemaining, _waves.PrepDuration);
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
            _waves.OnPrepTick -= OnPrepTick;
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

        if (hintText != null)
        {
            hintText.text = GameLocalization.Text(
                "Buy, merge, and arrange modules",
                "购买、合成并调整模块");
        }

        if (countdownText != null)
        {
            countdownText.text = string.Empty;
        }

        OnPrepTick(duration, duration);
    }

    void OnPrepTick(float remaining, float duration)
    {
        if (!_visible)
        {
            return;
        }

        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(remaining);
            timerText.text = string.Format("{0:00}:{1:00}", sec / 60, sec % 60);
            timerText.color = remaining <= 5f
                ? new Color(1f, 0.55f, 0.2f, 1f)
                : new Color(0.2f, 0.45f, 0.25f, 1f);
        }

        if (progressFill != null && duration > 0.01f)
        {
            progressFill.fillAmount = Mathf.Clamp01(remaining / duration);
            progressFill.color = remaining <= 5f
                ? new Color(1f, 0.5f, 0.15f, 0.95f)
                : new Color(0.35f, 0.85f, 0.55f, 0.95f);
        }
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
    }

    void SetPrepVisible(bool visible)
    {
        _visible = visible;
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
