using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Controls all UI panels. Reacts to GameState changes.
/// Single source of truth for UI visibility.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private CanvasGroup mainMenuPanel;
    [SerializeField] private CanvasGroup gameplayPanel;
    [SerializeField] private CanvasGroup winPanel;
    [SerializeField] private CanvasGroup losePanel;
    [SerializeField] private CanvasGroup pausePanel;

    [Header("Gameplay HUD")]
    [SerializeField] private TextMeshProUGUI levelLabel;
    [SerializeField] private TextMeshProUGUI blockCounterLabel;
    [SerializeField] private Slider progressBar;

    [Header("Win Panel")]
    [SerializeField] private TextMeshProUGUI winLevelLabel;
    [SerializeField] private Transform starsContainer;
    [SerializeField] private GameObject starPrefab;

    [Header("Lose Panel")]
    [SerializeField] private TextMeshProUGUI loseLevelLabel;

    [Header("Transition")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [SerializeField] private float transitionDuration = 0.3f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnEnable()
    {
        GameManager.Instance.OnGameStateChanged += HandleStateChanged;
    }

    void OnDisable()
    {
        if (GameManager.Instance)
            GameManager.Instance.OnGameStateChanged -= HandleStateChanged;
    }

    // ─── State → UI mapping ───────────────────────────────────────────────

    private void HandleStateChanged(GameState state)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionToState(state));
    }

    private IEnumerator TransitionToState(GameState state)
    {
        // Fade out
        if (transitionOverlay)
        {
            transitionOverlay.gameObject.SetActive(true);
            yield return transitionOverlay.DOFade(1f, transitionDuration).WaitForCompletion();
        }

        SetAllPanelsInactive();

        switch (state)
        {
            case GameState.MainMenu:
                ShowPanel(mainMenuPanel);
                break;

            case GameState.Playing:
                ShowPanel(gameplayPanel);
                UpdateLevelLabel();
                break;

            case GameState.Win:
                ShowPanel(winPanel);
                PlayWinAnimation();
                AudioManager.Instance?.PlayWin();
                break;

            case GameState.Lose:
                ShowPanel(losePanel);
                AudioManager.Instance?.PlayLose();
                break;

            case GameState.Paused:
                ShowPanel(pausePanel);
                break;
        }

        // Fade in
        if (transitionOverlay)
        {
            yield return transitionOverlay.DOFade(0f, transitionDuration).WaitForCompletion();
            transitionOverlay.gameObject.SetActive(false);
        }
    }

    // ─── Panel helpers ────────────────────────────────────────────────────

    private void SetAllPanelsInactive()
    {
        SetPanel(mainMenuPanel, false);
        SetPanel(gameplayPanel, false);
        SetPanel(winPanel,      false);
        SetPanel(losePanel,     false);
        SetPanel(pausePanel,    false);
    }

    private void ShowPanel(CanvasGroup panel)
    {
        if (panel == null) return;
        SetPanel(panel, true);
        panel.DOFade(1f, 0.2f).From(0f);
    }

    private void SetPanel(CanvasGroup panel, bool active)
    {
        if (panel == null) return;
        panel.gameObject.SetActive(active);
        panel.alpha = active ? 1f : 0f;
        panel.interactable = active;
        panel.blocksRaycasts = active;
    }

    // ─── HUD updates ──────────────────────────────────────────────────────

    public void UpdateBlockCounter(int remaining, int total)
    {
        if (blockCounterLabel)
            blockCounterLabel.text = $"{remaining} / {total}";

        if (progressBar)
        {
            float progress = total > 0 ? 1f - (float)remaining / total : 1f;
            progressBar.DOValue(progress, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private void UpdateLevelLabel()
    {
        int lvl = GameManager.Instance.CurrentLevel + 1;
        if (levelLabel)      levelLabel.text     = $"Level {lvl}";
        if (winLevelLabel)   winLevelLabel.text  = $"Level {lvl} Complete!";
        if (loseLevelLabel)  loseLevelLabel.text = $"Level {lvl} Failed";
    }

    // ─── Win animation ────────────────────────────────────────────────────

    private void PlayWinAnimation()
    {
        if (starsContainer == null) return;

        foreach (Transform child in starsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < 3; i++)
        {
            int index = i;
            DOVirtual.DelayedCall(0.2f + index * 0.25f, () =>
            {
                var star = Instantiate(starPrefab, starsContainer);
                star.transform.localScale = Vector3.zero;
                star.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
            });
        }
    }

    // ─── Button callbacks (wired in Inspector) ────────────────────────────

    public void OnPlayPressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.StartGame();
    }

    public void OnRestartPressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.RestartLevel();
    }

    public void OnNextLevelPressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.NextLevel();
    }

    public void OnMenuPressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.GoToMainMenu();
    }

    public void OnPausePressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.ChangeState(GameState.Paused);
    }

    public void OnResumePressed()
    {
        AudioManager.Instance?.PlayButton();
        GameManager.Instance.ChangeState(GameState.Playing);
    }
}
