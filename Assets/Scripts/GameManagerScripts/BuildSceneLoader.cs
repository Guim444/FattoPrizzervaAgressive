using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads the scenes that are open together in the Editor when the game starts.
/// MainScene is kept as the bootstrap scene and the gameplay scenes are loaded additively.
/// </summary>
public sealed class BuildSceneLoader : MonoBehaviour
{
    [SerializeField] private TMP_Text loadingText;

    [SerializeField] private string[] additiveScenePaths =
    {       
        "Assets/Scenes/LightingScene.unity",
        "Assets/Scenes/DialogueScene.unity",
        "Assets/Scenes/GameplayScene.unity",
    };

    [SerializeField] private string activeSceneName = "MainScene";

    private static readonly string[] LoadingStates = { "Loading", "Loading.", "Loading..", "Loading..." };

    private IEnumerator Start()
    {
        Coroutine animationCoroutine = null;

        if (loadingText != null)
        {
            loadingText.gameObject.SetActive(true);
            animationCoroutine = StartCoroutine(AnimateLoadingText());
        }

        foreach (string scenePath in additiveScenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                continue;

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid() && scene.isLoaded)
                continue;

            int buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (buildIndex < 0)
            {
                Debug.LogError($"[{nameof(BuildSceneLoader)}] La escena '{scenePath}' no está incluida en Build Settings.", this);
                continue;
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Additive);

            if (loadOperation == null)
            {
                Debug.LogError($"[{nameof(BuildSceneLoader)}] No se pudo iniciar la carga de '{scenePath}'.", this);
                continue;
            }

            yield return loadOperation;
        }

        Scene activeScene = SceneManager.GetSceneByName(activeSceneName);

        if (activeScene.IsValid() && activeScene.isLoaded)
            SceneManager.SetActiveScene(activeScene);
        else
            Debug.LogError($"[{nameof(BuildSceneLoader)}] No se encontró la escena activa '{activeSceneName}'.", this);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        if (loadingText != null)
            loadingText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateLoadingText()
    {
        int index = 0;

        while (true)
        {
            if (loadingText != null)
            {
                loadingText.text = LoadingStates[index];
                index = (index + 1) % LoadingStates.Length;
            }

            yield return new WaitForSecondsRealtime(0.05f);
        }
    }
}
