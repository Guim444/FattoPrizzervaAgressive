using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls a timer in GameplayScene that counts up until reaching the duration,
/// then opens a mini HUD and pauses the game.
/// Provides public methods to resume the game and to restart via ReloadScenes.
/// </summary>
public class GameplayTimerHUD : MonoBehaviour
{
    [Header("HUD")]
    [Tooltip("El GameObject del mini HUD que se activará.")]
    [SerializeField] private GameObject miniHud;

    [Header("Timer Settings")]
    [Tooltip("Duración en segundos del temporizador.")]
    [SerializeField] private float duration = 60f;

    private float currentTime;
    private bool isRunning;

    private void Awake()
    {
        if (miniHud != null)
            miniHud.SetActive(false);
    }

    private void Update()
    {
        if (!isRunning)
            return;

        currentTime += Time.deltaTime;

        if (currentTime >= duration)
        {
            currentTime = duration;
            isRunning = false;
            OpenHUD();
        }
    }

    /// <summary>
    /// Inicia el temporizador desde cero (count up).
    /// </summary>
    public void StartTimer()
    {
        currentTime = 0f;
        isRunning = true;
    }

    /// <summary>
    /// Pausa o detiene el temporizador.
    /// </summary>
    public void StopTimer()
    {
        isRunning = false;
    }

    /// <summary>
    /// Activa el mini HUD y pausa el tiempo del juego.
    /// </summary>
    public void OpenHUD()
    {
        if (miniHud != null)
            miniHud.SetActive(true);

        Time.timeScale = 0f;
    }

    /// <summary>
    /// Cierra el mini HUD y despausa el juego.
    /// </summary>
    public void CloseHUD()
    {
        if (miniHud != null)
            miniHud.SetActive(false);

        Time.timeScale = 1f;
    }

    /// <summary>
    /// Despausa el juego, busca el componente ReloadScenes en MainScene y solicita el reinicio.
    /// Si no lo encuentra, carga MainScene directamente como respaldo.
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        ReloadScenes reloader = Object.FindAnyObjectByType<ReloadScenes>(FindObjectsInactive.Include);
        if (reloader != null)
        {
            reloader.Reload();
        }
        else
        {
            SceneManager.LoadScene("MainScene", LoadSceneMode.Single);
        }
    }
}
