using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Muestra toda la información de combate en dos componentes TextMeshProUGUI:
/// 1. eventsText: Registro cronológico de eventos puntuales (quién golpeó a quién, empuje, daño, etc.)
/// 2. statusText: Estado continuo en tiempo real (fase del boss, golpes restantes, HP, endurance, stamina).
/// 
/// Utiliza DELEGADOS Y EVENTOS ESTÁTICOS para que cualquier script de combate pueda emitir
/// eventos sin generar dependencias rígidas ni requerir referencias directas a este componente.
/// </summary>
public class CombatDebugHUD : MonoBehaviour
{

    public delegate void CombatHitHandler(string attacker, string victim, string attackType, float knockbackForce, int damage);
    public delegate void CombatPhaseHandler(string characterName, int newPhase, string details);
    public delegate void CombatCustomLogHandler(string message);

    /// <summary>Disparado cuando ocurre un impacto, choque o ataque con daño/empuje.</summary>
    public static event CombatHitHandler OnCombatHit;

    /// <summary>Disparado cuando un personaje cambia de fase.</summary>
    public static event CombatPhaseHandler OnPhaseChanged;

    /// <summary>Disparado para cualquier evento puntual personalizado (RingOut, penalización, etc.).</summary>
    public static event CombatCustomLogHandler OnCombatCustomLog;

    public static void ReportHit(string attacker, string victim, string attackType, float knockbackForce, int damage)
    {
        OnCombatHit?.Invoke(attacker, victim, attackType, knockbackForce, damage);
    }

    public static void ReportPhase(string characterName, int newPhase, string details = "")
    {
        OnPhaseChanged?.Invoke(characterName, newPhase, details);
    }

    public static void ReportEvent(string message)
    {
        OnCombatCustomLog?.Invoke(message);
    }

    [Header("Referencias UI (TextMeshPro)")]
    [Tooltip("Texto para registrar eventos puntuales en orden cronológico.")]
    [SerializeField] private TextMeshProUGUI eventsText;

    [Tooltip("Texto para mostrar estado en tiempo real (fase, HP, stamina, etc.).")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Configuración del Log de Eventos")]
    [Tooltip("Máximo número de líneas de eventos visibles simultáneamente.")]
    [SerializeField] private int maxEventLines = 8;

    [Tooltip("Frecuencia de refresco del texto de estado (en segundos).")]
    [SerializeField] private float statusRefreshRate = 0.05f;

    [Header("Referencias de Entidades (Opcional - Se auto-detectan si están vacías)")]
    [SerializeField] private PlayerController player;
    [SerializeField] private RioTutteEnemy rioTutte;
    [SerializeField] private TutorialRingManager ringManager;

    private readonly List<string> _eventLog = new List<string>();
    private readonly StringBuilder _statusSb = new StringBuilder(512);
    private float _nextStatusRefreshTime;

    private void Awake()
    {
        // Auto-detección en escena si no se asignaron en el Inspector
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (rioTutte == null)
            rioTutte = FindFirstObjectByType<RioTutteEnemy>();

        if (ringManager == null)
            ringManager = FindFirstObjectByType<TutorialRingManager>();
    }

    private void OnEnable()
    {
        // Suscripción a los delegados
        OnCombatHit += HandleCombatHit;
        OnPhaseChanged += HandlePhaseChanged;
        OnCombatCustomLog += HandleCustomLog;

        AddLogEntry("<color=#66FF66>● HUD de Combate iniciado</color>");
    }

    private void OnDisable()
    {
        // Cancelación de suscripción para evitar memory leaks
        OnCombatHit -= HandleCombatHit;
        OnPhaseChanged -= HandlePhaseChanged;
        OnCombatCustomLog -= HandleCustomLog;
    }

    private void Update()
    {
        if (Time.time >= _nextStatusRefreshTime)
        {
            _nextStatusRefreshTime = Time.time + statusRefreshRate;
            UpdateStatusDisplay();
        }
    }
    private void HandleCombatHit(string attacker, string victim, string attackType, float knockbackForce, int damage)
    {
        string timeStr = Time.time.ToString("F1");
        string damageStr = damage > 0 ? $"<color=#FF4444>Daño: {damage}</color>" : "<color=#888888>Daño: 0</color>";
        string forceStr = knockbackForce > 0f ? $"<color=#FFCC00>Empuje: {knockbackForce:F1}</color>" : "<color=#888888>Empuje: 0</color>";

        string entry = $"[{timeStr}s] <b>{attacker}</b> → <b>{victim}</b> | <color=#00FFFF>{attackType}</color> | {forceStr} | {damageStr}";
        AddLogEntry(entry);
    }

    private void HandlePhaseChanged(string characterName, int newPhase, string details)
    {
        string timeStr = Time.time.ToString("F1");
        string detailStr = string.IsNullOrEmpty(details) ? "" : $" ({details})";
        string entry = $"<color=#FF00FF>★ [{timeStr}s] {characterName} CAMBIÓ A FASE {newPhase}{detailStr}</color>";
        AddLogEntry(entry);
    }

    private void HandleCustomLog(string message)
    {
        string timeStr = Time.time.ToString("F1");
        string entry = $"<color=#CCCCCC>[{timeStr}s] {message}</color>";
        AddLogEntry(entry);
    }

    private void AddLogEntry(string entry)
    {
        _eventLog.Add(entry);

        if (_eventLog.Count > maxEventLines)
            _eventLog.RemoveAt(0);

        if (eventsText != null)
            eventsText.text = string.Join("\n", _eventLog);
    }

    private void UpdateStatusDisplay()
    {
        if (statusText == null) return;

        _statusSb.Clear();
        _statusSb.AppendLine("<color=#FFDD44><b>ESTADO DEL COMBATE</b></color>");

        // ESTADO DE RIOTUTTE 
        if (rioTutte != null)
        {
            _statusSb.Append("<color=#FF6666><b>[RioTutte]</b></color> ");
            _statusSb.Append($"Fase: <b>{rioTutte.CurrentPhase}</b> | ");

            // Reflejar contador según fase
            if (rioTutte.CurrentPhase == 1)
            {
                _statusSb.Append($"Golpes recibidos: <b>{GetRioTutteRunningHits()}/{rioTutte.runningPunchsToAdvance}</b> | ");
            }

            _statusSb.Append($"Resistencia: {rioTutte.endurance} | ");

            if (rioTutte.groundedTimer > 0f)
                _statusSb.Append($"<color=#FF4444>DERRIBADO ({rioTutte.groundedTimer:F1}s)</color>");
            else if (rioTutte.IsAttacking)
                _statusSb.Append("<color=#FFAA00>Atacando</color>");
            else if (rioTutte.IsMoving)
                _statusSb.Append("<color=#88FF88>Moviéndose</color>");
            else
                _statusSb.Append("<color=#AAAAAA>Idle</color>");

            _statusSb.AppendLine();
        }
        else
        {
            _statusSb.AppendLine("<color=#888888>[RioTutte] No encontrado en escena</color>");
        }

        // ESTADO DEL JUGADOR 
        if (player != null)
        {
            var combat = player.combat;
            var stamina = player.staminaManager;

            _statusSb.Append("<color=#66CCFF><b>[Jugador]</b></color> ");

            if (combat != null)
            {
                string hpColor = combat.HP > 3 ? "#88FF88" : "#FF4444";
                _statusSb.Append($"HP: <color={hpColor}><b>{combat.HP:F0}/{combat.maxHP}</b></color> | ");
                _statusSb.Append($"Endurance: <b>{combat.EffectiveEndurance}</b> | ");
                _statusSb.Append($"Boost: <b>{combat.damageBoost}</b> | ");
            }

            if (stamina != null)
            {
                string stamColor = stamina.isTired ? "#FF4444" : "#FFFF88";
                _statusSb.Append($"Stamina: <color={stamColor}><b>{stamina.currentStamina:F0}%</b></color> | ");
            }

            _statusSb.Append($"Estado: <b>{player.currentState}</b>");
            _statusSb.AppendLine();
        }
        else
        {
            _statusSb.AppendLine("<color=#888888>[Jugador] No encontrado en escena</color>");
        }

        // ESTADO DEL RING / ZONAS 
        if (ringManager != null && player != null)
        {
            float playerZ = player.transform.position.z;
            _statusSb.Append($"<color=#CCCCCC>Pos Z: {playerZ:F1} | </color>");

            if (rioTutte != null)
            {
                float enemyZ = rioTutte.transform.position.z;
                _statusSb.Append($"<color=#CCCCCC>RioTutte Z: {enemyZ:F1}</color>");
            }
            _statusSb.AppendLine();
        }

        statusText.text = _statusSb.ToString();
    }

    private int GetRioTutteRunningHits()
    {
        // Acceso por reflexión ligera para no forzar que _runningPunchHits deba ser público
        if (rioTutte == null) return 0;
        var field = typeof(RioTutteEnemy).GetField("_runningPunchHits", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (int)field.GetValue(rioTutte) : 0;
    }
}
