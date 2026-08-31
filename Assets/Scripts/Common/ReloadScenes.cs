using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScenes : MonoBehaviour
{
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    private bool reloading;

    private HashSet<string> targetScenes;
    private HashSet<string> loadedScenes;

    private void Update()
    {
        if (Input.GetKeyDown(reloadKey) && !reloading)
        {
            StartCoroutine(ReloadAllScenes());
        }
    }

    private IEnumerator ReloadAllScenes()
    {
        reloading = true;

        // Escena donde está este objeto.
        string mainScenePath = gameObject.scene.path;

        // Guardamos qué escena era la activa.
        string activeScenePath = SceneManager.GetActiveScene().path;

        // Guardamos las escenas que había cargadas.
        List<string> scenesToReload = new List<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.isLoaded)
                continue;

            if (string.IsNullOrEmpty(scene.path))
                continue;

            if (!scenesToReload.Contains(scene.path))
            {
                scenesToReload.Add(scene.path);
            }
        }

        targetScenes = new HashSet<string>(scenesToReload);
        loadedScenes = new HashSet<string>();

        // Escuchamos cargas para detectar duplicados.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Este objeto tiene que sobrevivir a LoadSceneMode.Single.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // -----------------------------
        // 1. Recargar MainScene
        // -----------------------------

        yield return SceneManager.LoadSceneAsync(
            mainScenePath,
            LoadSceneMode.Single
        );

        loadedScenes.Add(mainScenePath);

        // Dejamos que Awake / Start de MainScene se ejecuten.
        yield return null;

        // -----------------------------
        // 2. Recuperar las otras escenas
        // -----------------------------

        foreach (string scenePath in scenesToReload)
        {
            if (scenePath == mainScenePath)
                continue;

            // Puede que MainScene ya la haya cargado.
            if (IsSceneLoaded(scenePath))
            {
                loadedScenes.Add(scenePath);
                continue;
            }

            yield return SceneManager.LoadSceneAsync(
                scenePath,
                LoadSceneMode.Additive
            );

            loadedScenes.Add(scenePath);

            // Dejamos ejecutar Awake / Start.
            yield return null;
        }

        // Esperamos un frame adicional por si algún manager
        // intenta cargar una escena automáticamente.
        yield return null;

        // -----------------------------
        // 3. Restaurar escena activa
        // -----------------------------

        Scene activeScene =
            SceneManager.GetSceneByPath(activeScenePath);

        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            SceneManager.SetActiveScene(activeScene);
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Destruimos el reloader antiguo.
        // MainScene ya contiene su copia nueva.
        Destroy(gameObject);
    }

    private bool IsSceneLoaded(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded && scene.path == path)
                return true;
        }

        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!reloading)
            return;

        if (string.IsNullOrEmpty(scene.path))
            return;

        // Solo nos importan las escenas que existían
        // cuando pulsamos R.
        if (!targetScenes.Contains(scene.path))
            return;

        int count = 0;

        // Contamos cuántas instancias existen de esa escena.
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene current = SceneManager.GetSceneAt(i);

            if (current.isLoaded &&
                current.path == scene.path)
            {
                count++;
            }
        }

        // Si ya hay más de una, descargamos la que
        // acaba de aparecer.
        if (count > 1)
        {
            Debug.LogWarning(
                $"[ReloadScenes] Se intentó cargar dos veces " +
                $"{scene.name}. Eliminando duplicado."
            );

            SceneManager.UnloadSceneAsync(scene);
        }
    }
}