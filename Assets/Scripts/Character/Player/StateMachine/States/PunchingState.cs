using System.Collections;
using UnityEngine;

public class PunchingState : IStateActions
{
    public PlayerController player;
    public CharacterController controller;
    public PlayerStaminaManager stamina;
    public SphereCollider punchCollider;

    // Impact event: 0.125 s. The safety window keeps the state active
    // until Unity has dispatched the Animation Event, including at low frame rates.
    private const float PunchDuration = 0.95f;

    private bool punchExecuted = false;

    public PunchingState(PlayerStaminaManager staminaManager)
    {
        stamina = staminaManager;
    }

    public void Enter()
    {
        if (punchExecuted) return;
        punchExecuted = true;

        player.movement.ApplyPunchFlip();
        player.combat.RegisterPunch(0, new Vector3(0f, 0f, player.movement.FacingZSign));
        player.combatAttackHandler.PrepareAttack(AttackType.Punch);
        player.animator.SetTrigger("isPunching");

        var combat = player.combat;
        stamina.ModifyStamina(-combat.punchStaminaCostPhase0);
        combat.normalPunchTimer = PunchDuration;
    }

    public void Update()
    {
        var movement = player.movement;
        movement.ApplyPunchFlip();

        bool isMoving = movement.GetDirectionalInput().magnitude > 0.1f;

        if (isMoving)
        {
            stamina.SetWalking();
            Vector3 input = movement.GetDirectionalInput();
            if (controller.enabled)
                controller.Move(input * movement.punchingSpeed * Time.deltaTime);
        }
        else
        {
            stamina.SetIdle();
            if (controller.enabled)
                controller.Move(Vector3.zero);
        }
    }

    public void Exit()
    {
        if (player.canAttack)
            player.combatAttackHandler.CancelPreparedAttack();

        player.canAttack = true;
        punchExecuted = false;
    }
}
