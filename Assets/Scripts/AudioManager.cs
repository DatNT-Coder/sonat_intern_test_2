using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    private float _sfxVolume = 1f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitPool();
    }

    void Start()
    {
        if (bgMusic != null && musicSource != null)
        {
            musicSource.clip = bgMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    private void InitPool()
    {
        if (sfxPrefab == null) return;
        for (int i = 0; i < poolSize; i++)
        {
            var source = Instantiate(sfxPrefab, transform);
            source.gameObject.SetActive(false);
            _sfxPool.Enqueue(source);
        }
    }

    public void PlaySFX(AudioClip clip, float pitch = 1f)
    {
        if (clip == null || _sfxVolume <= 0f) return;
        var source = GetPooledSource();
        if (source == null) return;
        source.pitch = pitch;
        source.clip = clip;
        source.volume = _sfxVolume;
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

    public void SetMusicVolume(float vol)
    {
        if (musicSource != null) musicSource.volume = vol;
    }

    public void SetSFXVolume(float vol)
    {
        _sfxVolume = vol;
    }

    public void StopAllSFX()
    {
        foreach (var source in GetComponentsInChildren<AudioSource>())
            if (source != musicSource) source.Stop();
    }

    private AudioSource GetPooledSource()
    {
        if (_sfxPool.Count > 0) return _sfxPool.Dequeue();
        if (sfxPrefab != null) return Instantiate(sfxPrefab, transform);
        return null;
    }

    private IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.gameObject.SetActive(false);
        _sfxPool.Enqueue(source);
    }
}