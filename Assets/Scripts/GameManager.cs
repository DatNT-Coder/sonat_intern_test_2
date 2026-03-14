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
    public int CurrentLevel { get; private set; }   // level đang chơi
    public int HighestLevel { get; private set; }   // level cao nhất đã unlock
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

    void Update()
    {
        // Nhấn R để retry màn hiện tại
        if (Input.GetKeyDown(KeyCode.R))
            RetryLevel();
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

    /// <summary>Gọi khi block trượt ra khỏi màn thành công (không phải khi bị chặn)</summary>
    public void UseMove()
    {
        if (CurrentState != GameState.Playing) return;
        MovesLeft--;
        UIManager.Instance?.UpdateMoves(MovesLeft);

        if (MovesLeft <= 0)
            StartCoroutine(CheckLoseDelayed());
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
        HighestLevel = Mathf.Max(HighestLevel, CurrentLevel + 1);
        CurrentLevel++;
        PlayerPrefs.SetInt("HighestLevel", HighestLevel);
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        PlayerPrefs.Save();
        SetState(GameState.Win);
    }

    public void RetryLevel()
    {
        // Reload đúng level đang chơi — KHÔNG tăng CurrentLevel
        LevelManager.Instance?.LoadLevel(CurrentLevel);
        InitMoves(GameManager.Instance.TotalMoves);
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

    [ContextMenu("Reset Save Data")]
    public void ResetSave()
    {
        PlayerPrefs.DeleteAll();
        CurrentLevel = 0;
        HighestLevel = 0;
        Coins = 0;
        UIManager.Instance?.UpdateCoins(0);
        UIManager.Instance?.UpdateMoves(0);
        Debug.Log("[GameManager] Save data reset!");
    }

    private void LoadSave()
    {
        HighestLevel = PlayerPrefs.GetInt("HighestLevel", 0);
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 0); // Tiếp tục từ level đã chơi
        Coins = PlayerPrefs.GetInt("Coins", 0);
    }
}