using System;
using System.Collections;
using UnityEngine;

public enum DashResult { Interrupted, PlayerPushed, HitObstacle, ReachedTarget }

// Dash del velocista de RioTutte
// Canaliza guardando donde esta el jugador, luego casi se teletransporta a ese punto
// Si el jugador le pega con Thrust 2 o mas hacia el origen del dash, se interrumpe
public sealed class RioTutteDashAttack : MonoBehaviour
{
    [Header("Tiempos")]
    public float channelSeconds = 2f;
    public float dashSeconds = 0.25f;
    public float cooldownSeconds = 5f;

    [Header("Interrupcion")]
    // Un puñetazo hecho hasta estos segundos antes del choque cuenta, mas alto es mas facil
    public float interruptWindow = 1f;
    public int minInterruptThrust = 2;

    // Cuanto tiene que ir el jugador hacia el origen del dash, de -1 a 1
    [Range(-1f, 1f)] public float headingTolerance = 0.2f;

    [Header("Empuje y daño")]
    public float perpendicularForce = 8f;

    public float thrust2PlayerForce = 8f;
    public float thrust2BossForce = 10f;
    public int thrust2Damage = 1;

    public float thrust3PlayerForce = 8f;
    public float thrust3BossForce = 8f;
    public int thrust3Damage = 0;

    // Distancia extra a la que se considera que choca con el jugador
    public float contactMargin = 0.3f;

    public bool IsChanneling { get; private set; }
    public bool IsDashing { get; private set; }
    public bool IsBusy => IsChanneling || IsDashing;
    public bool CanStart => !IsBusy && Time.time >= _nextAllowedTime;
    public float ChannelRemaining => IsChanneling ? Mathf.Max(0f, _channelEndTime - Time.time) : 0f;
    public float CooldownRemaining => Mathf.Max(0f, _nextAllowedTime - Time.time);

    public event Action<DashResult> DashFinished;

    private CharacterController _cc;
    private EnemyBase _enemy;
    private PlayerController _player;
    private float _channelEndTime;
    private float _nextAllowedTime;
    private Coroutine _routine;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _enemy = GetComponent<EnemyBase>();
    }

    public void StartDash()
    {
        if (!CanStart) return;

        if (_player == null)
            _player = FindFirstObjectByType<PlayerController>();
        if (_player == null) return;

        _routine = StartCoroutine(DashRoutine());
    }

    // Cancela el dash en curso, por ejemplo al parar la prueba
    public void Cancel()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        IsChanneling = false;
        IsDashing = false;
    }

    private IEnumerator DashRoutine()
    {
        Vector3 origin = transform.position;
        Vector3 target = _player.transform.position;
        target.y = origin.y;

        // Canal de 2 segundos, el punto de destino ya esta guardado aunque el jugador se mueva
        IsChanneling = true;
        _channelEndTime = Time.time + channelSeconds;
        while (Time.time < _channelEndTime)
            yield return null;
        IsChanneling = false;

        Vector3 flat = target - origin;
        flat.y = 0f;
        float distance = flat.magnitude;
        Vector3 direction = distance > 0.01f ? flat / distance : transform.forward;
        float speed = distance / Mathf.Max(dashSeconds, 0.01f);

        IsDashing = true;
        DashResult result = DashResult.ReachedTarget;
        float travelled = 0f;

        while (travelled < distance)
        {
            float step = Mathf.Min(speed * Time.deltaTime, distance - travelled);
            CollisionFlags flags = _cc.enabled ? _cc.Move(direction * step) : CollisionFlags.None;
            travelled += step;

            if (PlayerInContact())
            {
                result = ResolvePlayerHit(origin, direction);
                break;
            }

            if ((flags & CollisionFlags.Sides) != 0)
            {
                result = DashResult.HitObstacle;
                break;
            }

            yield return null;
        }

        IsDashing = false;
        _routine = null;
        _nextAllowedTime = Time.time + cooldownSeconds;
        DashFinished?.Invoke(result);
    }

    private bool PlayerInContact()
    {
        Vector3 delta = _player.transform.position - transform.position;
        delta.y = 0f;

        float bossRadius = _cc.radius * Mathf.Abs(transform.lossyScale.x);
        float playerRadius = _player.cc.radius * Mathf.Abs(_player.transform.lossyScale.x);
        return delta.magnitude <= bossRadius + playerRadius + contactMargin;
    }

    private DashResult ResolvePlayerHit(Vector3 origin, Vector3 direction)
    {
        PlayerCombat combat = _player.combat;

        Vector3 toOrigin = origin - _player.transform.position;
        toOrigin.y = 0f;
        toOrigin.Normalize();

        bool recentPunch = Time.time - combat.LastPunchTime <= interruptWindow;
        bool heading = Vector3.Dot(combat.LastPunchDirection.normalized, toOrigin) >= headingTolerance;
        int thrust = combat.LastPunchThrust;

        if (recentPunch && heading && thrust >= minInterruptThrust)
        {
            bool strong = thrust >= 3;
            int damage = strong ? thrust3Damage : thrust2Damage;
            float playerForce = strong ? thrust3PlayerForce : thrust2PlayerForce;
            float bossForce = strong ? thrust3BossForce : thrust2BossForce;

            // Los dos retroceden, el daño va antes para que el empuje no se pierda
            if (damage > 0) combat.TakeDamage(damage);
            _player.knockbackHandler.ReceiveEnemyKnockback(direction, playerForce);
            _enemy.ReceiveKnockback(-direction, bossForce);
            return DashResult.Interrupted;
        }

        // Sin interrupcion el jugador sale empujado hacia un lado del trayecto
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        Vector3 relative = _player.transform.position - transform.position;
        relative.y = 0f;
        if (Vector3.Dot(relative, side) < 0f) side = -side;

        _player.knockbackHandler.ReceiveEnemyKnockback(side, perpendicularForce);
        return DashResult.PlayerPushed;
    }
}
