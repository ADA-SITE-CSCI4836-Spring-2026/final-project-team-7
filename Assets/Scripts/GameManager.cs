using UnityEngine;
using UnityEngine.SceneManagement;
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
    public GameObject gameOverPanel;
    public GameObject winPanel;
    public GameObject pausePanel;
    public TMP_Text winFinalTimeText;

    [Header("Player")]
    public BossController player;

    public float CurrentTime { get; private set; }
    public float currentDrainRate { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsPaused { get; private set; }
    public bool TimeFrozen { get; private set; }

    float messageTimer;
    int defaultTimerFontSize;
    Color defaultTimerColor;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        CurrentTime = startingTime;
        currentDrainRate = normalDrainRate;

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
    }

    void Update()
    {
        if (IsGameOver || IsWon || IsPaused) return;

        if (!TimeFrozen)
        {
            CurrentTime -= currentDrainRate * Time.deltaTime;
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

    void LateUpdate()
    {
        if (IsGameOver || IsWon) return;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) ResumeGame();
            else PauseGame();
        }
    }

    void UpdateTimerText()
    {
        if (!timerText) return;
        timerText.text = Mathf.CeilToInt(CurrentTime).ToString();

        if (CurrentTime <= 10f && !IsGameOver && !IsWon)
        {
            timerText.color = Color.red;
            timerText.fontSize = defaultTimerFontSize + 24;
        }
        else
        {
            timerText.color = defaultTimerColor;
            timerText.fontSize = defaultTimerFontSize;
        }
    }

    public void AddTime(float amount)
    {
        if (IsGameOver || IsWon) return;
        CurrentTime += amount;
        if (CurrentTime < 0f) CurrentTime = 0f;
        UpdateTimerText();
    }

    public void ShowMessage(string msg, float duration = 2f)
    {
        if (!messageText) return;
        messageText.text = msg;
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

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}