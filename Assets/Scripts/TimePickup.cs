using UnityEngine;

public class TimePickup : MonoBehaviour
{
    public enum PickupType { Green, Gold, Broken }

    [Header("Config")]
    public PickupType type = PickupType.Green;
    [Tooltip("Time added on pickup. Negative = remove time.")]
    public float timeAmount = 5f;

    [Header("Polish")]
    public float spinSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;

    Vector3 startPos;

    void Start() { startPos = transform.position; }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        float y = startPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddTime(timeAmount);

        string sign = timeAmount >= 0 ? "+" : "-";
        string label = type == PickupType.Broken ? "Time Lost" : "Time";
        GameManager.Instance.ShowMessage(sign + Mathf.Abs(timeAmount).ToString("0") + " " + label);

        Destroy(gameObject);
    }
}