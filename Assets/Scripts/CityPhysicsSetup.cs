using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-1000)]
public class CityPhysicsSetup : MonoBehaviour
{
    [System.Serializable]
    public class SupportPlatform
    {
        public string name = "BridgePlatform";
        public Vector3 center = new Vector3(42f, 0.05f, 35f);
        public Vector3 size = new Vector3(28f, 0.18f, 14f);
        public Color color = new Color(0.62f, 0.58f, 0.52f, 1f);
        public bool visible = false;
    }

    [Header("Scene Roots")]
    public string[] walkableRootNames = { "Roads", "Roads_Grass", "Roads_Sand" };
    public string[] obstacleRootNames = { "Buildings", "Cars", "Tree", "Sing_Column" };

    [Header("Gap Fixes")]
    public SupportPlatform[] supportPlatforms =
    {
        new SupportPlatform
        {
            name = "Bridge_To_Final_Block",
            center = new Vector3(43f, 0.05f, 35f),
            size = new Vector3(30f, 0.18f, 14f),
            color = new Color(0.58f, 0.56f, 0.52f, 1f),
            visible = false
        },
        new SupportPlatform
        {
            name = "Path_Extension_To_Apartments",
            center = new Vector3(35f, 0.06f, 48f),
            size = new Vector3(16f, 0.16f, 24f),
            color = new Color(0.78f, 0.48f, 0.32f, 1f),
            visible = false
        }
    };

    [Header("Fall Safety")]
    public bool createSafetyFloor = true;
    public string safetyFloorName = "City_Runtime_MapContact";
    public Vector3 safetyFloorCenter = new Vector3(35f, -0.05f, 35f);
    public Vector3 safetyFloorSize = new Vector3(78f, 0.1f, 78f);

    [Header("Collider Options")]
    public bool addWalkableMeshColliders = false;
    public bool addWalkableBoxFallbacks = true;
    public float minimumWalkableBoxThickness = 0.25f;
    public bool addObstacleMeshColliders = false;
    public bool addObstacleBoxFallbacks = true;
    [Range(0.2f, 1f)] public float defaultObstacleColliderScale = 0.72f;
    [Range(0.2f, 1f)] public float carColliderScale = 0.78f;
    [Range(0.2f, 1f)] public float buildingColliderScale = 0.92f;
    public float columnColliderWidth = 0.35f;
    public float columnColliderHeightPadding = 0.15f;
    public bool addNavMeshObstacleCarving = false;
    public bool keepObstacleTriggers = false;

    [Header("Player Safety")]
    public Transform player;
    public float playerSpawnHeight = 0.08f;

    readonly HashSet<Mesh> warnedMeshes = new HashSet<Mesh>();

    void Awake()
    {
        CreateSupportPlatforms();
        CreateSafetyFloor();
        AddWalkableColliders();
        AddObstacleColliders();
        LiftPlayerAboveGround();
    }

    void CreateSupportPlatforms()
    {
        foreach (SupportPlatform platform in supportPlatforms)
        {
            if (platform == null || platform.size.sqrMagnitude <= 0.001f) continue;
            if (GameObject.Find(platform.name) != null) continue;

            GameObject bridge = platform.visible ? GameObject.CreatePrimitive(PrimitiveType.Cube) : new GameObject(platform.name);
            bridge.name = platform.name;
            bridge.transform.position = platform.center;
            bridge.transform.localScale = platform.size;

            if (!platform.visible)
            {
                BoxCollider box = bridge.AddComponent<BoxCollider>();
                box.isTrigger = false;
                continue;
            }

            Renderer renderer = bridge.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = platform.color;
            }
        }
    }

    void CreateSafetyFloor()
    {
        if (!createSafetyFloor || GameObject.Find(safetyFloorName) != null) return;

        GameObject floor = new GameObject(safetyFloorName);
        floor.transform.position = safetyFloorCenter;
        floor.transform.localScale = safetyFloorSize;

        BoxCollider box = floor.AddComponent<BoxCollider>();
        box.isTrigger = false;
    }

    void AddWalkableColliders()
    {
        foreach (string rootName in walkableRootNames)
        {
            Transform root = FindRoot(rootName);
            if (root == null) continue;

            if (addWalkableMeshColliders) AddMeshColliders(root, true);
            if (addWalkableBoxFallbacks) AddBoxFallbacks(root, true);
        }
    }

    void AddObstacleColliders()
    {
        foreach (string rootName in obstacleRootNames)
        {
            Transform root = FindRoot(rootName);
            if (root == null) continue;

            bool treeRoot = rootName.ToLowerInvariant().Contains("tree");
            if (treeRoot)
            {
                AddTreeTrunkColliders(root);
                continue;
            }

            bool columnRoot = rootName.ToLowerInvariant().Contains("column");
            if (columnRoot)
            {
                AddColumnColliders(root);
                continue;
            }

            if (addObstacleMeshColliders) AddMeshColliders(root, false);
            if (addObstacleBoxFallbacks) AddBoxFallbacks(root, false);
            if (addNavMeshObstacleCarving) AddNavMeshObstacles(root);
        }
    }

    void AddMeshColliders(Transform root, bool walkable)
    {
        foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null) continue;
            if (filter.GetComponent<Collider>() != null) continue;

            MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            collider.isTrigger = false;

            if (!walkable && !keepObstacleTriggers)
            {
                collider.isTrigger = false;
            }

            if (!filter.sharedMesh.isReadable && warnedMeshes.Add(filter.sharedMesh))
            {
                // Non-readable meshes still work as sharedMesh references for MeshCollider in most imports.
                // This warning makes it easier to diagnose importer settings if Unity rejects a specific mesh.
                Debug.Log($"CityPhysicsSetup added collider for non-readable mesh: {filter.sharedMesh.name}");
            }
        }
    }

    void AddBoxFallbacks(Transform root, bool walkable)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.GetComponent<Collider>() != null) continue;
            if (ShouldSkipRenderer(renderer, walkable)) continue;

            BoxCollider box = renderer.gameObject.AddComponent<BoxCollider>();
            Bounds bounds = renderer.bounds;
            Vector3 worldSize = bounds.size;
            if (walkable) worldSize.y = Mathf.Max(worldSize.y, minimumWalkableBoxThickness);
            else worldSize = ShrinkObstacleSize(renderer, worldSize);

            box.center = renderer.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(
                worldSize.x / SafeAbs(renderer.transform.lossyScale.x),
                worldSize.y / SafeAbs(renderer.transform.lossyScale.y),
                worldSize.z / SafeAbs(renderer.transform.lossyScale.z));
            box.isTrigger = false;
        }
    }

    void AddTreeTrunkColliders(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.GetComponent<Collider>() != null) continue;
            if (ShouldSkipRenderer(renderer, false)) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.y < 1.5f) continue;

            BoxCollider trunk = renderer.gameObject.AddComponent<BoxCollider>();
            float trunkHeight = Mathf.Min(bounds.size.y * 0.55f, 2.4f);
            float trunkWidthX = Mathf.Clamp(bounds.size.x * 0.18f, 0.35f, 1.1f);
            float trunkWidthZ = Mathf.Clamp(bounds.size.z * 0.18f, 0.35f, 1.1f);
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y + trunkHeight * 0.5f, bounds.center.z);

            trunk.center = renderer.transform.InverseTransformPoint(center);
            trunk.size = new Vector3(
                trunkWidthX / SafeAbs(renderer.transform.lossyScale.x),
                trunkHeight / SafeAbs(renderer.transform.lossyScale.y),
                trunkWidthZ / SafeAbs(renderer.transform.lossyScale.z));
            trunk.isTrigger = false;
        }
    }

    void AddColumnColliders(Transform root)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.GetComponent<Collider>() != null) continue;
            if (ShouldSkipRenderer(renderer, false)) continue;

            Bounds bounds = renderer.bounds;
            if (bounds.size.y < 1f) continue;

            BoxCollider column = renderer.gameObject.AddComponent<BoxCollider>();
            float width = Mathf.Min(columnColliderWidth, Mathf.Max(0.2f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.85f));
            float height = Mathf.Max(0.5f, bounds.size.y - columnColliderHeightPadding);
            Vector3 center = new Vector3(bounds.center.x, bounds.min.y + height * 0.5f, bounds.center.z);

            column.center = renderer.transform.InverseTransformPoint(center);
            column.size = new Vector3(
                width / SafeAbs(renderer.transform.lossyScale.x),
                height / SafeAbs(renderer.transform.lossyScale.y),
                width / SafeAbs(renderer.transform.lossyScale.z));
            column.isTrigger = false;
        }
    }

    Vector3 ShrinkObstacleSize(Renderer renderer, Vector3 worldSize)
    {
        string objectName = renderer.gameObject.name.ToLowerInvariant();
        Transform root = renderer.transform.root;
        string rootName = root != null ? root.name.ToLowerInvariant() : "";

        float scale = defaultObstacleColliderScale;
        if (rootName.Contains("building") || objectName.Contains("bld") || objectName.Contains("building"))
        {
            scale = buildingColliderScale;
        }
        else if (rootName.Contains("cars") || objectName.Contains("car"))
        {
            scale = carColliderScale;
            worldSize.y *= 0.85f;
        }

        worldSize.x *= scale;
        worldSize.z *= scale;
        return worldSize;
    }

    bool ShouldSkipRenderer(Renderer renderer, bool walkable)
    {
        if (renderer == null || !renderer.gameObject.activeInHierarchy) return true;
        if (renderer.GetComponent<DrainZone>() != null || renderer.GetComponent<ExitZone>() != null) return true;

        string lowerName = renderer.gameObject.name.ToLowerInvariant();
        Bounds bounds = renderer.bounds;

        if (walkable)
        {
            return lowerName.Contains("grass_") ||
                   lowerName.Contains("bush") ||
                   lowerName.Contains("tree") ||
                   lowerName.Contains("rock") ||
                   bounds.size.x < 1f ||
                   bounds.size.z < 1f;
        }

        if (lowerName.Contains("grass") ||
            lowerName.Contains("bush") ||
            lowerName.Contains("leaf") ||
            lowerName.Contains("leaves") ||
            lowerName.Contains("rock"))
        {
            return true;
        }

        return bounds.size.y < 0.5f || bounds.size.x < 0.35f || bounds.size.z < 0.35f;
    }

    void AddNavMeshObstacles(Transform root)
    {
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>(true))
        {
            if (box.isTrigger) continue;

            NavMeshObstacle obstacle = box.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = box.gameObject.AddComponent<NavMeshObstacle>();

            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = box.center;
            obstacle.size = box.size;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
        }
    }

    void LiftPlayerAboveGround()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) player = playerObject.transform;
        }

        if (player == null) return;

        Vector3 position = player.position;
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 50f, Vector3.down, 100f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));

        foreach (RaycastHit hit in hits)
        {
            if (!IsSafeGroundHit(hit)) continue;
            position.y = hit.point.y + playerSpawnHeight;
            player.position = position;
            return;
        }
    }

    bool IsSafeGroundHit(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (hit.collider.CompareTag("Player")) return false;

        Transform root = hit.collider.transform.root;
        string rootName = root != null ? root.name.ToLowerInvariant() : "";
        string objectName = hit.collider.gameObject.name.ToLowerInvariant();

        return objectName.Contains("road") ||
               objectName.Contains("sidewalk") ||
               objectName.Contains("pedastrian") ||
               objectName.Contains("pedestrian") ||
               objectName.Contains("sand") ||
               objectName.Contains("bridge") ||
               objectName.Contains("platform") ||
               objectName.Contains("mapcontact") ||
               rootName.Contains("roads") ||
               rootName.Contains("bridge") ||
               rootName.Contains("platform");
    }

    Transform FindRoot(string rootName)
    {
        GameObject obj = GameObject.Find(rootName);
        return obj != null ? obj.transform : null;
    }

    float SafeAbs(float value)
    {
        value = Mathf.Abs(value);
        return value < 0.0001f ? 1f : value;
    }
}
