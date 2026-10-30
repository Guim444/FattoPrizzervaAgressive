using System;
using UnityEngine;

// Agarre de RioTutte para la fase del agarre
// Corre un trozo corto hacia el jugador y, si lo alcanza, lo agarra y lo estampa contra el suelo
// El jugador se levanta pulsando muchas veces el boton de salto
public sealed class RioTutteGrabAttack : MonoBehaviour
{
    [Header("Carrera")]
    public float runSpeed = 7.5f;
    public float maxRunSeconds = 1.2f;

    // Si el jugador esta mas cerca que esto, RioTutte prefiere el agarre al Dash
    public float triggerDistance = 3.5f;

    // Distancia extra a la que se considera que alcanza al jugador
    public float contactMargin = 0.3f;

    [Header("Estampada")]
    public int slamDamage = 3;
    public int smashPresses = 8;
    public float smashSeconds = 6f;

    [Header("Pausas")]
    public float cooldownSeconds = 3f;

    // Tiempo tras levantarse en el que no se le puede volver a agarrar
    public float getUpGraceSeconds = 1.5f;

    public bool IsRunning { get; private set; }
    public bool IsHoldingPlayer { get; private set; }
    public bool IsBusy => IsRunning || IsHoldingPlayer;
    public bool CanStart => !IsBusy && Time.time >= _nextAllowedTime && Time.time >= _noGrabUntil;

    // true si el jugador agarrado se levanto a tiempo
    public event Action<bool> GrabFinished;
    public event Action GrabMissed;

    private CharacterController _cc;
    private Animator _anim;
    private PlayerController _player;
    private Transform _visual;
    private Quaternion _visualRotation;
    private float _nextAllowedTime;
    private float _noGrabUntil;
    private Coroutine _routine;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _anim = GetComponentInChildren<Animator>();
    }

    public bool PlayerIsNear()
    {
        if (!FindPlayer()) return false;

        Vector3 delta = _player.transform.position - transform.position;
        delta.y = 0f;
        return delta.magnitude <= triggerDistance;
    }

    public void StartGrab()
    {
        if (!CanStart || !FindPlayer()) return;
        _routine = StartCoroutine(GrabRoutine());
    }

    // Cancela todo, por ejemplo al parar la prueba
    public void Cancel()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = null;
        IsRunning = false;

        if (IsHoldingPlayer && _player != null)
        {
            SmashButtonChallenge smash = _player.GetComponent<SmashButtonChallenge>();
            if (smash != null) smash.Cancel();
            ReleasePlayer();
        }

        IsHoldingPlayer = false;
    }

    private bool FindPlayer()
    {
        if (_player == null) _player = FindFirstObjectByType<PlayerController>();
        return _player != null;
    }

    private System.Collections.IEnumerator GrabRoutine()
    {
        IsRunning = true;
        float endTime = Time.time + maxRunSeconds;
        bool caught = false;

        // Corre hacia donde esta el jugador en cada momento
        while (Time.time < endTime)
        {
            Vector3 toPlayer = _player.transform.position - transform.position;
            toPlayer.y = 0f;

            if (IsInContact(toPlayer.magnitude))
            {
                caught = true;
                break;
            }

            if (_cc.enabled && toPlayer.sqrMagnitude > 0.0001f)
                _cc.Move(toPlayer.normalized * runSpeed * Time.deltaTime);

            yield return null;
        }

        IsRunning = false;
        _routine = null;

        if (caught)
        {
            HoldPlayer();
        }
        else
        {
            _nextAllowedTime = Time.time + cooldownSeconds;
            GrabMissed?.Invoke();
        }
    }

    private bool IsInContact(float distance)
    {
        float bossRadius = _cc.radius * Mathf.Abs(transform.lossyScale.x);
        float playerRadius = _player.cc.radius * Mathf.Abs(_player.transform.lossyScale.x);
        return distance <= bossRadius + playerRadius + contactMargin;
    }

    // Agarra al jugador, lo estampa y empieza el Smash the Button para levantarse
    private void HoldPlayer()
    {
        IsHoldingPlayer = true;

        if (_anim != null) _anim.SetTrigger("Punch");

        _player.ResetToIdle();
        _player.canMove = false;

        if (slamDamage > 0)
        {
            _player.combat.TakeDamage(slamDamage);

            // TakeDamage deja al jugador en Knockedback, lo devolvemos a Idle
            if (_player.combat.HP <= 0)
            {
                IsHoldingPlayer = false;
                _nextAllowedTime = Time.time + cooldownSeconds;
                return;
            }

            _player.stateMachineController.TransitionTo(State.Idle);
        }

        LayPlayerDown();

        SmashButtonChallenge smash = _player.GetComponent<SmashButtonChallenge>();
        if (smash == null) smash = _player.gameObject.AddComponent<SmashButtonChallenge>();
        smash.Begin("Levantate", smashPresses, smashSeconds, OnSmashFinished);
    }

    private void OnSmashFinished(bool success)
    {
        ReleasePlayer();
        IsHoldingPlayer = false;
        _nextAllowedTime = Time.time + cooldownSeconds;
        _noGrabUntil = Time.time + getUpGraceSeconds;
        GrabFinished?.Invoke(success);
    }

    // Placeholder de estar en el suelo: se gira el sprite del jugador 90 grados
    private void LayPlayerDown()
    {
        SpriteRenderer sprite = _player.GetComponentInChildren<SpriteRenderer>(true);
        _visual = sprite != null ? sprite.transform : null;
        if (_visual == null) return;

        _visualRotation = _visual.localRotation;
        _visual.localRotation = _visualRotation * Quaternion.Euler(0f, 0f, 90f);
    }

    private void ReleasePlayer()
    {
        if (_visual != null) _visual.localRotation = _visualRotation;
        if (_player != null && _player.combat.HP > 0) _player.canMove = true;
    }
}
