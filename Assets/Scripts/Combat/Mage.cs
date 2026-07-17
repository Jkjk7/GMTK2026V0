using System;
using UnityEngine;

/// <summary>
/// 魔法师：3 次机会；漏怪时清屏并扣命，机会归零则失败。
/// </summary>
public class Mage : MonoBehaviour
{
    public const int MaxLives = 3;

    const float BreachDebounceSeconds = 0.5f;

    [SerializeField] int livesRemaining = MaxLives;

    WaveManager _waveManager;
    GameSession _session;
    SpriteRenderer _visual;
    float _breachDebounce;

    public int LivesRemaining => livesRemaining;

    public event Action<int> OnLivesChanged;
    public event Action OnBreach;

    /// <summary>
    /// 放置到路线最左端并注入依赖。
    /// </summary>
    public void Initialize(Vector3 worldPosition, WaveManager waveManager, GameSession session)
    {
        transform.position = worldPosition;
        _waveManager = waveManager;
        _session = session;
        livesRemaining = MaxLives;
        EnsureVisual();
        OnLivesChanged?.Invoke(livesRemaining);
    }

    void Update()
    {
        if (_breachDebounce > 0f)
        {
            _breachDebounce -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 敌人到达终点：清屏、扣一命；第三次漏怪触发失败。
    /// </summary>
    public void OnEnemyBreach()
    {
        if (_breachDebounce > 0f || livesRemaining <= 0)
        {
            return;
        }

        _breachDebounce = BreachDebounceSeconds;
        _waveManager?.ClearAllEnemies();

        livesRemaining = Mathf.Max(0, livesRemaining - 1);
        OnLivesChanged?.Invoke(livesRemaining);
        OnBreach?.Invoke();

        if (livesRemaining <= 0)
        {
            _session?.SetDefeat();
        }
    }

    void EnsureVisual()
    {
        if (_visual == null)
        {
            _visual = GetComponent<SpriteRenderer>();
            if (_visual == null)
            {
                _visual = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        _visual.sprite = PrototypeSprites.Square;
        _visual.color = new Color(0.35f, 0.55f, 0.95f, 1f);
        _visual.sortingOrder = 9;
        transform.localScale = new Vector3(0.85f, 0.85f, 1f);
    }
}
