using System;
using UnityEngine;

/// <summary>
/// 对局状态：准备/进行中/胜利/失败。仅胜负时冻结操作。
/// </summary>
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    GameSessionState _state = GameSessionState.Preparing;

    public GameSessionState State => _state;

    /// <summary>可买、可拆、可放、可发球、可刷怪。</summary>
    public bool IsPlaying =>
        _state == GameSessionState.Preparing || _state == GameSessionState.Playing;

    public event Action OnVictory;
    public event Action OnDefeat;

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

    public void BeginPlaying()
    {
        if (_state == GameSessionState.Preparing)
        {
            _state = GameSessionState.Playing;
        }
    }

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
