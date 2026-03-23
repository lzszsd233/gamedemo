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

        //赋予冲刺速度
        stateMachine.RB.linearVelocity = dashDirection * stateMachine.dashSpeed;
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
        // 冲刺期间，锁死速度，不接受玩家的移动摇杆干扰
        stateMachine.RB.linearVelocity = dashDirection * stateMachine.dashSpeed;
    }

    public override void Exit()
    {
        base.Exit();
        // 退出冲刺
        stateMachine.RB.gravityScale = stateMachine.defaultGravity;

        // 可选：冲刺结束后，稍微削减一点速度，防止惯性太大飞出去
        stateMachine.RB.linearVelocity = stateMachine.RB.linearVelocity * 0.5f;
    }
}
