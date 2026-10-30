using System.Collections.Generic;
using UnityEngine;

// Pruebas del combate contra RioTutte por parejas ataque + forma de dañarlo
// Pack 1: Dash, meta interrumpir dos dashes con Thrust
// Pack 2: Dash y agarre, hay que levantarse con Smash the Button
// Pack 3: boss quieto, meta empujarlo contra los objetos del mapa
public sealed class DashPackTest : MonoBehaviour
{
    public enum PackMode { Dash, DashAndGrab, PushObjects }

    public int dashesToInterrupt = 2;
    public bool useObstacles = true;

    private HUDManager _hud;
    private RioTutteEnemy _boss;
    private RioTutteDashAttack _dash;
    private RioTutteGrabAttack _grab;
    private PlayerController _player;
    private BossFightPhaseController _phases;
    private readonly List<MapInteractable> _objects = new List<MapInteractable>();

    private PackMode _mode;
    private bool _running;
    private int _interrupted;
    private int _grabsLanded;
    private int _grabsEscaped;
    private int _objectsUsed;
    private string _lastResult = "-";
    private float _messageUntil;

    public bool Running => _running;

    public void Begin(HUDManager hud, PackMode mode)
    {
        Stop();
        _hud = hud;
        _mode = mode;

        // Entra en la iglesia en modo libre y deja al boss listo en su cuadrado
        _hud.OnClickFreeMove();
        _hud.StartBattleBoss0();

        _boss = _hud.EnemyRio != null ? _hud.EnemyRio.GetComponent<RioTutteEnemy>() : null;
        _player = FindFirstObjectByType<PlayerController>();
        _phases = _hud.GetComponent<BossFightPhaseController>();
        if (_boss == null || _player == null || _phases == null || _phases.Area == null)
        {
            Debug.LogWarning("[DashPackTest] Falta el boss, el jugador o el cuadrado de combate");
            return;
        }

        // Las fases del boss no se usan en estas pruebas
        _phases.enabled = false;

        _dash = _boss.Dash;
        if (_dash == null) _dash = _boss.gameObject.AddComponent<RioTutteDashAttack>();
        _dash.DashFinished += OnDashFinished;

        _grab = _boss.Grab;
        if (_grab == null) _grab = _boss.gameObject.AddComponent<RioTutteGrabAttack>();
        _grab.GrabFinished += OnGrabFinished;

        bool bossAttacks = mode != PackMode.PushObjects;
        _boss.attacksEnabled = true;
        _boss.lockLife = true;
        _boss.dashPackMode = bossAttacks;
        _boss.grabEnabled = mode == PackMode.DashAndGrab;
        _boss.facePlayerByZ = true;
        _boss.activeAI = bossAttacks;

        PlacePlayer();
        if (useObstacles || mode == PackMode.PushObjects) SpawnObstacles();

        _interrupted = 0;
        _grabsLanded = 0;
        _grabsEscaped = 0;
        _objectsUsed = 0;
        _lastResult = "-";
        _running = true;
    }

    public void Stop()
    {
        if (_dash != null)
        {
            _dash.DashFinished -= OnDashFinished;
            _dash.Cancel();
        }

        if (_grab != null)
        {
            _grab.GrabFinished -= OnGrabFinished;
            _grab.Cancel();
        }

        if (_boss != null)
        {
            _boss.dashPackMode = false;
            _boss.grabEnabled = false;
        }

        foreach (MapInteractable item in _objects)
        {
            if (item == null) continue;
            item.Triggered -= OnObjectTriggered;
            Destroy(item.gameObject);
        }
        _objects.Clear();

        _running = false;
    }

    // El jugador empieza en el lado contrario del cuadrado, a un cuarto desde el Front
    private void PlacePlayer()
    {
        TutorialRingManager area = _phases.Area;
        float frontZ = area.frontBound.position.z;
        float backZ = area.backBound.position.z;
        float side = Mathf.Sign(backZ - frontZ);

        Vector3 position = _player.transform.position;
        position.x = (area.leftBound.position.x + area.rightBound.position.x) * 0.5f;
        position.z = frontZ + side * Mathf.Abs(backZ - frontZ) * 0.25f;

        _player.cc.enabled = false;
        _player.transform.position = position;
        _player.cc.enabled = true;
    }

    private void SpawnObstacles()
    {
        foreach (MapInteractable item in MapInteractable.SpawnBoxes(_phases.Area, _boss))
        {
            item.Triggered += OnObjectTriggered;
            _objects.Add(item);
        }
    }

    private void OnDashFinished(DashResult result)
    {
        _lastResult = result.ToString();

        if (_mode != PackMode.Dash || result != DashResult.Interrupted) return;

        _interrupted++;
        if (_interrupted >= dashesToInterrupt)
            FinishPack();
    }

    private void OnGrabFinished(bool escaped)
    {
        _grabsLanded++;
        if (escaped) _grabsEscaped++;
        _lastResult = escaped ? "Te levantaste" : "Te levantaste tarde";
    }

    private void OnObjectTriggered(MapInteractable item)
    {
        _objectsUsed++;
        _lastResult = $"Boss contra {item.name}";

        if (_mode == PackMode.PushObjects && _objectsUsed >= _objects.Count)
            FinishPack();
    }

    // Meta cumplida, el boss deja de atacar y sale el mensaje
    private void FinishPack()
    {
        _messageUntil = Time.unscaledTime + 3f;
        _boss.dashPackMode = false;
        _boss.activeAI = false;
    }

    private void OnGUI()
    {
        if (!_running) return;

        PlayerCombat combat = _player.combat;
        bool running = _player.currentState == State.Running || _player.currentState == State.PunchRunning;

        GUILayout.BeginArea(new Rect(10f, 10f, 340f, 260f), GUI.skin.box);
        GUILayout.Label($"PACK: {_mode}");
        GUILayout.Label($"Thrust: {(running ? combat.CurrentThrust : 0)}   Vida: {combat.HP}/{combat.maxHP}");
        GUILayout.Label($"Ultimo puñetazo: Thrust {combat.LastPunchThrust} hace {Time.time - combat.LastPunchTime:F1} s");

        if (_mode == PackMode.Dash)
            GUILayout.Label($"Dashes interrumpidos: {_interrupted}/{dashesToInterrupt}");
        if (_mode == PackMode.DashAndGrab)
            GUILayout.Label($"Agarres: {_grabsLanded}   Levantado a tiempo: {_grabsEscaped}");
        if (_mode == PackMode.PushObjects)
            GUILayout.Label($"Objetos usados: {_objectsUsed}/{_objects.Count}");

        GUILayout.Label($"Ultimo resultado: {_lastResult}");

        if (_mode != PackMode.PushObjects)
        {
            string state = _dash.IsChanneling ? $"Canalizando {_dash.ChannelRemaining:F1} s"
                : _dash.IsDashing ? "Dash"
                : _grab.IsRunning ? "Corriendo a agarrarte"
                : _grab.IsHoldingPlayer ? "Te tiene agarrado"
                : $"Pausa {_dash.CooldownRemaining:F1} s";
            GUILayout.Label($"Boss: {state}");
        }

        GUILayout.EndArea();

        if (Time.unscaledTime < _messageUntil)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, 0f, Screen.width, Screen.height * 0.4f), "Finish Phase", style);
        }
    }

    private void OnDestroy()
    {
        Stop();
    }
}
