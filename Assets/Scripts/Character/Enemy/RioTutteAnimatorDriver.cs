using UnityEngine;

/// <summary>
/// RioTutte-specific animation controller.
/// Extends EnemyAnimatorDriver adding parameters specific to this enemy.
///
/// PARAMETERS MANAGED (in addition to the common ones in EnemyAnimatorDriver):
///   dashGrab      (bool) — during the grab dash
///   isGrabbing    (bool) — holding the player
///   superDash     (bool) — during the super dash
///   knockDownFront (bool) — knocked down face-forward (phase 2)
///   knockDownBack  (bool) — knocked down face-backward (phase 2)
///   phase         (int)  — current phase (0-3)
///
/// CONVENTION:
///   Only continuous-state bools/ints. Punctual SetTriggers
///   (GrabPunch, Hit, Punch) are fired from event code.
/// </summary>
public class RioTutteAnimatorDriver : EnemyAnimatorDriver
{
    private RioTutteEnemy   _rioTutte;
    private RioTutteAttacks _attacks;

    protected override void Awake()
    {
        base.Awake();
        _rioTutte = GetComponent<RioTutteEnemy>();
        _attacks  = GetComponent<RioTutteAttacks>();
    }

    protected override void DriveSpecificParams()
    {
        // Por ahora el Dash nuevo reutiliza las animaciones que ya existen
        // Canal: la carrera de DashGrab, Dash: la del teletransporte de SuperDash
        // La carrera del agarre tambien usa la de DashGrab
        RioTutteDashAttack dash = _rioTutte.Dash;
        RioTutteGrabAttack grab = _rioTutte.Grab;
        bool channeling = dash != null && dash.IsChanneling;
        bool dashing = dash != null && dash.IsDashing;
        bool grabRunning = grab != null && grab.IsRunning;
        bool grabHolding = grab != null && grab.IsHoldingPlayer;

        _anim.SetBool("dashGrab",      _attacks.IsUsingDashGrab || channeling || grabRunning);
        _anim.SetBool("isGrabbing",    _attacks.IsGrabbing || grabHolding);
        _anim.SetBool("superDash",     _attacks.IsUsingSuperDash || dashing);
        _anim.SetBool("knockDownFront", _rioTutte.groundedTimer > 0f &&  _rioTutte.fallDirection);
        _anim.SetBool("knockDownBack",  _rioTutte.groundedTimer > 0f && !_rioTutte.fallDirection);
        _anim.SetInteger("phase",      _rioTutte.CurrentPhase);
    }
}
