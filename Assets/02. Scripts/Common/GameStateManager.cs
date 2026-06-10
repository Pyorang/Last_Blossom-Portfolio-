using System;
using UnityEngine;

public enum GameState
{
    Ready,
    Playing,
    Paused,
    GameOver
}

public class GameStateManager : SingletonBehaviour<GameStateManager>
{
    private GameState _currentState = GameState.Ready;

    public GameState CurrentState => _currentState;

    public event Action<GameState> OnGameStateChanged;
    public event Action OnGameStart;
    public event Action OnGamePause;
    public event Action OnGameResume;
    public event Action<bool> OnGameEnd;

    protected override void Init()
    {
        IsDestroyOnLoad = true;
        SetCursorVisible(false);
        base.Init();
    }

    private void Start()
    {
        AudioManager.Instance?.Play(AudioType.BGM, "BGM");
    }

    #region State Management

    public void ChangeState(GameState newState)
    {
        if (_currentState == newState) return;

        GameState previousState = _currentState;
        _currentState = newState;

        HandleStateTransition(previousState, newState);
        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleStateTransition(GameState from, GameState to)
    {
        switch (to)
        {
            case GameState.Ready:
                SetCursorVisible(false);
                break;

            case GameState.Playing:
                TimeScaleManager.Instance?.Resume();
                SetCursorVisible(false);
                if (from == GameState.Ready)
                {
                    OnGameStart?.Invoke();
                }
                else if (from == GameState.Paused)
                {
                    OnGameResume?.Invoke();
                }
                break;

            case GameState.Paused:
                TimeScaleManager.Instance?.Pause();
                SetCursorVisible(true);
                OnGamePause?.Invoke();
                break;

            case GameState.GameOver:
                SetCursorVisible(true);
                break;
        }
    }

    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    #endregion

    #region Public Methods

    public void StartGame()
    {
        if (_currentState == GameState.Ready)
        {
            ChangeState(GameState.Playing);
        }
    }

    public void PauseGame()
    {
        if (_currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (_currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }

    public void EndGame(bool isVictory)
    {
        if (_currentState == GameState.GameOver) return;

        ChangeState(GameState.GameOver);

        if (!isVictory && WaveManager.Instance != null)
        {
            WaveManager.Instance.FailWave();
        }

        OnGameEnd?.Invoke(isVictory);

        Debug.Log($"[GameStateManager] 게임 종료 - {(isVictory ? "승리!" : "패배...")}");
    }

    #endregion

    #region Properties

    public bool IsPlaying => _currentState == GameState.Playing;
    public bool IsPaused => _currentState == GameState.Paused;
    public bool IsGameOver => _currentState == GameState.GameOver;
    public bool IsReady => _currentState == GameState.Ready;

    #endregion
}
