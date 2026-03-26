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
                // 1. 计算当前冲刺已经过去了多久
                float dashTimeElapsed = Time.time - dashStartTime;

                // 2. 设定超级跳的极短窗口（比如 0.1 秒，你可以把这个提到大管家里方便调）
                float superJumpWindow = 0.05f;

                // 3. 判断是否在黄金时间内起跳！
                if (dashTimeElapsed <= superJumpWindow)
                {

                    // 赋予正常起跳的垂直速度
                    stateMachine.Speed.y = stateMachine.jumpForce;

                    // 继承并保持冲刺时极高的水平速度
                    stateMachine.Speed.x = Mathf.Sign(dashDirection.x) * stateMachine.dashSpeed;
                }
                else
                {
                    stateMachine.Speed.y = stateMachine.jumpForce;
                    stateMachine.Speed.x = Mathf.Sign(dashDirection.x) * stateMachine.moveSpeed;
                }

                stateMachine.ChangeState(stateMachine.JumpState);
                return;
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
