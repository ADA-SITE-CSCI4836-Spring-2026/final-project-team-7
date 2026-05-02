using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Prefabs (random pick)")]
    public GameObject[] zombiePrefabs;

    [Header("Spawn Settings")]
    public float spawnInterval = 1.25f;
    public int maxAlive = 18;
    public float startDelay = 1f; // grace period at start

    [Header("Spawn Area")]
    [Tooltip("Half-width of arena on X.")]
    public float areaHalfX = 22f;
    [Tooltip("Half-width of arena on Z.")]
    public float areaHalfZ = 22f;
    [Tooltip("Spawn only at edges this far from center.")]
    public float edgeBuffer = 18f;
    public float spawnY = 0.05f;
    [Tooltip("Optional city spawn zones, relative to this spawner. If set, zombies spawn from these zones instead of one arena edge.")]
    public Vector3[] localSpawnOffsets =
    {
        new Vector3(-30f, 0f, -28f),
        new Vector3(30f, 0f, -28f),
        new Vector3(-30f, 0f, 28f),
        new Vector3(30f, 0f, 28f),
        new Vector3(0f, 0f, -34f),
        new Vector3(0f, 0f, 34f),
        new Vector3(-38f, 0f, 0f),
        new Vector3(38f, 0f, 0f),
        new Vector3(-22f, 0f, 38f),
        new Vector3(22f, 0f, -38f)
    };
    public float spawnPointJitter = 8f;

    [Header("Player Distance")]
    [Tooltip("Don't spawn within this distance of player.")]
    public float minDistanceFromPlayer = 11f;

    [Header("Runtime NavMesh")]
    [Tooltip("Builds a NavMesh from objects named Floor if the scene has no baked NavMesh.")]
    public bool buildNavMeshAtRuntime = true;
    public Vector3 navMeshCenter = Vector3.zero;
    public Vector3 navMeshSize = new Vector3(60f, 10f, 60f);
    public float navMeshSampleRadius = 6f;
    public bool addFallbackFlatNavMesh = true;

    Transform player;
    readonly List<ZombieController> alive = new List<ZombieController>();
    float timer;
    NavMeshData runtimeNavMesh;
    NavMeshDataInstance runtimeNavMeshInstance;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        EnsureNavMesh();
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
        if (localSpawnOffsets != null && localSpawnOffsets.Length > 0)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector3 candidate = PickSpawnPointPosition();
                if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;
                if (TrySampleNavMesh(candidate, out Vector3 navMeshPosition)) return navMeshPosition;
            }
        }

        return PickEdgePositionFallback();
    }

    Vector3 PickSpawnPointPosition()
    {
        Vector3 offset = localSpawnOffsets[Random.Range(0, localSpawnOffsets.Length)];
        Vector2 jitter = Random.insideUnitCircle * spawnPointJitter;
        offset.x += jitter.x;
        offset.z += jitter.y;
        offset.y = spawnY;
        return transform.position + offset;
    }

    Vector3 PickEdgePositionFallback()
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

            Vector3 candidate = transform.position + new Vector3(x, spawnY, z);
            if (player != null && Vector3.Distance(candidate, player.position) < minDistanceFromPlayer) continue;
            if (TrySampleNavMesh(candidate, out Vector3 navMeshPosition)) return navMeshPosition;
        }
        // Fallback
        Vector3 fallback = transform.position + new Vector3(edgeBuffer, spawnY, edgeBuffer);
        if (TrySampleNavMesh(fallback, out Vector3 sampledFallback)) return sampledFallback;
        return fallback;
    }

    bool TrySampleNavMesh(Vector3 candidate, out Vector3 position)
    {
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            position = hit.position;
            return true;
        }

        position = candidate;
        return false;
    }

    void EnsureNavMesh()
    {
        if (!buildNavMeshAtRuntime) return;
        if (NavMesh.SamplePosition(transform.position, out _, Mathf.Max(navMeshSize.x, navMeshSize.z), NavMesh.AllAreas)) return;

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        CollectFloorSources(sources);
        if (sources.Count == 0 && addFallbackFlatNavMesh)
        {
            AddFallbackFlatSource(sources);
        }

        if (sources.Count == 0)
        {
            Debug.LogWarning("ZombieSpawner could not find any walkable geometry to build a runtime NavMesh.");
            return;
        }

        if (NavMesh.GetSettingsCount() == 0)
        {
            Debug.LogWarning("ZombieSpawner could not build a runtime NavMesh because no NavMesh agent settings exist.");
            return;
        }

        NavMeshBuildSettings settings = NavMesh.GetSettingsByIndex(0);
        Vector3 center = navMeshCenter == Vector3.zero ? transform.position : navMeshCenter;
        Bounds buildBounds = new Bounds(center, navMeshSize);
        runtimeNavMesh = NavMeshBuilder.BuildNavMeshData(settings, sources, buildBounds, Vector3.zero, Quaternion.identity);

        if (runtimeNavMesh == null)
        {
            Debug.LogWarning("ZombieSpawner failed to build runtime NavMesh data.");
            return;
        }

        runtimeNavMeshInstance = NavMesh.AddNavMeshData(runtimeNavMesh);
    }

    void CollectFloorSources(List<NavMeshBuildSource> sources)
    {
        foreach (var meshCollider in FindObjectsOfType<MeshCollider>())
        {
            if (!IsWalkableSurface(meshCollider.gameObject) || meshCollider.sharedMesh == null) continue;

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = meshCollider.sharedMesh,
                transform = meshCollider.transform.localToWorldMatrix,
                area = 0
            });
        }

        foreach (var boxCollider in FindObjectsOfType<BoxCollider>())
        {
            if (!IsWalkableSurface(boxCollider.gameObject)) continue;

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = boxCollider.size,
                transform = Matrix4x4.TRS(
                    boxCollider.transform.TransformPoint(boxCollider.center),
                    boxCollider.transform.rotation,
                    boxCollider.transform.lossyScale),
                area = 0
            });
        }

        CollectObstacleSources(sources);
    }

    void CollectObstacleSources(List<NavMeshBuildSource> sources)
    {
        int notWalkableArea = NavMesh.GetAreaFromName("Not Walkable");
        if (notWalkableArea < 0) notWalkableArea = 1;

        foreach (var boxCollider in FindObjectsOfType<BoxCollider>())
        {
            if (!IsObstacleSurface(boxCollider.gameObject)) continue;

            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = boxCollider.size,
                transform = Matrix4x4.TRS(
                    boxCollider.transform.TransformPoint(boxCollider.center),
                    boxCollider.transform.rotation,
                    boxCollider.transform.lossyScale),
                area = notWalkableArea
            });
        }
    }

    void AddFallbackFlatSource(List<NavMeshBuildSource> sources)
    {
        Vector3 center = navMeshCenter == Vector3.zero ? transform.position : navMeshCenter;
        sources.Add(new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box,
            size = new Vector3(navMeshSize.x, 0.1f, navMeshSize.z),
            transform = Matrix4x4.TRS(center, Quaternion.identity, Vector3.one),
            area = 0
        });
    }

    bool IsWalkableSurface(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;

        string name = obj.name.ToLowerInvariant();
        Transform root = obj.transform.root;
        string rootName = root != null ? root.name.ToLowerInvariant() : "";

        return name.Contains("floor") ||
               name.Contains("road") ||
               name.Contains("grass") ||
               name.Contains("sand") ||
               name.Contains("bridge") ||
               name.Contains("platform") ||
               rootName.Contains("roads") ||
               rootName.Contains("grass");
    }

    bool IsObstacleSurface(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;

        Transform root = obj.transform.root;
        string rootName = root != null ? root.name.ToLowerInvariant() : "";
        return rootName.Contains("building") ||
               rootName.Contains("cars") ||
               rootName.Contains("tree") ||
               rootName.Contains("props") ||
               rootName.Contains("billboards") ||
               rootName.Contains("column");
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

    void OnDestroy()
    {
        runtimeNavMeshInstance.Remove();
    }
}
