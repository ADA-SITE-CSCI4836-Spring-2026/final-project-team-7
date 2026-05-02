using UnityEngine;

public class ExitZone : MonoBehaviour
{
    [Tooltip("Minimum seconds required to escape.")]
    public float requiredTime = 10f;
    public string failMessage = "Need at least 10 seconds to escape!";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.CurrentTime >= requiredTime)
        {
            GameManager.Instance.TriggerWin();
        }
        else
        {
            GameManager.Instance.ShowMessage(failMessage, 2f);
        }
    }
}