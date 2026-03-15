using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class WinPanelController : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private TextMeshProUGUI coinsEarnedLabel;
    [SerializeField] private TextMeshProUGUI totalCoinsLabel;

    [Header("Buttons")]
    [SerializeField] private Button nextLevelBtn;

    [Header("UI Elements")]
    [SerializeField] private Image coinIcon;
    [SerializeField] private RectTransform cardPanel; // panel chính animate

    [Header("Settings")]
    [SerializeField] private float animDelay = 0.2f;

    private int _coinsEarned;

    void Awake()
    {
        nextLevelBtn?.onClick.AddListener(OnNextLevel);
        gameObject.SetActive(false);
    }

    public void Show(int coinsEarned, int totalCoins, int levelCompleted)
    {
        gameObject.SetActive(true);
        _coinsEarned = coinsEarned;

        Time.timeScale = 0f; // Dừng tất cả animation game
        AudioManager.Instance?.PlayWin();

        // Reset trạng thái ban đầu
        if (cardPanel != null) cardPanel.localScale = Vector3.zero;
        if (coinsEarnedLabel != null) coinsEarnedLabel.transform.localScale = Vector3.zero;

        StartCoroutine(PlayShowSequence(coinsEarned, totalCoins, levelCompleted));
    }

    private IEnumerator PlayShowSequence(int coinsEarned, int totalCoins, int levelCompleted)
    {

        yield return new WaitForSecondsRealtime(animDelay);

        // 2. Card scale in
        if (cardPanel != null)
            yield return cardPanel.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true).WaitForCompletion();

        yield return new WaitForSecondsRealtime(0.1f);

        // 3. Title animate
        if (titleLabel != null)
        {
            titleLabel.transform.localScale = Vector3.zero;
            titleLabel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (subtitleLabel != null)
            subtitleLabel.text = $"Level {levelCompleted} Complete!";

        yield return new WaitForSecondsRealtime(0.2f);

        // 4. Coin icon bounce
        if (coinIcon != null)
            coinIcon.transform.DOPunchScale(Vector3.one * 0.3f, 0.4f, 5, 0.5f);

        // 5. Coin đếm lên
        if (coinsEarnedLabel != null)
        {
            coinsEarnedLabel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
            yield return CountUpCoins(coinsEarnedLabel, 0, coinsEarned, 0.8f, true);
        }

        // 6. Update total coins
        if (totalCoinsLabel != null)
            totalCoinsLabel.text = totalCoins.ToString();

        yield return new WaitForSecondsRealtime(0.1f);

        // 7. Buttons animate
        nextLevelBtn?.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);
    }

    private IEnumerator CountUpCoins(TextMeshProUGUI label, int from, int to, float duration, bool unscaled = false)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
            label.text = "+" + current + " coins";
            yield return null;
        }
        label.text = "+" + to + " coins";
    }

    private void OnNextLevel()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
        GameManager.Instance.NextLevel();
    }

    public void Hide()
    {
        cardPanel?.DOScale(0f, 0.2f).SetEase(Ease.InBack)
            .OnComplete(() => gameObject.SetActive(false));
    }
}