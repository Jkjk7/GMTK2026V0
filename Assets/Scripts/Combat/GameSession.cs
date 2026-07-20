using System;
using UnityEngine;

/// <summary>
/// 对局状态：准备 / 战斗 / 胜负。
/// IsPreparing / IsCombatActive / IsRunActive 分流各系统。
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    GameSessionState _state = GameSessionState.Preparing;

    public GameSessionState State => _state;

    public bool IsPreparing => _state == GameSessionState.Preparing;

    public bool IsCombatActive => _state == GameSessionState.Playing;

    /// <summary>整局仍在进行（准备或战斗），可用于商店/放置。</summary>
    public bool IsRunActive =>
        _state == GameSessionState.Preparing || _state == GameSessionState.Playing;

    /// <summary>兼容旧调用：等同 IsRunActive。</summary>
    public bool IsPlaying => IsRunActive;

    public event Action OnVictory;
    public event Action OnDefeat;
    public event Action OnEnteredPreparing;
    public event Action OnEnteredCombat;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void EnterPreparing()
    {
        if (_state == GameSessionState.Victory || _state == GameSessionState.Defeat)
        {
            return;
        }

        _state = GameSessionState.Preparing;
        OnEnteredPreparing?.Invoke();
    }

    public void EnterCombat()
    {
        if (_state == GameSessionState.Victory || _state == GameSessionState.Defeat)
        {
            return;
        }

        _state = GameSessionState.Playing;
        OnEnteredCombat?.Invoke();
    }

    /// <summary>兼容旧调用：进入战斗。</summary>
    public void BeginPlaying() => EnterCombat();

    public void SetVictory()
    {
        if (_state == GameSessionState.Victory || _state == GameSessionState.Defeat)
        {
            return;
        }

        _state = GameSessionState.Victory;
        OnVictory?.Invoke();
        Debug.Log("[GameSession] VICTORY");
    }

    public void SetDefeat()
    {
        if (_state == GameSessionState.Victory || _state == GameSessionState.Defeat)
        {
            return;
        }

        _state = GameSessionState.Defeat;
        OnDefeat?.Invoke();
        Debug.Log("[GameSession] DEFEAT");
    }
}

public enum GameSessionState
{
    Preparing,
    Playing,
    Victory,
    Defeat
}
