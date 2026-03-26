using UnityEngine;

public class PlayerWallSlideState : PlayerState
{
    public PlayerWallSlideState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        stateMachine.Speed.x = 0f;
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

        // 在 WallSlide 判断之前优先判断 Climb
        if (stateMachine.grabAction.action.IsPressed() && stateMachine.IsTouchingWall() && stateMachine.CurrentStamina > 0 && stateMachine.GrabCooldownCounter <= 0f)
        {
            stateMachine.ChangeState(stateMachine.ClimbState);
            return;
        }

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

            // 直接修改自定义速度 Speed
            stateMachine.Speed = new Vector2(jumpDirection * stateMachine.wallJumpForceX, stateMachine.wallJumpForceY);

            // 接下来 0.15 秒不要听玩家的左右摇杆
            stateMachine.SetWallJumpLock(jumpDirection);

            // 切回空中下落状态
            stateMachine.ChangeState(stateMachine.JumpState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 【核心重构】：滑墙摩擦力模拟
        // 在滑墙状态下，我们不应用普通重力，而是手动限制最大下落速度
        // 每帧给一个向下的趋势，但限制在 wallSlideSpeed
        stateMachine.Speed.y -= stateMachine.customGravity * Time.fixedDeltaTime;

        if (stateMachine.Speed.y < -stateMachine.wallSlideSpeed)
        {
            stateMachine.Speed.y = -stateMachine.wallSlideSpeed;
        }

        // 确保 X 轴速度保持为 0 以紧贴墙面
        stateMachine.Speed.x = 0f;
    }

    public override void Exit()
    {
        base.Exit();
    }
}
