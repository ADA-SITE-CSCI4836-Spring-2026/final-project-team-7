using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BossController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Animator anim;
    public CameraFollow cameraFollow;

    [Header("Movement Settings")]
    public float speed = 5f;
    public float rotationSpeed = 15f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float groundProbeDistance = 0.18f;
    public float groundProbeRadius = 0.22f;
    public bool rotateWithCamera = true;

    [Header("Fall Recovery")]
    public float fallResetY = -6f;
    public float safeGroundProbeHeight = 20f;
    public float safeGroundProbeDistance = 60f;
    public float groundOffset = 0.08f;

    // NEW — public toggle so GameManager can freeze the player
    public bool CanMove { get; set; } = true;

    private Vector3 movement;
    private Vector3 velocity;
    private Vector3 spawnPosition;
    private Vector3 lastSafePosition;
    private bool jumpConsumed;
    private readonly int speedHash = Animator.StringToHash("Speed");

    void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (cameraFollow == null && Camera.main != null) cameraFollow = Camera.main.GetComponent<CameraFollow>();
        spawnPosition = transform.position;
        lastSafePosition = spawnPosition;
    }

    void Update()
    {
        RecoverIfFalling();

        bool grounded = IsGroundedReliable();

        // Gravity & Grounding (always runs so player doesn't float when frozen)
        if (grounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            lastSafePosition = transform.position;
            jumpConsumed = false;
        }

        // NEW — if frozen, skip input but still drop with gravity
        if (!CanMove)
        {
            movement = Vector3.zero;
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
            RecoverIfFalling();
            if (anim != null) anim.SetFloat(speedHash, 0f);
            return;
        }

        // Input is read directly from keys so controller axis noise cannot move the player.
        Vector2 input = ReadMoveInput();
        movement = GetCameraRelativeMovement(input);

        if (rotateWithCamera && cameraFollow != null)
        {
            Quaternion cameraYaw = Quaternion.Euler(0f, cameraFollow.CurrentYaw, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, cameraYaw, rotationSpeed * Time.deltaTime);
        }

        // Movement & Rotation
        if (movement.sqrMagnitude >= 0.01f)
        {
            if (!rotateWithCamera)
            {
                float targetAngle = Mathf.Atan2(movement.x, movement.z) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            controller.Move(movement * speed * Time.deltaTime);
        }

        // Jumping: one jump per grounded contact, including while moving across seams.
        if (Input.GetKeyDown(KeyCode.Space) && grounded && !jumpConsumed)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpConsumed = true;
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        RecoverIfFalling();

        // Animator
        if (anim != null)
        {
            anim.SetFloat(speedHash, movement.magnitude);
        }
    }

    bool IsGroundedReliable()
    {
        if (controller.isGrounded) return true;
        if (velocity.y > 0.05f) return false;

        Vector3 origin = transform.position + Vector3.up * 0.18f;
        return Physics.SphereCast(origin, groundProbeRadius, Vector3.down, out RaycastHit hit, groundProbeDistance + 0.18f, ~0, QueryTriggerInteraction.Ignore) &&
               IsSafeGroundHit(hit);
    }

    void RecoverIfFalling()
    {
        if (transform.position.y >= fallResetY) return;

        Vector3 target = lastSafePosition;
        if (!TryFindGroundedPosition(target, out target))
        {
            target = spawnPosition;
            TryFindGroundedPosition(target, out target);
        }

        controller.enabled = false;
        transform.position = target;
        controller.enabled = true;
        velocity = Vector3.zero;
        lastSafePosition = target;
    }

    bool TryFindGroundedPosition(Vector3 around, out Vector3 grounded)
    {
        Vector3 rayStart = around + Vector3.up * safeGroundProbeHeight;
        RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, safeGroundProbeDistance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (RaycastHit hit in hits)
        {
            if (!IsSafeGroundHit(hit)) continue;
            grounded = hit.point + Vector3.up * groundOffset;
            return true;
        }

        grounded = around;
        return false;
    }

    bool IsSafeGroundHit(RaycastHit hit)
    {
        if (hit.collider == null) return false;
        if (hit.collider.CompareTag("Player")) return false;
        if (hit.normal.y < 0.45f) return false;

        string objectName = hit.collider.gameObject.name.ToLowerInvariant();
        Transform root = hit.collider.transform.root;
        string rootName = root != null ? root.name.ToLowerInvariant() : "";

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

    Vector2 ReadMoveInput()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) z -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) z += 1f;

        Vector2 input = new Vector2(x, z);
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    Vector3 GetCameraRelativeMovement(Vector2 input)
    {
        if (input.sqrMagnitude <= 0.001f) return Vector3.zero;

        Vector3 forward = cameraFollow != null ? cameraFollow.FlatForward : transform.forward;
        Vector3 right = cameraFollow != null ? cameraFollow.FlatRight : transform.right;
        Vector3 direction = forward * input.y + right * input.x;
        direction.y = 0f;

        return direction.sqrMagnitude > 1f ? direction.normalized : direction;
    }
}
