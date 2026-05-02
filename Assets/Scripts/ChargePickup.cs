using UnityEngine;

public class ChargePickup : MonoBehaviour
{
    public int chargeAmount = 1;

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

        GameManager.Instance.AddCharges(chargeAmount);
        GameManager.Instance.ShowMessage($"+{chargeAmount} ZAP CHARGE", new Color(0.7f, 0.3f, 1f), 1.5f);
        Destroy(gameObject);
    }
}