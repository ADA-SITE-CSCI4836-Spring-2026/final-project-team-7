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

    [Header("Player")]
    public BossController player;

    [Header("Combat")]
    public int startingCharges = 3;
    public float zapRadius = 4f;

    [Header("Spawner Ref")]
    public ZombieSpawner zombieSpawner;

    public float CurrentTime { get; private set; }
    public float currentDrainRate { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsPaused { get; private set; }
    public bool TimeFrozen { get; private set; }
    public int Charges { get; private set; }

    float messageTimer;
    int defaultTimerFontSize;
    Color defaultTimerColor;
    float lowTimeMessageCooldown;
    bool lowTimeWarned;

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

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f && messageText) messageText.text = "";
        }
    }

    float EffectiveDrainRate()
    {
        // Base drain (set by DrainZone enter/exit) + sum of zombie touches
        float zombieAdd = 0f;
        foreach (var z in contactZombies)
        {
            if (z != null) zombieAdd += z.drainRateOnTouch;
        }
        return currentDrainRate + zombieAdd;
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

    public void RegisterZombieContact(ZombieController z) { contactZombies.Add(z); }
    public void UnregisterZombieContact(ZombieController z) { contactZombies.Remove(z); }

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
        chargesText.text = "ZAP: " + Charges;
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
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void TriggerWin()
    {
        if (IsGameOver || IsWon) return;
        IsWon = true;
        if (player) player.CanMove = false;
        if (winFinalTimeText) winFinalTimeText.text = "Final Time: " + Mathf.CeilToInt(CurrentTime).ToString();
        if (winPanel) winPanel.SetActive(true);
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