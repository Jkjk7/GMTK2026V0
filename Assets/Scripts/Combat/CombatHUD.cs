using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：波次/伤害文字、机会图标、漏怪警告、胜负 CanvasGroup 遮罩。
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [SerializeField] Text statusText;
    [SerializeField] Text livesText;
    [SerializeField] Text breachText;
    [SerializeField] Image breachFlash;
    [SerializeField] ResultOverlayView resultOverlay;
    [SerializeField] float breachMessageSeconds = 2.5f;

    Mage _mage;
    WaveManager _waves;
    GameSession _session;
    DamageTracker _damageTracker;
    UIAudioFeedback _audio;

    float _breachMessageTimer;
    float _flashTimer;

    public void Initialize(
        Text statusLabel,
        Text livesLabel,
        Text breachLabel,
        Image flashImage,
        ResultOverlayView overlay,
        Mage mage,
        WaveManager waves,
        GameSession session,
        DamageTracker damageTracker,
        UIAudioFeedback audio = null)
    {
        statusText = statusLabel;
        livesText = livesLabel;
        breachText = breachLabel;
        breachFlash = flashImage;
        resultOverlay = overlay;
        _mage = mage;
        _waves = waves;
        _session = session;
        _damageTracker = damageTracker;
        _audio = audio;

        if (_mage != null)
        {
            _mage.OnLivesChanged += OnLivesChanged;
            _mage.OnBreach += OnBreach;
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
        RefreshLives();
    }

    void OnDestroy()
    {
        if (_mage != null)
        {
            _mage.OnLivesChanged -= OnLivesChanged;
            _mage.OnBreach -= OnBreach;
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

    void OnLivesChanged(int lives)
    {
        RefreshLives();
        RefreshStatus();
    }

    void OnWaveChanged(int current, int total) => RefreshStatus();

    void OnBreach()
    {
        _breachMessageTimer = breachMessageSeconds;
        _flashTimer = 0.35f;
        _audio?.PlayBreach();

        if (breachText != null)
        {
            breachText.gameObject.SetActive(true);
            breachText.text = "防线突破！清屏 -1 机会";
            breachText.color = new Color(1f, 0.45f, 0.4f, 1f);
        }

        if (statusText != null)
        {
            statusText.color = new Color(1f, 0.55f, 0.45f, 1f);
        }
    }

    void OnVictory() => resultOverlay?.Show("VICTORY", new Color(0.45f, 0.95f, 0.55f, 1f));

    void OnDefeat() => resultOverlay?.Show("DEFEAT", new Color(0.95f, 0.35f, 0.35f, 1f));

    void RefreshLives()
    {
        if (livesText == null)
        {
            return;
        }

        int lives = _mage != null ? _mage.LivesRemaining : 0;
        // ◆ 活着 / ◇ 已失
        char[] marks = new char[Mage.MaxLives];
        for (int i = 0; i < Mage.MaxLives; i++)
        {
            marks[i] = i < lives ? '◆' : '◇';
        }

        livesText.text = "机会 " + new string(marks);
        livesText.color = lives <= 1
            ? new Color(1f, 0.45f, 0.4f, 1f)
            : new Color(0.7f, 0.85f, 1f, 1f);
    }

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

        if (_session != null && _session.State == GameSessionState.Preparing)
        {
            statusText.color = new Color(0.85f, 0.9f, 1f, 1f);
            statusText.text = $"准备中… | 敌人 {enemies} | 伤害 {damage}";
            return;
        }

        statusText.color = Color.white;
        statusText.text = $"波次 {wave}/{totalWaves} | 敌人 {enemies} | 伤害 {damage}";
    }
}
