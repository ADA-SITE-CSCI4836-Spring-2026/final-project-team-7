using UnityEngine;

public class DrainZone : MonoBehaviour
{
    public float drainRateInside = 3f;
    public string enterMessage = "DANGER: TIME DRAINING FAST!";

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"DrainZone OnTriggerEnter fired by: {other.name}, tag: {other.tag}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log("  → rejected: not Player tag");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.Log("  → rejected: GameManager.Instance is null");
            return;
        }

        Debug.Log("  → accepted, setting drain rate to " + drainRateInside);
        GameManager.Instance.SetDrainRate(drainRateInside);
        if (!string.IsNullOrEmpty(enterMessage))
            GameManager.Instance.ShowMessage(enterMessage, 1.5f);
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log($"DrainZone OnTriggerExit fired by: {other.name}");
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;
        GameManager.Instance.ResetDrainRate();
    }
}