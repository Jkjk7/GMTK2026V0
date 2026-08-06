using UnityEngine;
using UnityEngine.UI;

/// <summary>开发者模式 HUD：跳过当前波、加金币。</summary>
public class DeveloperToolsHud : MonoBehaviour
{
    WaveManager _waves;
    Button _skipButton;
    Text _skipLabel;
    Button _goldButton;

    public void Initialize(
        WaveManager waves,
        Button skipButton,
        Text skipLabel,
        Button goldButton = null)
    {
        _waves = waves;
        _skipButton = skipButton;
        _skipLabel = skipLabel;
        _goldButton = goldButton;

        bool enabled = GameSettings.DeveloperMode;
        gameObject.SetActive(enabled);
        if (!enabled)
        {
            return;
        }

        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(OnSkipClicked);
            _skipButton.onClick.AddListener(OnSkipClicked);
        }

        if (_goldButton != null)
        {
            _goldButton.onClick.RemoveListener(OnGoldClicked);
            _goldButton.onClick.AddListener(OnGoldClicked);
        }

        RefreshLabel();
    }

    void Update()
    {
        if (!GameSettings.DeveloperMode || _skipButton == null || _waves == null)
        {
            return;
        }

        bool canSkip = _waves.CanDevSkipCurrentWave;
        _skipButton.interactable = canSkip;
        if (_skipLabel != null)
        {
            _skipLabel.color = canSkip
                ? new Color(0.95f, 0.85f, 0.4f, 1f)
                : new Color(0.45f, 0.4f, 0.35f, 1f);
        }
    }

    void OnSkipClicked()
    {
        _waves?.TryDevSkipCurrentWave();
    }

    void OnGoldClicked()
    {
        Economy.Instance?.AddGold(100);
    }

    void RefreshLabel()
    {
        if (_skipLabel != null)
        {
            _skipLabel.text = GameLocalization.Text("Skip Wave", "跳过本波");
        }
    }
}
