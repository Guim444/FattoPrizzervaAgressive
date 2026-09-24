using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScenes : MonoBehaviour
{
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    private bool reloading;

    private void Update()
    {
        if (Input.GetKeyDown(reloadKey) && !reloading)
        {
            reloading = true;

            int buildIndex = gameObject.scene.buildIndex;
            if (buildIndex >= 0)
                SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
            else
                SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
        }
    }
}

