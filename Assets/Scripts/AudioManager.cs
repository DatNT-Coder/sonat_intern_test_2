using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized audio management. Singleton.
/// Supports SFX pooling and background music.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip bgMusic;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxPrefab;
    [SerializeField] private int poolSize = 10;

    [Header("Clips")]
    [SerializeField] public AudioClip blockTapClip;
    [SerializeField] public AudioClip blockSlideClip;
    [SerializeField] public AudioClip blockExitClip;
    [SerializeField] public AudioClip winClip;
    [SerializeField] public AudioClip loseClip;
    [SerializeField] public AudioClip buttonClickClip;

    private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitPool();
    }

    void Start()
    {
        PlayMusic(bgMusic);
    }

    private void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            var source = Instantiate(sfxPrefab, transform);
            source.gameObject.SetActive(false);
            _sfxPool.Enqueue(source);
        }
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip == null) return;
        var source = GetPooledSource();
        source.pitch = pitch;
        source.clip = clip;
        source.gameObject.SetActive(true);
        source.Play();
        StartCoroutine(ReturnToPool(source, clip.length + 0.1f));
    }

    public void PlayBlockTap() => PlaySFX(blockTapClip, Random.Range(0.95f, 1.05f));
    public void PlayBlockSlide() => PlaySFX(blockSlideClip);
    public void PlayBlockExit() => PlaySFX(blockExitClip, Random.Range(0.9f, 1.1f));
    public void PlayWin() => PlaySFX(winClip);
    public void PlayLose() => PlaySFX(loseClip);
    public void PlayButton() => PlaySFX(buttonClickClip);

    private AudioSource GetPooledSource()
    {
        if (_sfxPool.Count > 0) return _sfxPool.Dequeue();
        return Instantiate(sfxPrefab, transform);
    }

    private System.Collections.IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.gameObject.SetActive(false);
        _sfxPool.Enqueue(source);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null || clip == null) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void SetMusicVolume(float vol) => musicSource.volume = vol;
    public void SetSFXVolume(float vol) { /* apply to all active sources */ }
}
