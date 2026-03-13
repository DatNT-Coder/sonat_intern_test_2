using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance { get; private set; }

    [Header("Particle Prefabs (optional)")]
    [SerializeField] private ParticleSystem exitParticlePrefab;
    [SerializeField] private ParticleSystem bombParticlePrefab;
    [SerializeField] private ParticleSystem winParticlePrefab;

    [Header("Pool Size")]
    [SerializeField] private int exitPoolSize = 15;

    private Queue<ParticleSystem> _exitPool = new Queue<ParticleSystem>();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (exitParticlePrefab != null) PrewarmExitPool();
    }

    private void PrewarmExitPool()
    {
        for (int i = 0; i < exitPoolSize; i++)
        {
            var ps = Instantiate(exitParticlePrefab, transform);
            ps.gameObject.SetActive(false);
            _exitPool.Enqueue(ps);
        }
    }

    public void SpawnExitParticle(Vector3 position, Color color)
    {
        // Không có prefab thì bỏ qua, không crash
        if (exitParticlePrefab == null) return;

        ParticleSystem ps = GetExitParticle();
        ps.transform.position = position;
        ps.gameObject.SetActive(true);
        var main = ps.main;
        main.startColor = color;
        ps.Play();
        StartCoroutine(ReturnExitParticle(ps, main.duration + main.startLifetime.constantMax));
    }

    public void SpawnBombEffect(Vector3 position)
    {
        if (bombParticlePrefab == null) return;
        var ps = Instantiate(bombParticlePrefab, position, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 3f);
    }

    public void SpawnWinEffect()
    {
        if (winParticlePrefab == null) return;
        var ps = Instantiate(winParticlePrefab, Vector3.zero, Quaternion.identity);
        ps.Play();
        Destroy(ps.gameObject, 5f);
    }

    private ParticleSystem GetExitParticle()
    {
        if (_exitPool.Count > 0) return _exitPool.Dequeue();
        return Instantiate(exitParticlePrefab, transform);
    }

    private IEnumerator ReturnExitParticle(ParticleSystem ps, float delay)
    {
        yield return new WaitForSeconds(delay);
        ps.Stop();
        ps.gameObject.SetActive(false);
        _exitPool.Enqueue(ps);
    }
}