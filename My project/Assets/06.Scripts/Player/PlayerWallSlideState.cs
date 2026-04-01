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
        if (stateMachine.grabAction.action.IsPressed() && stateMachine.IsTouchingWall() && stateMachine.CurrentStamina > 0 && stateMachine.ClimbState.CanGrad())
        {
            stateMachine.ChangeState(stateMachine.ClimbState);
            return;
        }

        if (!stateMachine.IsTouchingWall() || !isPushingWall)
        {
            stateMachine.ChangeState(stateMachine.JumpState);
            return;
        }

        if (stateMachine.DashBufferCounter > 0f && stateMachine.CanDash)
        {
            stateMachine.ConsumeDashBuffer();
            stateMachine.ChangeState(stateMachine.DashState);
            return;
        }

        // 蹬墙跳
        if (stateMachine.jumpAction.action.WasPressedThisFrame())
        {
            // 获取跳跃的反方向
            float jumpDirection = -stateMachine.FacingDir;

            // 直接修改自定义速度 Speed
            stateMachine.Speed = new Vector2(jumpDirection * stateMachine.wallJumpForceX, stateMachine.wallJumpForceY);

            // 接下来 0.15 秒不要听玩家的左右摇杆
            stateMachine.JumpState.ConfigureWallJumpLock(stateMachine.wallJumpDuration, jumpDirection);

            // 【核心新增】：继承滑着的墙壁的动量！
            Collider2D wall = stateMachine.GetWallCollider();
            if (wall != null)
            {
                MomentumBlock block = wall.GetComponentInParent<MomentumBlock>();
                if (block != null)
                {
                    // 【核心平衡】：
                    // 1. 如果方块在急刹车，吐出了 LiftBoost（比如 30），全额继承！这就是超级跳的奖励！
                    if (block.LiftBoost != Vector2.zero)
                    {
                        stateMachine.Speed += block.LiftBoost;
                    }
                    // 2. 如果方块正在赶路（CurrentVelocity），我们只继承一小部分（比如 30% 到 50%）！
                    // 这样既能感觉被方块带了一下，又绝对达不到超级跳的恐怖高度！
                    else if (block.CurrentVelocity != Vector2.zero)
                    {
                        // 乘以 0.3f 或你觉得合适的手感系数
                        stateMachine.Speed += block.CurrentVelocity * 0.5f;
                    }
                }
            }

            // 切回空中下落状态
            stateMachine.ChangeState(stateMachine.JumpState);
            return;
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
