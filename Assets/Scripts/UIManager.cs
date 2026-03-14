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
    [SerializeField] private GameObject settingsPanel; // popup nhỏ

    [Header("Top Bar HUD")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI movesLabel;
    [SerializeField] private TextMeshProUGUI coinsLabel;

    [Header("Bottom Bar Buttons (luon hien trong game)")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button boomButton;
    [SerializeField] private Button moreMovesButton;

    [Header("Item Cost")]
    [SerializeField] private int boomCost = 20;
    [SerializeField] private int moreMoveCost = 30;
    [SerializeField] private int moreMoveAmt = 10;

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winCoinsEarned;
    [SerializeField] private TextMeshProUGUI winLevelLabel;
    [SerializeField] private Button nextLevelBtn;

    [Header("Lose Panel")]
    [SerializeField] private TextMeshProUGUI loseTitleLabel;
    [SerializeField] private Button loseRetryBtn;
    [SerializeField] private Button loseMoreMovesBtn;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += HandleStateChanged;

        // Bottom bar buttons
        retryButton?.onClick.AddListener(OnRetry);
        settingsButton?.onClick.AddListener(OnSettings);
        boomButton?.onClick.AddListener(OnBoom);
        moreMovesButton?.onClick.AddListener(OnMoreMoves);

        // Win panel
        nextLevelBtn?.onClick.AddListener(() => GameManager.Instance.NextLevel());

        // Lose panel
        loseRetryBtn?.onClick.AddListener(OnRetry);
        loseMoreMovesBtn?.onClick.AddListener(OnLoseMoreMoves);

        HideAll();
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
    }

    // ─── State ───────────────────────────────────────────────────────────────

    private void HandleStateChanged(GameState state)
    {
        // Ẩn win/lose panel khi chuyển state
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        mainMenuPanel?.SetActive(false);

        switch (state)
        {
            case GameState.MainMenu:
                mainMenuPanel?.SetActive(true);
                gameplayPanel?.SetActive(false);
                break;
            case GameState.Playing:
                gameplayPanel?.SetActive(true);
                UpdateCoins(GameManager.Instance.Coins);
                UpdateMoves(GameManager.Instance.MovesLeft);
                break;
            case GameState.Win:
                StartCoroutine(ShowWinDelayed());
                break;
            case GameState.Lose:
                StartCoroutine(ShowLoseDelayed());
                break;
        }
    }

    // ─── Win ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowWinDelayed()
    {
        yield return new WaitForSeconds(0.6f);
        winPanel?.SetActive(true);

        int earned = 10 + GameManager.Instance.MovesLeft;
        if (winLevelLabel != null)
            winLevelLabel.text = $"Level {GameManager.Instance.CurrentLevel} Complete!";
        if (winCoinsEarned != null)
        {
            winCoinsEarned.text = $"+{earned} 🪙";
            winCoinsEarned.transform.localScale = Vector3.zero;
            winCoinsEarned.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        }
        winPanel?.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).From(Vector3.zero);
    }

    // ─── Lose ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowLoseDelayed()
    {
        yield return new WaitForSeconds(0.4f);
        losePanel?.SetActive(true);
        if (loseTitleLabel != null) loseTitleLabel.text = "Out of moves!";

        // Disable lose-more-moves nếu không đủ xu
        if (loseMoreMovesBtn != null)
            loseMoreMovesBtn.interactable = GameManager.Instance.Coins >= moreMoveCost;

        losePanel?.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).From(Vector3.zero);
    }

    // ─── Bottom bar actions ──────────────────────────────────────────────────

    private void OnRetry()
    {
        losePanel?.SetActive(false);
        GameManager.Instance.RetryLevel();
    }

    private void OnSettings()
    {
        bool isOpen = settingsPanel != null && settingsPanel.activeSelf;
        settingsPanel?.SetActive(!isOpen);
    }

    private void OnBoom()
    {
        if (!GameManager.Instance.SpendCoins(boomCost))
        {
            boomButton?.transform.DOShakePosition(0.3f, 8f, 20);
            return;
        }
        // Xóa 1 block random còn lại
        LevelManager.Instance?.RemoveRandomBlock();
        UpdateCoins(GameManager.Instance.Coins);
    }

    private void OnMoreMoves()
    {
        if (!GameManager.Instance.SpendCoins(moreMoveCost))
        {
            moreMovesButton?.transform.DOShakePosition(0.3f, 8f, 20);
            return;
        }
        GameManager.Instance.AddMoves(moreMoveAmt);
        UpdateCoins(GameManager.Instance.Coins);
    }

    private void OnLoseMoreMoves()
    {
        if (!GameManager.Instance.SpendCoins(moreMoveCost)) return;
        GameManager.Instance.AddMoves(moreMoveAmt);
        losePanel?.SetActive(false);
        GameManager.Instance.SetState(GameState.Playing);
    }

    // ─── HUD updates ─────────────────────────────────────────────────────────

    public void UpdateMoves(int moves)
    {
        if (movesLabel == null) return;
        movesLabel.text = moves.ToString();
        movesLabel.color = moves <= 5 ? Color.red : Color.white;
        if (moves <= 5)
            movesLabel.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 5, 0.3f);
    }

    public void UpdateCoins(int coins)
    {
        if (coinsLabel != null)
            coinsLabel.text = coins.ToString();
    }

    public void SetLevelLabel(int level)
    {
        if (levelLabel != null)
            levelLabel.text = $"Level {level}";
    }

    // Không dùng nữa nhưng giữ để tránh lỗi compile
    public void UpdateBlockCounter(int remaining, int total) { }

    private void HideAll()
    {
        mainMenuPanel?.SetActive(false);
        gameplayPanel?.SetActive(true); // gameplay luôn hiện
        winPanel?.SetActive(false);
        losePanel?.SetActive(false);
        settingsPanel?.SetActive(false);
    }
}