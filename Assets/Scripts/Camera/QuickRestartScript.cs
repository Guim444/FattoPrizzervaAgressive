using UnityEngine;

public class RestartManager : MonoBehaviour
{
    [Header("Configuració")]
    // L'objecte que marca el punt inicial real
    public Transform puntInicialReal;

    // La teva Càmera Principal
    public GameObject objecteCamera;

    private DoorSceneCameraController scriptCamera;

    private void Awake()
    {
        if (objecteCamera != null)
        {
            scriptCamera = objecteCamera.GetComponent<DoorSceneCameraController>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (objecteCamera == null || puntInicialReal == null)
            {
                Debug.LogError("⚠️ Falta assignar la Càmera o el Punt Inicial a l'Inspector!");
                return;
            }

            // 1. Apaguem l'OBJECTE de la càmera completament.
            // Això mata TOTES les coroutines i el sacseig (shake) a l'instant per nassos.
            objecteCamera.SetActive(false);

            // 2. Ara que està tot mort i congelat, la col·loquem al seu lloc real
            objecteCamera.transform.position = puntInicialReal.position;
            objecteCamera.transform.rotation = puntInicialReal.rotation;

            // 3. Tornem a encendre l'objecte de la càmera
            objecteCamera.SetActive(true);

            // 4. Si el script té la funció de netejar el negre, la cridem
            if (scriptCamera != null)
            {
                scriptCamera.ResetFade();
            }

            Debug.Log("🔄 Objecte reiniciat de zero i coroutines eliminades!");
        }
    }
}
