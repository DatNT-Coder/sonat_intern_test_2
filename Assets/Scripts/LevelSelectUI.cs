using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Displays a scrollable grid of level buttons.
/// Locked levels are greyed out. Completed levels show a star.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private LevelSelectButton buttonPrefab;
    [SerializeField] private int totalLevels = 20;

    private List<LevelSelectButton> _buttons = new List<LevelSelectButton>();

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        // Clear old buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        _buttons.Clear();

        int highestUnlocked = SaveSystem.Load().highestLevel;

        for (int i = 0; i < totalLevels; i++)
        {
            var btn = Instantiate(buttonPrefab, buttonContainer);
            bool unlocked = i <= highestUnlocked;
            btn.Setup(i, unlocked, onLevelSelected: (idx) =>
            {
                AudioManager.Instance?.PlayButton();
                GameManager.Instance.ChangeState(GameState.Playing);
            });

            // Stagger spawn animation
            btn.transform.localScale = Vector3.zero;
            btn.transform.DOScale(1f, 0.25f)
               .SetDelay(i * 0.04f)
               .SetEase(Ease.OutBack);

            _buttons.Add(btn);
        }
    }
}
