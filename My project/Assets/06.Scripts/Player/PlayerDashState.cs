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

        // 条件：冲刺状态中 + 必须在地面上 + 按下跳跃键
        if (stateMachine.IsGrounded() && stateMachine.JumpBufferCounter > 0f)
        {
            if (dashDirection.x != 0)
            {
                stateMachine.ConsumeJumpBuffer();
                float dashTimeElapsed = Time.time - dashStartTime;
                float superJumpWindow = 0.05f;

                if (dashTimeElapsed <= superJumpWindow)
                {
                    // 触发超级跳：配置状态并切换
                    stateMachine.JumpState.ConfigureSuperJump(Mathf.Sign(dashDirection.x));
                    stateMachine.ChangeState(stateMachine.JumpState);
                    return;
                }
                else
                {
                    // 触发普通跳：直接切换，不用配置，因为 JumpState 默认就是普通跳
                    stateMachine.Speed.y = stateMachine.jumpForce;
                    stateMachine.Speed.x = Mathf.Sign(dashDirection.x) * stateMachine.dashSpeed;
                    stateMachine.ChangeState(stateMachine.JumpState);
                    return;
                }
            }
        }

        if (Time.time >= dashStartTime + stateMachine.dashDuration)
        {
            stateMachine.Speed *= 0.5f;

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
    }
}
