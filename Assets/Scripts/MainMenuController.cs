using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject instructionsPanel;
    public GameObject creditsPanel;

    public void PlayGame()
    {
        SceneManager.LoadScene("Free_City_Scene_I");
    }

    public void ShowInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(true);
        creditsPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(false);
        creditsPanel.SetActive(true);
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        instructionsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
