using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class LosePanelController : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;

    [Header("Buttons")]
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button moreMovesBtn;
    [SerializeField] private TextMeshProUGUI moreMovesCostLabel;

    [Header("Settings")]
    [SerializeField] private int moreMoveCost = 30;
    [SerializeField] private int moreMoveAmt = 10;
    [SerializeField] private RectTransform cardPanel;

    void Awake()
    {
        retryBtn?.onClick.AddListener(OnRetry);
        moreMovesBtn?.onClick.AddListener(OnMoreMoves);
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;
        AudioManager.Instance?.PlayLose();

        if (moreMovesCostLabel != null)
            moreMovesCostLabel.text = $"+{moreMoveAmt} Moves";

        // Disable nút nếu không đủ xu
        if (moreMovesBtn != null)
            moreMovesBtn.interactable = GameManager.Instance.Coins >= moreMoveCost;

        // Animate card
        if (cardPanel != null)
        {
            cardPanel.localScale = Vector3.zero;
            cardPanel.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        // Shake title
        if (titleLabel != null)
        {
            titleLabel.transform.localScale = Vector3.zero;
            titleLabel.transform.DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true)
                .SetDelay(0.2f);
        }
    }

    private void OnRetry()
    {
        // Retry = reset lại màn chơi từ đầu
        Time.timeScale = 1f;
        cardPanel?.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            gameObject.SetActive(false);
            GameManager.Instance.RetryLevel();
        });
    }

    private void OnMoreMoves()
    {
        // +10 Moves = thêm moves và tiếp tục chơi màn hiện tại (không reset blocks)
        if (!GameManager.Instance.SpendCoins(moreMoveCost))
        {
            moreMovesBtn?.transform.DOShakePosition(0.3f, 8f, 20).SetUpdate(true);
            return;
        }
        Time.timeScale = 1f;
        GameManager.Instance.AddMoves(moreMoveAmt);
        cardPanel?.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
        {
            gameObject.SetActive(false);
            // Resume game mà không reload level
            LevelManager.Instance?.ResumeWithoutReload();
        });
    }

    public void Hide()
    {
        Time.timeScale = 1f;
        gameObject.SetActive(false);
    }
}