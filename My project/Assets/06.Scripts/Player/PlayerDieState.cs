using UnityEngine;

public class PlayerDieState : PlayerState
{
    public PlayerDieState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        float bounceDirX = Random.Range(-1f, 1f);
        float bounceForceY = 8f;

        stateMachine.RB.gravityScale = stateMachine.defaultGravity;
        stateMachine.RB.linearVelocity = new Vector2(bounceDirX * 5f, bounceForceY);

        // stateMachine.Anim.Play("Player_Hurt");

        stateMachine.StartDeathSequence();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
    }

    public override void Exit()
    {
        base.Exit();
        // 当退出死亡状态时，把物理和视觉恢复原状
        stateMachine.RB.gravityScale = stateMachine.defaultGravity;
        stateMachine.Anim.gameObject.SetActive(true);
    }
}
