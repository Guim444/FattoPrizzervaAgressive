using UnityEngine;
using UnityEngine.SceneManagement;

public class ReloadScenes : MonoBehaviour
{
    [SerializeField] private KeyCode reloadKey = KeyCode.R;

    private bool reloading;
    [SerializeField]
    private bool reloadWithInput = true;

    private void Update()
    {
        if (reloadWithInput && Input.GetKeyDown(reloadKey) && !reloading)
        {
            Reload();
        }
    }

    public void Reload()
    {
        if (reloading) return;
        reloading = true;
        Time.timeScale = 1f;

        int buildIndex = gameObject.scene.buildIndex;
        if (buildIndex >= 0)
            SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(gameObject.scene.name, LoadSceneMode.Single);
    }
}
