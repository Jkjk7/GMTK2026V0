using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 战斗 HUD：波次、机会、伤害、漏怪提示、胜负 overlay。
/// </summary>
public class CombatHUD : MonoBehaviour
{
    [SerializeField] Text statusText;
    [SerializeField] Text overlayText;
    [SerializeField] float breachMessageSeconds = 2.5f;

    Mage _mage;
    WaveManager _waves;
    GameSession _session;
    DamageTracker _damageTracker;

    float _breachMessageTimer;

    public void Initialize(
        Text statusLabel,
        Text overlayLabel,
        Mage mage,
        WaveManager waves,
        GameSession session,
        DamageTracker damageTracker)
    {
        statusText = statusLabel;
        overlayText = overlayLabel;
        _mage = mage;
        _waves = waves;
        _session = session;
        _damageTracker = damageTracker;

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

        if (overlayText != null)
        {
            overlayText.gameObject.SetActive(false);
        }

        RefreshStatus();
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
            if (_breachMessageTimer <= 0f)
            {
                RefreshStatus();
            }
        }
        else if (_session != null && _session.State == GameSessionState.Preparing)
        {
            RefreshStatus();
        }
    }

    void OnLivesChanged(int lives) => RefreshStatus();

    void OnWaveChanged(int current, int total) => RefreshStatus();

    void OnBreach()
    {
        _breachMessageTimer = breachMessageSeconds;
        if (statusText != null)
        {
            statusText.text = "漏怪！清屏 -1 机会";
            statusText.color = new Color(1f, 0.55f, 0.45f, 1f);
        }
    }

    void OnVictory() => ShowOverlay("VICTORY", new Color(0.45f, 0.95f, 0.55f, 1f));

    void OnDefeat() => ShowOverlay("DEFEAT", new Color(0.95f, 0.35f, 0.35f, 1f));

    void RefreshStatus()
    {
        if (statusText == null)
        {
            return;
        }

        int lives = _mage != null ? _mage.LivesRemaining : 0;
        int wave = _waves != null ? _waves.CurrentWaveDisplay : 0;
        int totalWaves = _waves != null ? _waves.TotalWaves : 0;
        int enemies = _waves != null ? _waves.ActiveEnemyCount : 0;
        int damage = _damageTracker != null ? _damageTracker.TotalDamage : 0;

        if (_session != null && _session.State == GameSessionState.Preparing)
        {
            statusText.color = new Color(0.85f, 0.9f, 1f, 1f);
            statusText.text = $"准备中… | 机会: {lives} | 伤害: {damage}";
            return;
        }

        statusText.color = Color.white;
        statusText.text =
            $"波次 {wave}/{totalWaves} | 敌人 {enemies} | 机会: {lives} | 伤害: {damage}";
    }

    void ShowOverlay(string message, Color color)
    {
        if (overlayText == null)
        {
            return;
        }

        overlayText.gameObject.SetActive(true);
        overlayText.text = message;
        overlayText.color = color;
    }
}
