using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Panel de pruebas del combate, se abre con Tab y sale a la derecha
// Solo se crea si la escena abierta se llama TestBoss
public sealed class TestBossPanel : MonoBehaviour
{
    private const string TestSceneName = "TestBoss";
    private const float PanelWidth = 230f;

    private HUDManager _hud;
    private bool _visible;
    private bool _shadowsAttached;
    private ColliderDebugView _colliderView;

    private void Awake()
    {
        _colliderView = gameObject.AddComponent<ColliderDebugView>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Spawn()
    {
        if (!SceneManager.GetSceneByName(TestSceneName).IsValid())
            return;

        new GameObject(nameof(TestBossPanel)).AddComponent<TestBossPanel>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            _visible = !_visible;

        if (_hud == null)
            _hud = FindFirstObjectByType<HUDManager>();

        if (_hud != null && !_shadowsAttached)
            AttachShadows();
    }

    // Pone las sombras al jugador y al boss cuando la escena de gameplay ya esta cargada
    private void AttachShadows()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        RioTutteEnemy[] bosses = FindObjectsByType<RioTutteEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (PlayerController player in players)
            BlobShadow.Attach(player.gameObject, 1.2f);

        foreach (RioTutteEnemy boss in bosses)
            BlobShadow.Attach(boss.gameObject, 1.8f);

        _shadowsAttached = players.Length > 0 && bosses.Length > 0;
    }

    private void OnGUI()
    {
        if (!_visible) return;

        Rect area = new Rect(Screen.width - PanelWidth - 10f, 10f, PanelWidth, Screen.height - 20f);
        GUILayout.BeginArea(area, GUI.skin.box);
        GUILayout.Label("TEST BOSS  (Tab para ocultar)");

        if (_hud == null)
        {
            GUILayout.Label("HUDManager no encontrado, espera a que termine la carga");
        }
        else
        {
            if (GUILayout.Button("Start (con cinemática)")) _hud.OnClickStartHere();
            if (GUILayout.Button("Skip cinem (a la iglesia)")) _hud.OnClickFreeMove();
            if (GUILayout.Button("Start Fight")) _hud.StartBattleBoss0();
            if (GUILayout.Button("Test Battle (skip + pelea)"))
            {
                _hud.OnClickFreeMove();
                _hud.StartBattleBoss0();
            }

            GUILayout.Space(10f);
            _colliderView.show = GUILayout.Toggle(_colliderView.show, "Modo debug (ver colliders)");
            GUILayout.Label("Verde jugador, rojo enemigo, amarillo trigger, celeste mapa");

            GUILayout.Space(10f);
            GUILayout.Label("Iluminación (la 3 es la de la iglesia)");
            GUILayout.BeginHorizontal();
            for (int i = 1; i <= 4; i++)
            {
                if (GUILayout.Button(i.ToString()))
                {
                    LightingStateManager.UnlockKeyboardTransitions();
                    FindFirstObjectByType<LightingStateManager>()?.ApplyNumberTransition(i);
                }
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }
}
