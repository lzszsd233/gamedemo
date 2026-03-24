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

        stateMachine.Speed = new Vector2(bounceDirX * 5f, bounceForceY);

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
        //stateMachine.Speed.y -= stateMachine.customGravity * Time.fixedDeltaTime;
    }

    public override void Exit()
    {
        base.Exit();
    }
}
