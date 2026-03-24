using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 dashDirection;
    private float dashStartTime;
    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //冻结帧
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.Hitstop(0.05f);
        }

        stateMachine.Anim.PlayDash();

        stateMachine.CanDash = false;
        dashStartTime = Time.time;
        stateMachine.RB.gravityScale = 0f;
        dashDirection = stateMachine.MoveInput;

        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(stateMachine.FacingDir, 0);
        }
        // .normalized 的作用是：确保斜向冲刺时，速度不会比单向快（防止勾股定理导致的加速）
        dashDirection = dashDirection.normalized;

        stateMachine.Speed = dashDirection * stateMachine.dashSpeed;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Time.time >= dashStartTime + stateMachine.dashDuration)
        {
            if (stateMachine.IsGrounded())
                stateMachine.ChangeState(stateMachine.NormalState);
            else
                stateMachine.ChangeState(stateMachine.JumpState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        stateMachine.Speed = dashDirection * stateMachine.dashSpeed;
    }

    public override void Exit()
    {
        base.Exit();
        stateMachine.Speed *= 0.5f;
    }
}
