using UnityEngine;

/// <summary>
/// Single responsibility: execute a player attack.
/// Detects the target with Raycast/OverlapSphere, calls KnockbackResolver,
/// applies damage + knockback to the target and recoil to the player.
///
/// Replaces PlayerCombat.ExecutePunch().
/// ClashHandler remains independent (handles continuous physical collision).
///
/// SETUP: add to the same GameObject as PlayerController.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class CombatAttackHandler : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. If null, KnockbackResolver default values are used.")]
    public KnockbackResolverConfig resolverConfig;

    [Header("Game Design: Attack Detection")]
    public float punchRange = 1.8f;

    public float punchRangeRunning = 2.6f; // PunchRunning has longer range
    public float edgeThresholdRatio = 0.6f; // hits above this ratio = edge (½ damage)
    public LayerMask enemyLayer;

    [Header("Game Design: Punch Box")]
    // Ancho de la caja en profundidad, mas grande es mas facil acertar
    public float punchBoxDepth = 2.5f;

    public float punchBoxHeight = 2f;

    // Distancia entre el jugador y el inicio de la caja, mas alto es mas lejos
    public float punchBoxStartOffset = 0.5f;

    // Altura del centro de la caja sobre el jugador
    public float punchBoxCenterHeight = 1f;

    // Cuanto influye el movimiento del jugador en la direccion del empuje
    [Range(0f, 1f)] public float inertiaInfluence = 0.5f;

    [Header("Game Design: Damage Values")]
    public float basicPunchDamage = 1f;

    public float runningPunchDamage = 1.5f;
    public float edgeDamageMultiplier = 0.5f;

    [Header("Game Design: Knockback Multiplier")]
    [Tooltip("Additional knockback multiplier when attacking (vs colliding).\n" +
             "Example: if ClashHandler gives 8, attacking gives 8 * attackKnockbackMultiplier")]
    public float attackKnockbackMultiplier = 1.5f;

    // Ultima caja de puñetazo, solo para el modo debug
    public static Vector3 DebugBoxCenter;
    public static Vector3 DebugBoxHalf;
    public static float DebugBoxTime = -100f;

    private PlayerController _player;
    private AttackType _preparedAttackType;
    private bool _hasPreparedAttack;

    void Awake() => _player = GetComponent<PlayerController>();

    /// <summary>
    /// Stores the attack type selected by the state before the animation starts.
    /// The Animation Event later consumes this exact context.
    /// </summary>
    public void PrepareAttack(AttackType attackType)
    {
        _preparedAttackType = attackType;
        _hasPreparedAttack = true;
    }

    public void CancelPreparedAttack()
    {
        if (_hasPreparedAttack)
            Debug.LogWarning("[Attack] Golpe cancelado antes de llegar al frame de impacto");

        _hasPreparedAttack = false;
    }

    public void ExecutePreparedAttack()
    {
        if (!_hasPreparedAttack)
        {
            Debug.LogWarning("[Attack] Animation Event received without a prepared attack.", this);
            return;
        }

        AttackType attackType = _preparedAttackType;
        _hasPreparedAttack = false;

        if (!_player.canAttack)
        {
            Debug.LogWarning("[Attack] Impacto ignorado porque el jugador no puede atacar en este momento");
            return;
        }
        _player.canAttack = false;

        // Pega hacia el lado al que mira, nunca hacia la profundidad
        Vector3 facing = new Vector3(0f, 0f, _player.movement.FacingZSign);
        Vector3 inertia = _player.movement.LastDirection;

        float reach = attackType == AttackType.PunchRunning ? punchRangeRunning : punchRange;
        Vector3 halfExtents = new Vector3(punchBoxDepth * 0.5f, punchBoxHeight * 0.5f, reach * 0.5f);
        Vector3 center = transform.position + Vector3.up * punchBoxCenterHeight + facing * (punchBoxStartOffset + reach * 0.5f);

        DrawDebugBox(center, halfExtents, Color.red, 0.5f);

        // Se guarda para poder verla en el modo debug del panel
        DebugBoxCenter = center;
        DebugBoxHalf = halfExtents;
        DebugBoxTime = Time.time;

        LayerMask mask = enemyLayer.value == 0 ? ~0 : enemyLayer;
        Collider[] all = Physics.OverlapBox(center, halfExtents, Quaternion.identity, mask);

        // Ignoramos los colliders del propio jugador
        Collider[] hits = System.Array.FindAll(all, c =>
            c.gameObject != gameObject && !c.transform.IsChildOf(transform)
        );

        if (hits.Length == 0)
        {
            Debug.Log($"[Attack] No hit — box center:{center} half:{halfExtents} facing:{facing}");
            return;
        }

        // El empuje va hacia donde mira, mezclado con el movimiento que llevaba el jugador
        Vector3 direction = facing + new Vector3(inertia.x, 0f, inertia.z) * inertiaInfluence;
        direction.y = 0f;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : facing;

        Collider closest = GetClosest(hits);
        float damage = CalculateDamage(attackType, true);

        // Enemy endurance no longer exists — only the player's matters
        KnockbackResult result = KnockbackResolver.Resolve(
            attackType,
            _player.combat.EffectiveEndurance,
            resolverConfig
        );

        if (closest.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage((int)damage);

        if (closest.TryGetComponent<IKnockbackable>(out var kb))
            kb.ReceiveKnockback(direction, result.ForceOnTarget);

        if (result.ForceOnSelf > 0.01f)
            _player.knockbackHandler.ReceiveKnockback(-direction, result.ForceOnSelf);
        _player.movement.LastDirection = Vector3.zero;
        _player.movement.CurrentSpeed = Vector3.zero;

        Debug.Log($"[Attack] {attackType} | playerEnd:{_player.combat.EffectiveEndurance} " +
                  $"forceEnemy:{result.ForceOnTarget:F1} forceSelf:{result.ForceOnSelf:F1}");
    }

    // CombatAttackHandler.cs — full GetClosest method
    private Collider GetClosest(Collider[] hits)
    {
        Collider closest = hits[0];
        float minDist = Vector3.Distance(transform.position, hits[0].transform.position);

        for (int i = 1; i < hits.Length; i++)
        {
            float d = Vector3.Distance(transform.position, hits[i].transform.position);
            if (d < minDist)
            {
                minDist = d;
                closest = hits[i];
            }
        }

        return closest;
    }
    private static void DrawDebugBox(Vector3 center, Vector3 half, Color color, float duration)
    {
        Vector3 min = center - half;
        Vector3 max = center + half;
        Vector3 a = new Vector3(min.x, min.y, min.z), b = new Vector3(max.x, min.y, min.z);
        Vector3 c = new Vector3(max.x, min.y, max.z), d = new Vector3(min.x, min.y, max.z);
        Vector3 up = new Vector3(0f, max.y - min.y, 0f);

        Debug.DrawLine(a, b, color, duration); Debug.DrawLine(b, c, color, duration);
        Debug.DrawLine(c, d, color, duration); Debug.DrawLine(d, a, color, duration);
        Debug.DrawLine(a + up, b + up, color, duration); Debug.DrawLine(b + up, c + up, color, duration);
        Debug.DrawLine(c + up, d + up, color, duration); Debug.DrawLine(d + up, a + up, color, duration);
        Debug.DrawLine(a, a + up, color, duration); Debug.DrawLine(b, b + up, color, duration);
        Debug.DrawLine(c, c + up, color, duration); Debug.DrawLine(d, d + up, color, duration);
    }

    private float CalculateDamage(AttackType type, bool isCenterHit)
    {
        float multiplier = isCenterHit ? 1f : edgeDamageMultiplier;
        float baseDamage = type == AttackType.PunchRunning
            ? runningPunchDamage
            : basicPunchDamage;
        return baseDamage * multiplier;
    }
}
