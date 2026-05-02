using UnityEngine;

public class ChargePickup : MonoBehaviour
{
    public int chargeAmount = 1;

    [Header("Polish")]
    public float spinSpeed = 90f;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;

    Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
        CreateChargeVisual();
    }

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

    void CreateChargeVisual()
    {
        if (transform.Find("ZapTokenVisual") != null) return;

        Renderer ownRenderer = GetComponent<Renderer>();
        if (ownRenderer != null) ownRenderer.enabled = false;

        GameObject visual = new GameObject("ZapTokenVisual");
        visual.transform.SetParent(transform, false);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "PurpleChargeCore";
        core.transform.SetParent(visual.transform, false);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        Destroy(core.GetComponent<Collider>());
        ApplyMaterial(core, new Color(0.55f, 0.15f, 1f));

        AddBoltSegment(visual.transform, "BoltTop", new Vector3(0.12f, 0.28f, 0f), new Vector3(0.18f, 0.75f, 0.12f), -28f);
        AddBoltSegment(visual.transform, "BoltBottom", new Vector3(-0.1f, -0.32f, 0f), new Vector3(0.18f, 0.75f, 0.12f), 28f);
    }

    void AddBoltSegment(Transform parent, string name, Vector3 position, Vector3 scale, float zRotation)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent, false);
        segment.transform.localPosition = position;
        segment.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        segment.transform.localScale = scale;
        Destroy(segment.GetComponent<Collider>());
        ApplyMaterial(segment, new Color(1f, 0.9f, 0.1f));
    }

    void ApplyMaterial(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
    }
}
