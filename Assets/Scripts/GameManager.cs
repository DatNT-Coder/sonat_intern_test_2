using UnityEngine;
using System;

public enum GameState { MainMenu, Playing, Win, Lose, Paused }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool skipMainMenu = true;

    [Header("Economy")]
    [SerializeField] private int coinsPerWin = 10;
    [SerializeField] private int bonusCoinsPerRemainingMove = 1;

    public event Action<GameState> OnGameStateChanged;

    public GameState CurrentState { get; private set; }
    public int CurrentLevel { get; private set; }
    public int Coins { get; private set; }
    public int MovesLeft { get; private set; }
    public int TotalMoves { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSave();
    }

    void Start()
    {
        if (skipMainMenu) SetState(GameState.Playing);
        else SetState(GameState.MainMenu);
    }

    // ─── State ───────────────────────────────────────────────────────────────

    public void SetState(GameState newState)
    {
        CurrentState = newState;
        Debug.Log($"[GameManager] State -> {newState}");
        OnGameStateChanged?.Invoke(newState);
    }

    // ─── Moves ───────────────────────────────────────────────────────────────

    public void InitMoves(int moves)
    {
        TotalMoves = moves;
        MovesLeft = moves;
        UIManager.Instance?.UpdateMoves(MovesLeft);
    }

    /// <summary>Được gọi mỗi khi player tap block (kể cả bị chặn)</summary>
    public void UseMove()
    {
        if (CurrentState != GameState.Playing) return;
        MovesLeft--;
        UIManager.Instance?.UpdateMoves(MovesLeft);

        if (MovesLeft <= 0)
        {
            // Kiểm tra còn block không — nếu LevelManager chưa win thì lose
            StartCoroutine(CheckLoseDelayed());
        }
    }

    private System.Collections.IEnumerator CheckLoseDelayed()
    {
        yield return new WaitForSeconds(0.4f);
        if (CurrentState == GameState.Playing)
            SetState(GameState.Lose);
    }

    public void AddMoves(int amount)
    {
        MovesLeft += amount;
        UIManager.Instance?.UpdateMoves(MovesLeft);
    }

    // ─── Win / Lose ──────────────────────────────────────────────────────────

    public void WinLevel()
    {
        int bonus = coinsPerWin + MovesLeft * bonusCoinsPerRemainingMove;
        AddCoins(bonus);
        CurrentLevel++;
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        PlayerPrefs.Save();
        SetState(GameState.Win);
    }

    public void RetryLevel()
    {
        SetState(GameState.Playing);
    }

    public void NextLevel()
    {
        SetState(GameState.Playing);
    }

    public void GoToMainMenu()
    {
        SetState(GameState.MainMenu);
    }

    // ─── Coins ───────────────────────────────────────────────────────────────

    public void AddCoins(int amount)
    {
        Coins += amount;
        PlayerPrefs.SetInt("Coins", Coins);
        PlayerPrefs.Save();
        UIManager.Instance?.UpdateCoins(Coins);
    }

    public bool SpendCoins(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        PlayerPrefs.SetInt("Coins", Coins);
        PlayerPrefs.Save();
        UIManager.Instance?.UpdateCoins(Coins);
        return true;
    }

    // ─── Save ─────────────────────────────────────────────────────────────────

    private void LoadSave()
    {
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 0);
        Coins = PlayerPrefs.GetInt("Coins", 0);
    }
}