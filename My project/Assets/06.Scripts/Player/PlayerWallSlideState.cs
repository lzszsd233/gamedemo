using UnityEngine;

public class PlayerWallSlideState : PlayerState
{
    public PlayerWallSlideState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // 进入滑墙状态时，确保重力是正常的
        stateMachine.RB.gravityScale = stateMachine.defaultGravity;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (stateMachine.IsGrounded())
        {
            stateMachine.ChangeState(stateMachine.NormalState);
            return;
        }

        // 只要玩家的 X 轴输入不为 0，并且输入的符号（左-1 右1）等于墙壁的方向，就算在推墙
        // Mathf.Sign 会把任何正数变成 1，负数变成 -1
        bool isPushingWall = stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == stateMachine.FacingDir;

        if (!stateMachine.IsTouchingWall() || !isPushingWall)
        {
            stateMachine.ChangeState(stateMachine.JumpState);
        }

        if (stateMachine.dashAction.action.WasPressedThisFrame() && stateMachine.CanDash)
        {
            stateMachine.ChangeState(stateMachine.DashState);
        }

        // 蹬墙跳
        if (stateMachine.jumpAction.action.WasPressedThisFrame())
        {
            // 获取跳跃的反方向
            float jumpDirection = -stateMachine.FacingDir;

            // 瞬间赋予斜向爆发力
            stateMachine.RB.linearVelocity = new Vector2(jumpDirection * stateMachine.wallJumpForceX, stateMachine.wallJumpForceY);

            // 接下来 0.15 秒不要听玩家的左右摇杆
            stateMachine.SetWallJumpLock(jumpDirection);

            // 切回空中下落状态
            stateMachine.ChangeState(stateMachine.JumpState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        Vector2 currentVelocity = stateMachine.RB.linearVelocity;

        // 如果当前正在往下掉，且掉落速度超过了我们设定的最大滑墙速度
        if (currentVelocity.y < 0)
        {
            // 强行把下落速度锁死在 -wallSlideSpeed (比如 -2f)
            currentVelocity.y = Mathf.Max(currentVelocity.y, -stateMachine.wallSlideSpeed);
        }

        // 把修改后的速度还给刚体
        stateMachine.RB.linearVelocity = currentVelocity;
    }
}
