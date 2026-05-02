using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Third Person Follow")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.55f, 0f);
    [SerializeField] private float distance = 4.8f;
    [SerializeField] private float minDistance = 2.3f;
    [SerializeField] private float maxDistance = 7.5f;
    [SerializeField] private float followSmoothTime = 0.04f;
    [SerializeField] private float rotationSmooth = 25f;

    [Header("Manual Orbit")]
    [SerializeField] private float mouseSensitivity = 2.4f;
    [SerializeField] private float keyboardRotateSpeed = 120f;
    [SerializeField] private float zoomSensitivity = 2f;
    [SerializeField] private float minPitch = -8f;
    [SerializeField] private float maxPitch = 48f;
    [SerializeField] private bool requireRightMouseButton = false;
    [SerializeField] private bool lockCursorDuringGameplay = true;

    [Header("Collision")]
    [SerializeField] private LayerMask collisionMask = ~0;
    [SerializeField] private float collisionRadius = 0.16f;
    [SerializeField] private float collisionBuffer = 0.2f;

    private float yaw;
    private float pitch = 12f;
    private Vector3 followVelocity;

    public float CurrentYaw => yaw;

    public Vector3 FlatForward
    {
        get
        {
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            forward.y = 0f;
            return forward.normalized;
        }
    }

    public Vector3 FlatRight
    {
        get
        {
            Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            right.y = 0f;
            return right.normalized;
        }
    }

    private void Start()
    {
        FindPlayerIfNeeded();
        if (player != null) yaw = player.eulerAngles.y;
    }

    private void Update()
    {
        FindPlayerIfNeeded();
        if (player == null) return;

        UpdateCursorState();
        UpdateOrbitInput();
    }

    private void LateUpdate()
    {
        FindPlayerIfNeeded();
        if (player == null) return;

        MoveCamera();
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) player = playerObject.transform;
    }

    private void UpdateOrbitInput()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsPaused) return;

        bool canUseMouse = !requireRightMouseButton || Input.GetMouseButton(1);
        if (canUseMouse)
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        }

        if (Input.GetKey(KeyCode.Q)) yaw -= keyboardRotateSpeed * Time.unscaledDeltaTime;
        if (Input.GetKey(KeyCode.E)) yaw += keyboardRotateSpeed * Time.unscaledDeltaTime;

        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance = Mathf.Clamp(distance - scroll * zoomSensitivity, minDistance, maxDistance);
        }

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void UpdateCursorState()
    {
        if (!lockCursorDuringGameplay) return;

        bool gameplayActive = GameManager.Instance == null ||
                              (!GameManager.Instance.IsPaused &&
                               !GameManager.Instance.IsGameOver &&
                               !GameManager.Instance.IsWon);

        Cursor.lockState = gameplayActive ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !gameplayActive;
    }

    private void MoveCamera()
    {
        Vector3 pivot = player.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot - orbitRotation * Vector3.forward * distance;

        if (Physics.SphereCast(pivot, collisionRadius, desiredPosition - pivot, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - collisionBuffer, minDistance);
            desiredPosition = pivot - orbitRotation * Vector3.forward * safeDistance;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);

        Quaternion lookRotation = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSmooth * Time.unscaledDeltaTime);
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.5f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        maxPitch = Mathf.Max(minPitch, maxPitch);
    }
}
