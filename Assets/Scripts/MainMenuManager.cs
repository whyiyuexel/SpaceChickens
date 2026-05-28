using UnityEngine;

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public GameObject instructionsPanel;
    public GameObject mainMenuPanel;

    public void StartGame()
    {
        SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
    }

    public void ShowInstructions()
    {
        mainMenuPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void HideInstructions()
    {
        instructionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
}