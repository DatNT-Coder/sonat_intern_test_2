using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Gameplay HUD")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI movesLabel;
    [SerializeField] private TextMeshProUGUI coinsLabel;
    [SerializeField] private TextMeshProUGUI blockCounterLabel;

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winCoinsEarned;
    [SerializeField] private TextMeshProUGUI winLevelLabel;
    [SerializeField] private Button nextLevelBtn;

    [Header("Lose Panel")]
    [SerializeField] private TextMeshProUGUI loseMovesLabel;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button addMovesBtn;   // +10 moves (30 xu)
    [SerializeField] private TextMeshProUGUI addMovesCostLabel;

    [Header("Shop/Item")]
    [SerializeField] private int addMovesCost = 30;
    [SerializeField] private int addMovesAmount = 10;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;

        // Setup buttons
        nextLevelBtn?.onClick.AddListener(() => GameManager.Instance.NextLevel());
        retryBtn?.onClick.AddListener(() => GameManager.Instance.RetryLevel());
        addMovesBtn?.onClick.AddListener(OnBuyMoves);

        if (addMovesCostLabel != null)
            addMovesCostLabel.text = $"{addMovesCost} 🪙";

        HideAll();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
    }

    // ─── State handler ────────────────────────────────────────────────────────

    private void HandleStateChanged(GameState state)
    {
        HideAll();
        switch (state)
        {
            case GameState.MainMenu:
                ShowPanel(mainMenuPanel);
                break;
            case GameState.Playing:
                ShowPanel(gameplayPanel);
                UpdateCoins(GameManager.Instance.Coins);
                break;
            case GameState.Win:
                StartCoroutine(ShowWinDelayed());
                break;
            case GameState.Lose:
                StartCoroutine(ShowLoseDelayed());
                break;
            case GameState.Paused:
                ShowPanel(pausePanel);
                break;
        }
    }

    // ─── Win ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowWinDelayed()
    {
        yield return new WaitForSeconds(0.6f);
        ShowPanel(winPanel);

        if (winLevelLabel != null)
            winLevelLabel.text = $"Level {GameManager.Instance.CurrentLevel} Complete!";

        if (winCoinsEarned != null)
        {
            int earned = 10 + GameManager.Instance.MovesLeft;
            winCoinsEarned.text = $"+{earned} 🪙";
            winCoinsEarned.transform.localScale = Vector3.zero;
            winCoinsEarned.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }

        // Animat win panel
        if (winPanel != null)
        {
            winPanel.transform.localScale = Vector3.zero;
            winPanel.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }
    }

    // ─── Lose ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowLoseDelayed()
    {
        yield return new WaitForSeconds(0.4f);
        ShowPanel(losePanel);

        if (loseMovesLabel != null)
            loseMovesLabel.text = "Out of moves!";

        // Update nút mua moves: disable nếu không đủ xu
        if (addMovesBtn != null)
        {
            bool canAfford = GameManager.Instance.Coins >= addMovesCost;
            addMovesBtn.interactable = canAfford;
            var colors = addMovesBtn.colors;
            colors.normalColor = canAfford ? Color.white : Color.gray;
            addMovesBtn.colors = colors;
        }

        if (losePanel != null)
        {
            losePanel.transform.localScale = Vector3.zero;
            losePanel.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }
    }

    // ─── Mua thêm moves ───────────────────────────────────────────────────────

    private void OnBuyMoves()
    {
        if (GameManager.Instance.SpendCoins(addMovesCost))
        {
            GameManager.Instance.AddMoves(addMovesAmount);
            GameManager.Instance.SetState(GameState.Playing);
        }
        else
        {
            // Shake nút để báo không đủ xu
            addMovesBtn?.transform.DOShakePosition(0.3f, 8f, 20);
        }
    }

    // ─── HUD updates ─────────────────────────────────────────────────────────

    public void UpdateMoves(int moves)
    {
        if (movesLabel == null) return;
        movesLabel.text = moves.ToString();

        // Đỏ khi còn ít moves
        movesLabel.color = moves <= 5 ? Color.red : Color.white;
        if (moves <= 5)
            movesLabel.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 5, 0.3f);
    }

    public void UpdateCoins(int coins)
    {
        if (coinsLabel != null)
            coinsLabel.text = coins.ToString();
    }

    public void UpdateBlockCounter(int remaining, int total)
    {
        if (blockCounterLabel != null)
            blockCounterLabel.text = $"{remaining}/{total}";
    }

    public void SetLevelLabel(int level)
    {
        if (levelLabel != null)
            levelLabel.text = $"Level {level}";
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void HideAll()
    {
        mainMenuPanel?.SetActive(false);
        gameplayPanel?.SetActive(false);
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        pausePanel?.SetActive(false);
    }

    private void ShowPanel(GameObject panel)
    {
        panel?.SetActive(true);
    }
}