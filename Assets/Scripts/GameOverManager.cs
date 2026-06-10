using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    public GameObject youDiedScreen;
    public GameObject youWonScreen;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (youDiedScreen != null)
            youDiedScreen.SetActive(false);
        if (youWonScreen != null)
            youWonScreen.SetActive(false);
    }

    public void ShowGameOver()
    {
        if (youDiedScreen != null)
            youDiedScreen.SetActive(true);

        Time.timeScale = 0f;
    }

    public void ShowWinScreen()
    {
        if (youWonScreen != null)
            youWonScreen.SetActive(true);

        Time.timeScale = 0f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}