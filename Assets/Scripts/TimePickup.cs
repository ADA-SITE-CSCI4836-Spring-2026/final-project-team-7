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

    void Start()
    {
        startPos = transform.position;
        CreateTokenVisual();
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

        GameManager.Instance.AddTime(timeAmount);

        string sign = timeAmount >= 0 ? "+" : "-";
        string label = type == PickupType.Broken ? "Time Lost" : "Time";
        string msg = sign + Mathf.Abs(timeAmount).ToString("0") + " " + label;

        Color msgColor;
        switch (type)
        {
            case PickupType.Green:  msgColor = Color.green; break;
            case PickupType.Gold:   msgColor = new Color(1f, 0.84f, 0f); break; // gold
            case PickupType.Broken: msgColor = Color.red; break;
            default:                msgColor = Color.white; break;
        }

        GameManager.Instance.ShowMessage(msg, msgColor);
        Destroy(gameObject);
    }

    void CreateTokenVisual()
    {
        if (transform.Find("TokenVisual") != null) return;

        Renderer ownRenderer = GetComponent<Renderer>();
        if (ownRenderer != null) ownRenderer.enabled = false;

        GameObject visual = new GameObject("TokenVisual");
        visual.transform.SetParent(transform, false);

        Color color = TokenColor();
        PrimitiveType coreShape = type == PickupType.Gold ? PrimitiveType.Sphere : PrimitiveType.Capsule;
        GameObject core = GameObject.CreatePrimitive(coreShape);
        core.name = type == PickupType.Broken ? "BrokenTimeCore" : "TimeCore";
        core.transform.SetParent(visual.transform, false);
        core.transform.localPosition = Vector3.zero;
        core.transform.localScale = type == PickupType.Broken ? new Vector3(0.7f, 0.7f, 0.7f) : new Vector3(0.8f, 0.8f, 0.8f);
        Destroy(core.GetComponent<Collider>());
        ApplyMaterial(core, color);

        if (type == PickupType.Broken)
        {
            AddBar(visual.transform, "Minus", new Vector3(0f, 0.15f, 0f), new Vector3(0.9f, 0.12f, 0.12f), Color.black);
            AddBar(visual.transform, "Crack", new Vector3(0.1f, -0.05f, 0f), new Vector3(0.12f, 0.75f, 0.12f), Color.black, 25f);
        }
        else
        {
            AddBar(visual.transform, "PlusHorizontal", Vector3.zero, new Vector3(1f, 0.14f, 0.14f), Color.white);
            AddBar(visual.transform, "PlusVertical", Vector3.zero, new Vector3(0.14f, 1f, 0.14f), Color.white);
        }

        if (type == PickupType.Gold)
        {
            AddBar(visual.transform, "GoldRing", Vector3.zero, new Vector3(1.25f, 0.06f, 1.25f), new Color(1f, 0.95f, 0.2f), 0f);
        }
    }

    Color TokenColor()
    {
        switch (type)
        {
            case PickupType.Gold: return new Color(1f, 0.84f, 0f);
            case PickupType.Broken: return new Color(0.95f, 0.12f, 0.08f);
            default: return Color.green;
        }
    }

    void AddBar(Transform parent, string name, Vector3 position, Vector3 scale, Color color, float zRotation = 0f)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = name;
        bar.transform.SetParent(parent, false);
        bar.transform.localPosition = position;
        bar.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
        bar.transform.localScale = scale;
        Destroy(bar.GetComponent<Collider>());
        ApplyMaterial(bar, color);
    }

    void ApplyMaterial(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;
        renderer.material = new Material(Shader.Find("Standard"));
        renderer.material.color = color;
    }
}
