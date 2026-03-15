using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeBtn;
    [SerializeField] private Button musicBtn;
    [SerializeField] private Button sfxBtn;

    [Header("Button Labels (thay cho icon)")]
    [SerializeField] private TextMeshProUGUI musicLabel;
    [SerializeField] private TextMeshProUGUI sfxLabel;

    [Header("Button Colors")]
    [SerializeField] private Image musicBtnImage;
    [SerializeField] private Image sfxBtnImage;
    [SerializeField] private Color onColor = new Color(0.18f, 0.8f, 0.44f); // xanh lá
    [SerializeField] private Color offColor = new Color(0.6f, 0.6f, 0.6f);   // xám

    [Header("Panel")]
    [SerializeField] private RectTransform cardPanel;

    private bool _musicOn = true;
    private bool _sfxOn = true;

    void Awake()
    {
        closeBtn?.onClick.AddListener(Hide);
        musicBtn?.onClick.AddListener(ToggleMusic);
        sfxBtn?.onClick.AddListener(ToggleSFX);
        gameObject.SetActive(false);
    }

    void Start()
    {
        _musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        _sfxOn = PlayerPrefs.GetInt("SFXOn", 1) == 1;
        UpdateButtons();
        ApplyAudio();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (cardPanel != null)
        {
            cardPanel.localScale = Vector3.zero;
            cardPanel.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    public void Hide()
    {
        cardPanel?.DOScale(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void ToggleMusic()
    {
        _musicOn = !_musicOn;
        PlayerPrefs.SetInt("MusicOn", _musicOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtons();
        ApplyAudio();
        musicBtn?.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f).SetUpdate(true);
    }

    private void ToggleSFX()
    {
        _sfxOn = !_sfxOn;
        PlayerPrefs.SetInt("SFXOn", _sfxOn ? 1 : 0);
        PlayerPrefs.Save();
        UpdateButtons();
        ApplyAudio();
        sfxBtn?.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f).SetUpdate(true);
    }

    private void UpdateButtons()
    {
        // Đổi màu nút ON/OFF
        if (musicBtnImage != null)
            musicBtnImage.color = _musicOn ? onColor : offColor;
        if (sfxBtnImage != null)
            sfxBtnImage.color = _sfxOn ? onColor : offColor;

        // Đổi text ON/OFF
        if (musicLabel != null)
            musicLabel.text = _musicOn ? "ON" : "OFF";
        if (sfxLabel != null)
            sfxLabel.text = _sfxOn ? "ON" : "OFF";
    }

    private void ApplyAudio()
    {
        AudioManager.Instance?.SetMusicVolume(_musicOn ? 1f : 0f);
        AudioManager.Instance?.SetSFXVolume(_sfxOn ? 1f : 0f);
    }
}