using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// A single button in the level select grid.
/// </summary>
public class LevelSelectButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelNumberText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private GameObject starIcon;
    [SerializeField] private Button button;
    [SerializeField] private Image background;

    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color lockedColor   = new Color(0.5f, 0.5f, 0.5f);

    private int _levelIndex;
    private Action<int> _onSelected;

    public void Setup(int levelIndex, bool unlocked, Action<int> onLevelSelected)
    {
        _levelIndex = levelIndex;
        _onSelected = onLevelSelected;

        levelNumberText.text = (levelIndex + 1).ToString();
        lockIcon.SetActive(!unlocked);
        starIcon.SetActive(false); // TODO: check save data for star completion
        background.color = unlocked ? unlockedColor : lockedColor;
        button.interactable = unlocked;
        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _onSelected?.Invoke(_levelIndex);
    }

    void OnDestroy()
    {
        button.onClick.RemoveListener(OnClick);
    }
}
