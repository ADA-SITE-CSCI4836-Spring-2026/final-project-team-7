using UnityEngine;
using UnityEngine.AI;

public class ZombieController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float stopDistance = 0f;
    public float turnSpeed = 8f;

    [Header("Damage")]
    [Tooltip("Drain rate added to GameManager while touching player.")]
    public float drainRateOnTouch = 5f;
    [Tooltip("Backup hit radius used in case trigger events miss a close player.")]
    public float contactDistance = 1.1f;

    [Header("Grounding")]
    public float groundRayHeight = 2f;
    public float groundRayDistance = 6f;
    public LayerMask groundMask = ~0;

    [Header("Jumping")]
    public float jumpHeight = 1.2f;
    public float jumpDuration = 0.45f;

    [Header("Death FX")]
    public float deathDuration = 0.4f;

    [Header("Procedural Animation")]
    public bool animateProceduralWalk = true;
    public float walkCycleSpeed = 4f;
    public float bodyBobHeight = 0.06f;
    public float bodyLeanAngle = 6f;
    public float limbSwingAngle = 24f;

    Transform player;
    bool isDying;
    bool triggerTouchingPlayer;
    bool registeredContact;
    Animator anim;
    NavMeshAgent agent;
    bool traversingLink;
    float repathTimer;
    float walkCycle;
    float visualMoveSpeed;
    Vector3 lastPosition;
    Transform visualRoot;
    Vector3 visualRootStartLocalPosition;
    Transform hips;
    Transform leftUpperLeg;
    Transform rightUpperLeg;
    Transform leftUpperArm;
    Transform rightUpperArm;
    Quaternion hipsStartRotation;
    Quaternion leftUpperLegStartRotation;
    Quaternion rightUpperLegStartRotation;
    Quaternion leftUpperArmStartRotation;
    Quaternion rightUpperArmStartRotation;

    const float RepathInterval = 0.15f;
    const float NavMeshSampleRadius = 4f;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            CacheProceduralRig();
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();

        agent.speed = speed;
        agent.stoppingDistance = stopDistance;
        agent.angularSpeed = turnSpeed * 180f;
        agent.acceleration = Mathf.Max(speed * 8f, 8f);
        agent.autoBraking = true;
        agent.updateRotation = false;
        agent.autoTraverseOffMeshLink = false;
        agent.radius = 0.45f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.avoidancePriority = Random.Range(20, 60);
    }

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        if (agent != null && !agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        if (isDying)
        {
            SetContact(false);
            return;
        }

        if (GameManager.Instance != null &&
            (GameManager.Instance.IsGameOver || GameManager.Instance.IsWon || GameManager.Instance.IsPaused))
        {
            StopMovement();
            SetContact(false);
            return;
        }

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
            else return;
        }

        MoveTowardPlayer();
        UpdateContact();
    }

    void MoveTowardPlayer()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = speed;
            agent.stoppingDistance = stopDistance;

            if (agent.isOnOffMeshLink)
            {
                if (!traversingLink) StartCoroutine(TraverseOffMeshLink());
                return;
            }

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f)
            {
                agent.SetDestination(player.position);
                repathTimer = RepathInterval;
            }

            Vector3 facing = agent.desiredVelocity.sqrMagnitude > 0.01f ? agent.desiredVelocity : toPlayer;
            FaceDirection(facing);
            return;
        }

        // Fallback keeps the prototype playable if the runtime NavMesh cannot be built.
        float dist = toPlayer.magnitude;
        if (dist > stopDistance)
        {
            Vector3 dir = toPlayer.normalized;
            transform.position += dir * speed * Time.deltaTime;
            SnapFallbackToGround();
            FaceDirection(dir);
        }
    }

    void SnapFallbackToGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * groundRayHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayHeight + groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
    }

    void UpdateContact()
    {
        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        bool closeEnough = toPlayer.sqrMagnitude <= contactDistance * contactDistance;
        SetContact(triggerTouchingPlayer || closeEnough);
    }

    void SetContact(bool active)
    {
        if (GameManager.Instance == null)
        {
            registeredContact = false;
            return;
        }

        if (registeredContact == active) return;
        registeredContact = active;

        if (active) GameManager.Instance.RegisterZombieContact(this);
        else GameManager.Instance.UnregisterZombieContact(this);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDying) return;
        if (!other.CompareTag("Player")) return;
        triggerTouchingPlayer = true;
        SetContact(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        triggerTouchingPlayer = false;
        if (player != null) UpdateContact();
        else SetContact(false);
    }

    public void Die()
    {
        if (isDying) return;
        isDying = true;
        triggerTouchingPlayer = false;
        SetContact(false);

        // Disable collider so we can't be killed twice
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        if (agent != null) agent.enabled = false;

        // Simple death sink
        StartCoroutine(SinkAndDestroy());
    }

    public void StopNow()
    {
        StopMovement();
        SetContact(false);
        enabled = false;
    }

    void StopMovement()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
        }
    }

    void OnDisable()
    {
        triggerTouchingPlayer = false;
        SetContact(false);
    }

    void LateUpdate()
    {
        UpdateProceduralAnimation();
        lastPosition = transform.position;
    }

    void CacheProceduralRig()
    {
        visualRoot = anim.transform;
        visualRootStartLocalPosition = visualRoot.localPosition;

        hips = anim.GetBoneTransform(HumanBodyBones.Hips);
        leftUpperLeg = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        rightUpperLeg = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        leftUpperArm = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        rightUpperArm = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);

        if (hips != null) hipsStartRotation = hips.localRotation;
        if (leftUpperLeg != null) leftUpperLegStartRotation = leftUpperLeg.localRotation;
        if (rightUpperLeg != null) rightUpperLegStartRotation = rightUpperLeg.localRotation;
        if (leftUpperArm != null) leftUpperArmStartRotation = leftUpperArm.localRotation;
        if (rightUpperArm != null) rightUpperArmStartRotation = rightUpperArm.localRotation;
    }

    void UpdateProceduralAnimation()
    {
        if (anim == null) return;

        float frameSpeed = Time.deltaTime > 0f ? Vector3.Distance(transform.position, lastPosition) / Time.deltaTime : 0f;
        visualMoveSpeed = Mathf.Lerp(visualMoveSpeed, frameSpeed, Time.deltaTime * 12f);
        float moveAmount = Mathf.Clamp01(visualMoveSpeed / Mathf.Max(speed, 0.01f));

        if (anim.runtimeAnimatorController != null)
        {
            anim.SetFloat("Speed", moveAmount);
        }

        if (!animateProceduralWalk || visualRoot == null) return;

        walkCycle += visualMoveSpeed * walkCycleSpeed * Time.deltaTime;
        float stride = Mathf.Sin(walkCycle);
        float counterStride = Mathf.Sin(walkCycle + Mathf.PI);
        float bob = Mathf.Abs(stride) * bodyBobHeight * moveAmount;
        float lean = Mathf.Sin(walkCycle * 0.5f) * bodyLeanAngle * moveAmount;
        float limbSwing = limbSwingAngle * moveAmount;

        visualRoot.localPosition = visualRootStartLocalPosition + Vector3.up * bob;
        ApplyBoneRotation(hips, hipsStartRotation, new Vector3(lean, 0f, lean * 0.35f));
        ApplyBoneRotation(leftUpperLeg, leftUpperLegStartRotation, new Vector3(stride * limbSwing, 0f, 0f));
        ApplyBoneRotation(rightUpperLeg, rightUpperLegStartRotation, new Vector3(counterStride * limbSwing, 0f, 0f));
        ApplyBoneRotation(leftUpperArm, leftUpperArmStartRotation, new Vector3(counterStride * limbSwing * 0.8f, 0f, 0f));
        ApplyBoneRotation(rightUpperArm, rightUpperArmStartRotation, new Vector3(stride * limbSwing * 0.8f, 0f, 0f));
    }

    void ApplyBoneRotation(Transform bone, Quaternion startRotation, Vector3 eulerOffset)
    {
        if (bone == null) return;
        bone.localRotation = startRotation * Quaternion.Euler(eulerOffset);
    }

    System.Collections.IEnumerator TraverseOffMeshLink()
    {
        traversingLink = true;
        OffMeshLinkData link = agent.currentOffMeshLinkData;
        Vector3 start = transform.position;
        Vector3 end = link.endPos;
        float duration = Mathf.Max(0.05f, jumpDuration);
        float elapsed = 0f;

        while (elapsed < duration && agent != null && agent.enabled)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = position;

            Vector3 facing = end - start;
            FaceDirection(facing);
            yield return null;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.CompleteOffMeshLink();
        }

        traversingLink = false;
    }

    System.Collections.IEnumerator SinkAndDestroy()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 1.5f;
        while (t < deathDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / deathDuration);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f);
            yield return null;
        }
        Destroy(gameObject);
    }
}
