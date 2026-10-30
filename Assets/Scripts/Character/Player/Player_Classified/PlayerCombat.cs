using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Single responsibility: manage the player's combat state.
/// Implements IDamageable. Handles HP, death, and the normal punch cooldown.
/// Damage parameters for each hit live here because they are combat data,
/// even though their application occurs within the corresponding states.
/// </summary>
public class PlayerCombat : MonoBehaviour, IDamageable
{
    [Header("Combat: Damage Values")]
    public float basicPunchDamage;

    [Header("HP")]
    [Tooltip("Player's maximum HP. Also the starting HP after each retry.")]
    public int maxHP = 10;

    [Header("Combat: State")]
    public int damageBoost = 0;

    public int endurance = 0;

    // Thrust actual del jugador de 1 a 4, sube con el tiempo que lleva corriendo
    public int CurrentThrust { get; set; }
    public float ThrustUpdatedTime { get; set; } = -100f;

    // Segundos corriendo que hacen falta para llegar a cada Thrust, el primero es el T1
    public float[] thrustStartTimes = { 0f, 1f, 2f, 2.5f };

    // Datos del ultimo puñetazo, el Dash del boss los lee para saber si lo interrumpes
    public int LastPunchThrust { get; private set; }
    public float LastPunchTime { get; private set; } = -100f;
    public Vector3 LastPunchDirection { get; private set; }

    // Devuelve el Thrust que corresponde a ese tiempo corriendo
    public int GetThrustForRunTime(float runTime)
    {
        int thrust = 1;
        for (int i = 1; i < thrustStartTimes.Length; i++)
        {
            if (runTime >= thrustStartTimes[i]) thrust = i + 1;
        }

        return thrust;
    }

    // Se llama al empezar un puñetazo, thrust 0 es un puñetazo normal
    public void RegisterPunch(int thrust, Vector3 direction)
    {
        LastPunchThrust = thrust;
        LastPunchTime = Time.time;
        LastPunchDirection = direction;
    }

    private int? _temporaryEnduranceOverride;

    /// <summary>
    /// Endurance used by combat and knockback calculations.
    /// Temporary state effects never overwrite the serialized/base endurance.
    /// </summary>
    public int EffectiveEndurance => _temporaryEnduranceOverride ?? endurance;

    [Header("Combat: Punch Cooldown")]
    public float normalPunchTimer = 0f;

    [Header("Game Design: Stamina Costs")]
    public float punchStaminaCostPhase0 = 2f;

    public float punchRunningStaminaCostPhase1 = 2f;
    public float punchRunningStaminaCostPhase2 = 4f;
    public float punchRunningStaminaCostPhase3 = 6f;

    [Header("Events")]
    [Tooltip("Fired when HP reaches 0. Connect to TutorialRingManager.ShowRetry() in the Inspector.")]
    public UnityEvent onDied;

    private PlayerController _player;
    public float HP { get; set; }

    /// <summary>Initializes the reference to the orchestrator and sets the initial HP.</summary>
    public void Initialize(PlayerController player)
    {
        _player = player;
        HP = maxHP;
    }

    /// <summary>
    /// Receives damage. Dies if HP reaches 0.
    /// </summary>
    public void TakeDamage(int dmg)
    {
        HP = Mathf.Max(0, HP - dmg);
        Debug.Log($"Player received {dmg} damage. HP: {HP}");

        if (HP <= 0)
        {
            Die();
            return;
        }

        _player.stateMachineController.TransitionTo(State.Knockedback);
    }

    /// <summary>
    /// Executes the player death sequence.
    /// </summary>
    public void Die()
    {
        _player.combatAttackHandler.CancelPreparedAttack();
        ClearEnduranceOverride();
        _player.canMove = false;
        _player.animator.speed = 1;
        _player.animator.SetBool("isDead", true);
        onDied.Invoke();
    }

    public void SetEnduranceOverride(int value)
    {
        _temporaryEnduranceOverride = value;
    }

    public void ClearEnduranceOverride()
    {
        _temporaryEnduranceOverride = null;
    }

    /// <summary>
    /// Decrements the normal punch cooldown timer.
    /// Must be called every Update().
    /// </summary>
    public void UpdatePunchCooldown()
    {
        if (normalPunchTimer > 0)
            normalPunchTimer -= Time.deltaTime;
    }
}
