using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Offset (tweak for better look)")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 5f, -8f);
    [SerializeField] private Vector3 eulerAngles = new Vector3(25f, 0f, 0f);

    private void LateUpdate()
    {
        if (player == null) return;

        transform.position = player.position + positionOffset;
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
}
