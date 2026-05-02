using UnityEngine;

public class DrainZone : MonoBehaviour
{
    [Tooltip("Drain rate while standing inside. Default normal is 1.")]
    public float drainRateInside = 3f;

    [Tooltip("Message shown on entry.")]
    public string enterMessage = "DANGER: TIME DRAINING FAST!";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.SetDrainRate(drainRateInside);
        if (!string.IsNullOrEmpty(enterMessage))
            GameManager.Instance.ShowMessage(enterMessage, Color.red, 1.5f);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        GameManager.Instance.ResetDrainRate();
    }
}