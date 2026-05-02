using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Time Settings")]
    public float startingTime = 30f;
    public float normalDrainRate = 1f;

    [Header("UI Refs")]
    public TMP_Text timerText;
    public TMP_Text messageText;
    public TMP_Text chargesText;
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject pausePanel;
    public TMP_Text winFinalTimeText;

    [Header("Audio (optional)")]
    public AudioSource warningAudio;
    public AudioClip lowTimeClip;
    public AudioClip playgroundLoopClip;
    public AudioClip zombieNearbyClip;
    public float playgroundVolume = 0.35f;
    public float zombieVolume = 0.8f;
    public float zombieSoundDistance = 13f;
    public float zombieSoundCooldown = 3f;

    [Header("Player")]
    public BossController player;

    [Header("Combat")]
    public int startingCharges = 3;
    public float zapRadius = 4f;

    [Header("Spawner Ref")]
    public ZombieSpawner zombieSpawner;

    [Header("Flags")]
    public int requiredFlags = 3;
    public Vector3[] flagSpawnCandidates =
    {
        new Vector3(14f, 0.2f, 14f),
        new Vector3(56f, 0.2f, 15f),
        new Vector3(56f, 0.2f, 55f)
    };
    public float flagSpawnGroundOffset = 0.08f;
    public Color[] flagColors =
    {
        new Color(1f, 0.16f, 0.12f),
        new Color(0.15f, 0.45f, 1f),
        new Color(1f, 0.84f, 0.1f)
    };
    public Vector3 flagVisualScale = Vector3.one;
    public Transform legacyFinalDestination;

    public float CurrentTime { get; private set; }
    public float currentDrainRate { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsPaused { get; private set; }
    public bool TimeFrozen { get; private set; }
    public int Charges { get; private set; }
    public int FlagsCollected { get; private set; }
    public int ZombieContactCount
    {
        get
        {
            RemoveMissingZombieContacts();
            return contactZombies.Count;
        }
    }

    float messageTimer;
    int defaultTimerFontSize;
    Color defaultTimerColor;
    float lowTimeMessageCooldown;
    float zombieSoundTimer;
    bool lowTimeWarned;
    AudioSource playgroundSource;
    AudioSource zombieSource;

    // Zombies currently touching player (each contributes drainRateOnTouch)
    readonly HashSet<ZombieController> contactZombies = new HashSet<ZombieController>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentTime = startingTime;
        currentDrainRate = normalDrainRate;
        Charges = startingCharges;
        FlagsCollected = 0;
        DisableLegacyExit();
        SpawnFlags();
        SetupGameplayAudio();

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (winPanel) winPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (messageText) messageText.text = "";

        if (timerText)
        {
            defaultTimerFontSize = (int)timerText.fontSize;
            defaultTimerColor = timerText.color;
        }

        UpdateTimerText();
        UpdateChargesText();
    }

    void Update()
    {
        if (IsGameOver || IsWon || IsPaused) return;

        // K to zap
        if (Input.GetKeyDown(KeyCode.K))
        {
            TryZap();
        }

        if (!TimeFrozen)
        {
            CurrentTime -= EffectiveDrainRate() * Time.deltaTime;
            if (CurrentTime <= 0f)
            {
                CurrentTime = 0f;
                TriggerGameOver();
            }
        }

        UpdateTimerText();
        UpdateZombieProximityAudio();

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText) messageText.text = "";
        }
    }

    float EffectiveDrainRate()
    {
        return currentDrainRate + ZombieContactDrainRate();
    }

    float ZombieContactDrainRate()
    {
        RemoveMissingZombieContacts();

        float totalZombieDrain = 0f;
        foreach (var z in contactZombies)
        {
            totalZombieDrain += z.drainRateOnTouch;
        }

        return totalZombieDrain;
    }

    void RemoveMissingZombieContacts()
    {
        contactZombies.RemoveWhere(z => z == null);
    }

    void LateUpdate()
    {
        if (IsGameOver || IsWon) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    void TryZap()
    {
        if (Charges <= 0)
        {
            ShowMessage("NO CHARGES! Find purple cubes.", Color.yellow, 1.5f);
            return;
        }
        if (zombieSpawner == null) return;

        Charges--;
        UpdateChargesText();
        zombieSpawner.KillZombiesInRadius(player.transform.position, zapRadius);
        ShowMessage("ZAP!", new Color(0.7f, 0.3f, 1f), 1f);
    }

    public void RegisterZombieContact(ZombieController z)
    {
        if (z == null || IsGameOver || IsWon) return;
        contactZombies.Add(z);
    }

    public void UnregisterZombieContact(ZombieController z)
    {
        if (z == null) return;
        contactZombies.Remove(z);
    }

    void UpdateTimerText()
    {
        if (!timerText) return;
        timerText.text = Mathf.CeilToInt(CurrentTime).ToString();

        if (CurrentTime <= 10f && !IsGameOver && !IsWon)
        {
            timerText.color = Color.red;
            timerText.fontSize = defaultTimerFontSize + 24;

            lowTimeMessageCooldown -= Time.deltaTime;
            if (lowTimeMessageCooldown <= 0f && messageTimer <= 0f)
            {
                ShowMessage("TIME IS CLOSING IN!", Color.red, 1.5f);
                lowTimeMessageCooldown = 3f;
            }

            if (!lowTimeWarned && warningAudio && lowTimeClip)
            {
                warningAudio.PlayOneShot(lowTimeClip);
                lowTimeWarned = true;
            }
        }
        else
        {
            timerText.color = defaultTimerColor;
            timerText.fontSize = defaultTimerFontSize;
        }
    }

    void UpdateChargesText()
    {
        if (!chargesText) return;
        chargesText.text = "ZAP: " + Charges + "  |  FLAGS: " + FlagsCollected + "/" + requiredFlags;
    }

    public void CollectFlag(FlagPickup flag)
    {
        if (IsGameOver || IsWon) return;
        FlagsCollected++;
        UpdateChargesText();
        ShowMessage("FLAG " + FlagsCollected + "/" + requiredFlags, Color.green, 1.5f);

        if (FlagsCollected >= requiredFlags)
        {
            TriggerWin();
        }
    }

    void SpawnFlags()
    {
        GameObject oldParent = GameObject.Find("RuntimeFlags");
        if (oldParent != null)
        {
            Destroy(oldParent);
        }

        List<Vector3> candidates = new List<Vector3>();
        if (flagSpawnCandidates != null)
        {
            candidates.AddRange(flagSpawnCandidates);
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("GameManager has no flag spawn candidates, so no flags can be spawned.");
            return;
        }

        GameObject parent = new GameObject("RuntimeFlags");
        int count = Mathf.Max(0, requiredFlags);
        for (int i = 0; i < count; i++)
        {
            Vector3 position = SnapToGround(candidates[i % candidates.Count]);
            Color color = flagColors != null && flagColors.Length > 0 ? flagColors[i % flagColors.Length] : Color.red;
            CreateFlag(parent.transform, position, i + 1, color);
        }

        Debug.Log("Spawned " + count + " flags at game start.");
    }

    void CreateFlag(Transform parent, Vector3 position, int number, Color color)
    {
        GameObject root = new GameObject("Flag_" + number);
        root.transform.SetParent(parent, true);
        root.transform.position = position;
        root.transform.localScale = Vector3.Scale(new Vector3(1.45f, 1.45f, 1.45f), flagVisualScale);

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 1.75f;
        trigger.center = new Vector3(0f, 1.8f, 0f);

        FlagPickup pickup = root.AddComponent<FlagPickup>();
        pickup.flagNumber = number;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        pole.transform.localScale = new Vector3(0.11f, 1.75f, 0.11f);
        Destroy(pole.GetComponent<Collider>());

        GameObject cloth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloth.name = "Flag";
        cloth.transform.SetParent(root.transform, false);
        cloth.transform.localPosition = new Vector3(0.8f, 2.95f, 0f);
        cloth.transform.localScale = new Vector3(1.6f, 0.9f, 0.08f);
        Destroy(cloth.GetComponent<Collider>());

        Renderer renderer = cloth.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
        }

        GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beacon.name = "Beacon";
        beacon.transform.SetParent(root.transform, false);
        beacon.transform.localPosition = new Vector3(0f, 4.15f, 0f);
        beacon.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
        Destroy(beacon.GetComponent<Collider>());

        Renderer beaconRenderer = beacon.GetComponent<Renderer>();
        if (beaconRenderer != null)
        {
            beaconRenderer.material = new Material(Shader.Find("Standard"));
            beaconRenderer.material.color = color;
        }

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(root.transform, false);
        labelObject.transform.localPosition = new Vector3(0f, 4.75f, 0f);
        TextMesh label = labelObject.AddComponent<TextMesh>();
        label.text = "FLAG " + number;
        label.fontSize = 42;
        label.characterSize = 0.08f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = Color.white;
    }

    Vector3 SnapToGround(Vector3 position)
    {
        RaycastHit[] hits = Physics.RaycastAll(position + Vector3.up * 30f, Vector3.down, 80f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => b.point.y.CompareTo(a.point.y));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponentInParent<FlagPickup>() != null) continue;
            if (hit.collider.GetComponentInParent<ZombieController>() != null) continue;
            if (hit.collider.CompareTag("Player")) continue;
            return hit.point + Vector3.up * flagSpawnGroundOffset;
        }

        return position;
    }

    void SetupGameplayAudio()
    {
        if (playgroundLoopClip != null)
        {
            playgroundSource = gameObject.AddComponent<AudioSource>();
            playgroundSource.clip = playgroundLoopClip;
            playgroundSource.loop = true;
            playgroundSource.playOnAwake = false;
            playgroundSource.volume = playgroundVolume;
            playgroundSource.spatialBlend = 0f;
            playgroundSource.Play();
        }

        if (zombieNearbyClip != null)
        {
            zombieSource = gameObject.AddComponent<AudioSource>();
            zombieSource.clip = zombieNearbyClip;
            zombieSource.loop = false;
            zombieSource.playOnAwake = false;
            zombieSource.volume = zombieVolume;
            zombieSource.spatialBlend = 0f;
        }
    }

    void UpdateZombieProximityAudio()
    {
        if (zombieSource == null || zombieNearbyClip == null || player == null) return;

        zombieSoundTimer -= Time.deltaTime;
        if (zombieSoundTimer > 0f) return;

        float closestDistance = float.MaxValue;
        foreach (ZombieController zombie in FindObjectsOfType<ZombieController>())
        {
            if (zombie == null || !zombie.isActiveAndEnabled) continue;
            float distance = Vector3.Distance(player.transform.position, zombie.transform.position);
            if (distance < closestDistance) closestDistance = distance;
        }

        if (closestDistance <= zombieSoundDistance)
        {
            float closeness = 1f - Mathf.Clamp01(closestDistance / zombieSoundDistance);
            zombieSource.volume = Mathf.Lerp(zombieVolume * 0.35f, zombieVolume, closeness);
            zombieSource.PlayOneShot(zombieNearbyClip);
            zombieSoundTimer = zombieSoundCooldown;
        }
    }

    void StopGameplayAudio()
    {
        if (playgroundSource != null) playgroundSource.Stop();
        if (zombieSource != null) zombieSource.Stop();
    }

    void DisableLegacyExit()
    {
        if (legacyFinalDestination == null)
        {
            ExitZone exit = FindObjectOfType<ExitZone>();
            if (exit != null) legacyFinalDestination = exit.transform;
        }

        if (legacyFinalDestination != null) legacyFinalDestination.gameObject.SetActive(false);
    }

    public void AddTime(float amount)
    {
        if (IsGameOver || IsWon) return;
        CurrentTime += amount;
        if (CurrentTime < 0f) CurrentTime = 0f;
        UpdateTimerText();
    }

    public void AddCharges(int amount)
    {
        Charges += amount;
        UpdateChargesText();
    }

    public void ShowMessage(string msg, float duration = 2f)
    {
        ShowMessage(msg, Color.white, duration);
    }

    public void ShowMessage(string msg, Color color, float duration = 2f)
    {
        if (!messageText) return;
        messageText.text = msg;
        messageText.color = color;
        messageTimer = duration;
    }

    public void SetDrainRate(float rate) { currentDrainRate = rate; }
    public void ResetDrainRate() { currentDrainRate = normalDrainRate; }
    public void SetTimeFrozen(bool frozen) { TimeFrozen = frozen; }

    void TriggerGameOver()
    {
        IsGameOver = true;
        if (player) player.CanMove = false;
        StopAllActors();
        StopGameplayAudio();
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void TriggerWin()
    {
        if (IsGameOver || IsWon) return;
        IsWon = true;
        if (player) player.CanMove = false;
        StopAllActors();
        StopGameplayAudio();
        if (winFinalTimeText) winFinalTimeText.text = "Final Time: " + Mathf.CeilToInt(CurrentTime).ToString();
        if (winPanel) winPanel.SetActive(true);
    }

    void StopAllActors()
    {
        foreach (ZombieController zombie in FindObjectsOfType<ZombieController>())
        {
            if (zombie != null) zombie.StopNow();
        }
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        if (pausePanel) pausePanel.SetActive(true);
        if (player) player.CanMove = false;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        if (pausePanel) pausePanel.SetActive(false);
        if (player) player.CanMove = true;
    }

    public void RestartGame() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void GoToMainMenu() { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); }
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
