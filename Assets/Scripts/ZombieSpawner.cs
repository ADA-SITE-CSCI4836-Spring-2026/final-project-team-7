using UnityEngine;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs (random pick)")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public float spawnInterval = 8f;
    public int maxAlive = 5;
    public float startDelay = 5f; // grace period at start

    [Header("Spawn Area")]
    [Tooltip("Half-width of arena on X.")]
    public float areaHalfX = 22f;
    [Tooltip("Half-width of arena on Z.")]
    public float areaHalfZ = 22f;
    [Tooltip("Spawn only at edges this far from center.")]
    public float edgeBuffer = 18f;
    public float spawnY = 0.05f;

    [Header("Player Distance")]
    [Tooltip("Don't spawn within this distance of player.")]
    public float minDistanceFromPlayer = 8f;

    Transform player;
    readonly List<ZombieController> alive = new List<ZombieController>();
    float timer;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        timer = -startDelay;
    }

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver || GameManager.Instance.IsWon || GameManager.Instance.IsPaused) return;
        if (zombiePrefabs == null || zombiePrefabs.Length == 0) return;

        // Clean up destroyed entries
        alive.RemoveAll(z => z == null);

        timer += Time.deltaTime;
        if (timer >= spawnInterval && alive.Count < maxAlive)
        {
            timer = 0f;
            SpawnOne();
        }
    }

    void SpawnOne()
    {
        Vector3 pos = PickEdgePosition();
        var prefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        var go = Instantiate(prefab, pos, Quaternion.identity, transform);
        var zc = go.GetComponent<ZombieController>();
        if (zc != null) alive.Add(zc);
    }

    Vector3 PickEdgePosition()
    {
        for (int i = 0; i < 20; i++)
        {
            // Pick a random edge: 0=N, 1=S, 2=E, 3=W
            int edge = Random.Range(0, 4);
            float x, z;
            switch (edge)
            {
                case 0: x = Random.Range(-areaHalfX, areaHalfX); z = edgeBuffer;  break;
                case 1: x = Random.Range(-areaHalfX, areaHalfX); z = -edgeBuffer; break;
                case 2: x = edgeBuffer;  z = Random.Range(-areaHalfZ, areaHalfZ); break;
                default: x = -edgeBuffer; z = Random.Range(-areaHalfZ, areaHalfZ); break;
            }

            Vector3 candidate = new Vector3(x, spawnY, z);
            if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;
            return candidate;
        }
        // Fallback
        return new Vector3(edgeBuffer, spawnY, edgeBuffer);
    }

    public void KillZombiesInRadius(Vector3 center, float radius)
    {
        foreach (var z in alive)
        {
            if (z == null) continue;
            if (Vector3.Distance(z.transform.position, center) <= radius)
            {
                z.Die();
            }
        }
        alive.RemoveAll(z => z == null);
    }
}