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
    private DashPackTest _dashPack;

    private void Awake()
    {
        _colliderView = gameObject.AddComponent<ColliderDebugView>();
        _dashPack = gameObject.AddComponent<DashPackTest>();
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

    // Barra de vida provisional arriba al centro, solo se ve con el panel abierto
    private void DrawBossLifeBar()
    {
        if (_hud == null) return;

        BossFightPhaseController phases = _hud.GetComponent<BossFightPhaseController>();
        if (phases == null || !phases.enabled || phases.Phase < 2 || phases.Boss == null) return;

        RioTutteEnemy boss = phases.Boss;
        float ratio = Mathf.Clamp01(boss.Life / Mathf.Max(boss.maxLife, 0.01f));
        Rect bar = new Rect(Screen.width * 0.5f - 200f, 12f, 400f, 22f);

        Color previous = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * ratio, bar.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(bar.center.x - 1f, bar.y, 2f, bar.height), Texture2D.whiteTexture);
        GUI.color = previous;

        GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(bar, $"RioTutte  {boss.Life:0.#}/{boss.maxLife:0.#}  Fase {Mathf.Min(phases.Phase, 3)}", style);
    }

    private void OnGUI()
    {
        if (!_visible) return;

        DrawBossLifeBar();

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
            GUILayout.Label("Packs de prueba");
            _dashPack.useObstacles = GUILayout.Toggle(_dashPack.useObstacles, "Cajas de obstáculo");
            SmashButtonChallenge.simplified = GUILayout.Toggle(SmashButtonChallenge.simplified, "Smash the Button simple");
            if (GUILayout.Button("Pack 1: Dash e interrupción"))
                _dashPack.Begin(_hud, DashPackTest.PackMode.Dash);
            if (GUILayout.Button("Pack 2: Dash + agarre"))
                _dashPack.Begin(_hud, DashPackTest.PackMode.DashAndGrab);
            if (GUILayout.Button("Pack 3: empujar a objetos"))
                _dashPack.Begin(_hud, DashPackTest.PackMode.PushObjects);
            if (_dashPack.Running && GUILayout.Button("Parar pack"))
                _dashPack.Stop();

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
