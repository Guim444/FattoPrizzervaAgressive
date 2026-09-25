using UnityEngine;
using Cinemachine;

/// <summary>
/// Inicializador automático para la escena de pruebas de combate.
/// Configura cámara, límites y ring automáticamente en Start().
/// </summary>
public class CombatTestBootstrap : MonoBehaviour
{
    [Header("Luchadores")]
    [SerializeField] private PlayerController player;
    [SerializeField] private RioTutteEnemy combatEnemy;

    [Header("Ring & Límites")]
    [SerializeField] private TutorialRingManager ringManager;
    [SerializeField] private PlayerBoundaryClamp playerBoundary;

    [Header("Cámara")]
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineVirtualCamera combatVirtualCamera;
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Posiciones de Inicio en el Ring")]
    [Tooltip("Si ya colocaste a los personajes donde quieres en la escena, desmarca esto.")]
    [SerializeField] private bool overridePositions = false;
    [SerializeField] private Vector3 playerCombatPosition = new Vector3(0f, 1.26f, -3f);
    [SerializeField] private Vector3 enemyCombatPosition  = new Vector3(0f, 2.43f, 1f);

    private void Awake()
    {
        // Aseguramos que el tiempo corre normal
        Time.timeScale = 1f;

        // Auto-detección si no están asignados en el Inspector
        if (player == null) 
            player = FindFirstObjectByType<PlayerController>();

        if (combatEnemy == null) 
            combatEnemy = FindFirstObjectByType<RioTutteEnemy>();

        if (ringManager == null) 
            ringManager = FindFirstObjectByType<TutorialRingManager>();

        if (cinemachineBrain == null) 
            cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();

        if (playerBoundary == null) 
            playerBoundary = FindFirstObjectByType<PlayerBoundaryClamp>();

        if (cameraMovement == null) 
            cameraMovement = FindFirstObjectByType<CameraMovement>();
    }

    private void Start()
    {
        // 1. Desactivar límites de la nieve si quedaba alguno
        if (playerBoundary != null) 
            playerBoundary.enabled = false;

        // 2. Reposicionar solo si overridePositions está marcado (por defecto false para respetar tu escena)
        if (overridePositions)
        {
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.transform.position = playerCombatPosition;
                Physics.SyncTransforms();
                if (cc != null) cc.enabled = true;
            }

            if (combatEnemy != null)
            {
                var ecc = combatEnemy.GetComponent<CharacterController>();
                if (ecc != null) ecc.enabled = false;
                combatEnemy.transform.position = enemyCombatPosition;
                Physics.SyncTransforms();
                if (ecc != null) ecc.enabled = true;
            }
        }

        // 3. Configurar la cámara para el combate
        if (cameraMovement != null) 
            cameraMovement.enabled = false;

        if (cinemachineBrain != null) 
            cinemachineBrain.enabled = true;

        if (combatVirtualCamera != null) 
            combatVirtualCamera.gameObject.SetActive(true);

        // 4. Iniciar el gestor del ring con las posiciones actuales
        if (ringManager != null)
        {
            ringManager.RefreshStartPositions();
            ringManager.enabled = true;
        }

        Debug.Log("[CombatTestBootstrap] Escena de combate inicializada correctamente.");
    }
}
