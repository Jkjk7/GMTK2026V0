using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：波次/伤害文字、漏怪罚沙警告、胜负 CanvasGroup 遮罩。
/// 生存资源为沙漏时间（SandClock），倒计时显示由 SandClockPanel 负责。
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [SerializeField] Text statusText;
    [SerializeField] Text breachText;
    [SerializeField] Image breachFlash;
    [SerializeField] ResultOverlayView resultOverlay;
    [SerializeField] float breachMessageSeconds = 2.5f;

    SandClock _sandClock;
    WaveManager _waves;
    GameSession _session;
    DamageTracker _damageTracker;
    UIAudioFeedback _audio;

    float _breachMessageTimer;
    float _flashTimer;

    public void Initialize(
        Text statusLabel,
        Text breachLabel,
        Image flashImage,
        ResultOverlayView overlay,
        SandClock sandClock,
        WaveManager waves,
        GameSession session,
        DamageTracker damageTracker,
        UIAudioFeedback audio = null)
    {
        statusText = statusLabel;
        breachText = breachLabel;
        breachFlash = flashImage;
        resultOverlay = overlay;
        _sandClock = sandClock;
        _waves = waves;
        _session = session;
        _damageTracker = damageTracker;
        _audio = audio;

        if (_sandClock != null)
        {
            _sandClock.OnPenalty += OnPenalty;
        }

        if (_waves != null)
        {
            _waves.OnWaveChanged += OnWaveChanged;
        }

        if (_session != null)
        {
            _session.OnVictory += OnVictory;
            _session.OnDefeat += OnDefeat;
        }

        if (breachText != null)
        {
            breachText.gameObject.SetActive(false);
        }

        if (breachFlash != null)
        {
            Color c = breachFlash.color;
            c.a = 0f;
            breachFlash.color = c;
            breachFlash.raycastTarget = false;
        }

        resultOverlay?.HideImmediate();
        RefreshStatus();
    }

    void OnDestroy()
    {
        if (_sandClock != null)
        {
            _sandClock.OnPenalty -= OnPenalty;
        }

        if (_waves != null)
        {
            _waves.OnWaveChanged -= OnWaveChanged;
        }

        if (_session != null)
        {
            _session.OnVictory -= OnVictory;
            _session.OnDefeat -= OnDefeat;
        }
    }

    void Update()
    {
        if (_breachMessageTimer > 0f)
        {
            _breachMessageTimer -= Time.deltaTime;
            if (_breachMessageTimer <= 0f && breachText != null)
            {
                breachText.gameObject.SetActive(false);
                RefreshStatus();
            }
        }
        else if (_session != null && _session.State == GameSessionState.Preparing)
        {
            RefreshStatus();
        }

        if (_flashTimer > 0f && breachFlash != null)
        {
            _flashTimer -= Time.deltaTime;
            Color c = breachFlash.color;
            c.a = Mathf.Clamp01(_flashTimer / 0.35f) * 0.35f;
            breachFlash.color = c;
        }
    }

    void OnWaveChanged(int current, int total) => RefreshStatus();

    void OnPenalty(int penaltyMs)
    {
        _breachMessageTimer = breachMessageSeconds;
        _flashTimer = 0.35f;
        _audio?.PlayBreach();

        if (breachText != null)
        {
            breachText.gameObject.SetActive(true);
            breachText.text = GameLocalization.Text(
                $"Breach! Hourglass -{penaltyMs / 1000f:0.0}s",
                $"漏怪！沙漏 -{penaltyMs / 1000f:0.0} 秒");
            breachText.color = new Color(1f, 0.45f, 0.4f, 1f);
        }

        if (statusText != null)
        {
            statusText.color = new Color(1f, 0.55f, 0.45f, 1f);
        }
    }

    void OnVictory() => resultOverlay?.Show("VICTORY", new Color(0.45f, 0.95f, 0.55f, 1f));

    void OnDefeat() => resultOverlay?.Show("DEFEAT", new Color(0.95f, 0.35f, 0.35f, 1f));

    void RefreshStatus()
    {
        if (statusText == null)
        {
            return;
        }

        int wave = _waves != null ? _waves.CurrentWaveDisplay : 0;
        int totalWaves = _waves != null ? _waves.TotalWaves : 0;
        int enemies = _waves != null ? _waves.ActiveEnemyCount : 0;
        int damage = _damageTracker != null ? _damageTracker.TotalDamage : 0;

        if (_session != null && _session.IsPreparing)
        {
            statusText.color = new Color(0.45f, 0.9f, 0.65f, 1f);
            int sec = _waves != null ? Mathf.CeilToInt(_waves.PrepRemaining) : 0;
            statusText.text = GameLocalization.Text(
                $"Wave {wave} prep {sec}s | Damage {damage}",
                $"第 {wave} 波准备 {sec}s | 伤害 {damage}");
            return;
        }

        if (_waves != null && _waves.IsCountdownPhase)
        {
            statusText.color = new Color(1f, 0.75f, 0.3f, 1f);
            statusText.text = GameLocalization.Text(
                $"Battle incoming... | Wave {wave}/{totalWaves}",
                $"即将开战… | 波次 {wave}/{totalWaves}");
            return;
        }

        statusText.color = Color.white;
        statusText.text = GameLocalization.Text(
            $"Wave {wave}/{totalWaves} | Enemies {enemies} | Damage {damage}",
            $"波次 {wave}/{totalWaves} | 敌人 {enemies} | 伤害 {damage}");
    }
}
