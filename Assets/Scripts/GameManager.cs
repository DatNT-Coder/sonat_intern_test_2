using UnityEngine;
using System;

/// <summary>
/// Central game manager. Singleton pattern.
/// Controls overall game state machine.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    [Header("Settings")]
    [SerializeField] private int totalLevels = 10;

    private GameState _currentState;
    private int _currentLevel;

    public GameState CurrentState => _currentState;
    public int CurrentLevel => _currentLevel;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Debug")]
    [SerializeField] private bool skipMainMenu = true; // tắt sau khi có UI

    void Start()
    {
        _currentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        ChangeState(skipMainMenu ? GameState.Playing : GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        _currentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] State -> {newState}");
    }

    public void StartGame()
    {
        ChangeState(GameState.Playing);
    }

    public void WinLevel()
    {
        _currentLevel = Mathf.Min(_currentLevel + 1, totalLevels - 1);
        PlayerPrefs.SetInt("CurrentLevel", _currentLevel);
        ChangeState(GameState.Win);
    }

    public void LoseLevel()
    {
        ChangeState(GameState.Lose);
    }

    public void RestartLevel()
    {
        ChangeState(GameState.Playing);
    }

    public void NextLevel()
    {
        ChangeState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        ChangeState(GameState.MainMenu);
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Win,
    Lose,
    Paused
}