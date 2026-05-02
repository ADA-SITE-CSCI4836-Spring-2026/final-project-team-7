using UnityEngine;
using System.Collections.Generic;

public class ShardSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string label = "Green";
        public GameObject prefab;
        [Min(0)] public int count = 5;
    }

    [Header("Prefabs to Spawn")]
    public List<SpawnEntry> entries = new List<SpawnEntry>();

    [Header("Spawn Area (relative to this object's position)")]
    [Tooltip("Half-width on X axis. Total area = 2 * areaHalfX.")]
    public float areaHalfX = 22f;
    [Tooltip("Half-width on Z axis.")]
    public float areaHalfZ = 22f;
    [Tooltip("Y position where shards spawn (above floor).")]
    public float spawnY = 0.7f;

    [Header("Placement Rules")]
    [Tooltip("Minimum distance between any two spawned shards.")]
    public float minDistanceBetween = 2f;
    [Tooltip("Minimum distance from the player's starting position.")]
    public float minDistanceFromPlayer = 4f;
    [Tooltip("Layers that block spawning (walls, drain zones, exit).")]
    public LayerMask obstacleLayers;
    [Tooltip("Radius checked for obstacles around each spawn point.")]
    public float obstacleCheckRadius = 0.7f;
    [Tooltip("Max attempts per shard before giving up.")]
    public int maxAttemptsPerShard = 30;

    [Header("Player Reference")]
    [Tooltip("Optional. Auto-finds GameObject tagged 'Player' if empty.")]
    public Transform player;

    [Header("Randomness")]
    [Tooltip("0 = different layout every play. Any other number = same layout for that seed.")]
    public int seed = 0;

    readonly List<Vector3> placed = new List<Vector3>();

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (seed != 0) Random.InitState(seed);

        foreach (var entry in entries)
        {
            if (entry.prefab == null) continue;
            for (int i = 0; i < entry.count; i++)
            {
                if (TryFindSpawnPosition(out Vector3 pos))
                {
                    Instantiate(entry.prefab, pos, Quaternion.identity, transform);
                    placed.Add(pos);
                }
                else
                {
                    Debug.LogWarning($"ShardSpawner: couldn't place {entry.label} #{i + 1} after {maxAttemptsPerShard} attempts. Area too cramped?");
                }
            }
        }
    }

    bool TryFindSpawnPosition(out Vector3 result)
    {
        for (int attempt = 0; attempt < maxAttemptsPerShard; attempt++)
        {
            float x = Random.Range(-areaHalfX, areaHalfX);
            float z = Random.Range(-areaHalfZ, areaHalfZ);
            Vector3 candidate = transform.position + new Vector3(x, spawnY, z);

            // Reject if too close to player spawn
            if (player != null)
            {
                Vector3 flatPlayer = new Vector3(player.position.x, candidate.y, player.position.z);
                if (Vector3.Distance(candidate, flatPlayer) < minDistanceFromPlayer) continue;
            }

            // Reject if too close to another shard
            bool tooClose = false;
            foreach (var p in placed)
            {
                if (Vector3.Distance(candidate, p) < minDistanceBetween) { tooClose = true; break; }
            }
            if (tooClose) continue;

            // Reject if overlapping obstacles (walls, drain zones, exit)
            if (obstacleLayers.value != 0)
            {
                if (Physics.CheckSphere(candidate, obstacleCheckRadius, obstacleLayers)) continue;
            }

            result = candidate;
            return true;
        }
        result = Vector3.zero;
        return false;
    }

    // Visualize the spawn area in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Vector3 center = transform.position + Vector3.up * spawnY;
        Vector3 size = new Vector3(areaHalfX * 2f, 0.1f, areaHalfZ * 2f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
}