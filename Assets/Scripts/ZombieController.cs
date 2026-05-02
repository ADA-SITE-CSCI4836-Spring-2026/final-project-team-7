using UnityEngine;

public class ZombieController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 3f;
    public float stopDistance = 0f;
    public float turnSpeed = 8f;

    [Header("Damage")]
    [Tooltip("Drain rate added to GameManager while touching player.")]
    public float drainRateOnTouch = 5f;

    [Header("Death FX")]
    public float deathDuration = 0.4f;

    Transform player;
    bool isDying;
    bool touchingPlayer;
    Animator anim;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
        anim = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDying || player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > stopDistance)
        {
            Vector3 dir = toPlayer.normalized;
            transform.position += dir * speed * Time.deltaTime;

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Maintain drain while overlapping
        if (touchingPlayer && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterZombieContact(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDying) return;
        if (!other.CompareTag("Player")) return;
        touchingPlayer = true;
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterZombieContact(this);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        touchingPlayer = false;
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterZombieContact(this);
    }

    public void Die()
    {
        if (isDying) return;
        isDying = true;
        touchingPlayer = false;
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterZombieContact(this);

        // Disable collider so we can't be killed twice
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Simple death sink
        StartCoroutine(SinkAndDestroy());
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