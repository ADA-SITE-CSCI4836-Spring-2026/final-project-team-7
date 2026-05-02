using UnityEngine;

public class FlagPickup : MonoBehaviour
{
    public int flagNumber = 1;
    public float spinSpeed = 55f;
    public float bobAmplitude = 0.12f;
    public float bobFrequency = 2f;

    Vector3 startPosition;
    TextMesh label;
    bool collected;

    void Start()
    {
        startPosition = transform.position;
        label = GetComponentInChildren<TextMesh>();
    }

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);
        float y = startPosition.y + Mathf.Sin(Time.time * bobFrequency + flagNumber) * bobAmplitude;
        transform.position = new Vector3(startPosition.x, y, startPosition.z);

        if (label != null && Camera.main != null)
        {
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - Camera.main.transform.position, Vector3.up);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || !other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        collected = true;
        GameManager.Instance.CollectFlag(this);
        Destroy(gameObject);
    }
}
