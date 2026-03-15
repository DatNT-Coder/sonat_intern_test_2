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

    [Header("Win Panel")]
    [SerializeField] private WinPanelController winPanelController;

    [Header("Lose Panel")]
    [SerializeField] private LosePanelController losePanelController;

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

        // Lose panel

        HideAll();
        // Dùng Invoke để đợi GameManager load save xong
        Invoke(nameof(RefreshHUD), 0.1f);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
    }

    private void RefreshHUD()
    {
        if (GameManager.Instance == null) return;
        UpdateCoins(GameManager.Instance.Coins);
        UpdateMoves(GameManager.Instance.MovesLeft);
        SetLevelLabel(GameManager.Instance.CurrentLevel + 1);
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
                SetLevelLabel(GameManager.Instance.CurrentLevel + 1);
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
        yield return new WaitForSeconds(0.5f);
        int earned = 10 + GameManager.Instance.MovesLeft;
        int level = GameManager.Instance.CurrentLevel;
        Debug.Log($"[UIManager] ShowWin: controller={winPanelController != null}, earned={earned}");
        if (winPanelController != null)
            winPanelController.Show(earned, GameManager.Instance.Coins, level);
        else
        {
            // Fallback: hiện winPanel trực tiếp
            winPanel?.SetActive(true);
        }
    }

    // ─── Lose ─────────────────────────────────────────────────────────────────

    private IEnumerator ShowLoseDelayed()
    {
        yield return new WaitForSeconds(0.4f);
        losePanelController?.Show();
    }

    // ─── Bottom bar actions ──────────────────────────────────────────────────

    private void OnRetry()
    {
        losePanel?.SetActive(false);
        GameManager.Instance.RetryLevel();
    }

    [Header("Settings Panel")]
    [SerializeField] private SettingsPanelController settingsPanelController;

    private void OnSettings()
    {
        if (settingsPanelController == null) return;
        if (settingsPanelController.gameObject.activeSelf)
            settingsPanelController.Hide();
        else
            settingsPanelController.Show();
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
        if (!GameManager.Instance.SpendCoins(30))
        {
            moreMovesButton?.transform.DOShakePosition(0.3f, 8f, 20);
            return;
        }
        GameManager.Instance.AddMoves(10);
        UpdateCoins(GameManager.Instance.Coins);
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