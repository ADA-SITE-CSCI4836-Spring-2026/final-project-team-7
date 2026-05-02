using UnityEngine;
using System.Collections;

public class FreezePickup : MonoBehaviour
{
    public float freezeDuration = 3f;
    public string message = "TIME FROZEN!";

    [Header("Polish")]
    public float spinSpeed = 90f;
    public float bobAmplitude = 0.2f;
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

        GameManager.Instance.StartCoroutine(FreezeRoutine(freezeDuration));
        GameManager.Instance.ShowMessage(message, Color.cyan, freezeDuration);
        Destroy(gameObject);
    }

    static IEnumerator FreezeRoutine(float duration)
    {
        var gm = GameManager.Instance;
        gm.SetTimeFrozen(true);
        yield return new WaitForSeconds(duration);
        if (gm != null) gm.SetTimeFrozen(false);
    }
}