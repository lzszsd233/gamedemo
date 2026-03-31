using UnityEngine;

public class PlayerClimbState : PlayerState
{
    private float nextAllowedGrabTime = 0f;
    public PlayerClimbState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public bool CanGrad()
    {
        return Time.time >= nextAllowedGrabTime;
    }

    public void StartCooldown(float duration)
    {
        nextAllowedGrabTime = Time.time + duration;
    }

    public override void Enter()
    {
        base.Enter();
        // 抓墙瞬间，消除重力和所有速度，死死钉在墙上
        stateMachine.Speed = Vector2.zero;

        // 播放抓墙动画 (如果你的小恐龙有的话，没有就用 WallSlide 代替)
        //stateMachine.Anim.PlayWallSlide();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (stateMachine.dashAction.action.WasPressedThisFrame() && stateMachine.CanDash)
        {
            stateMachine.ChangeState(stateMachine.DashState);
            return;
        }

        if (!stateMachine.grabAction.action.IsPressed() || !stateMachine.IsTouchingWall())
        {
            stateMachine.ChangeState(stateMachine.JumpState);
            return;
        }

        if (stateMachine.IsGrounded())
        {
            stateMachine.ChangeState(stateMachine.NormalState);
            return;
        }

        // 贴墙跳跃
        if (stateMachine.jumpAction.action.WasPressedThisFrame())
        {
            float moveX = stateMachine.MoveInput.x;
            bool isPushingAway = (moveX != 0 && Mathf.Sign(moveX) != stateMachine.FacingDir);

            if (isPushingAway)
            {
                stateMachine.CurrentStamina -= 25f;
                // 执行蹬墙跳逻辑 (复用我们之前的代码)
                float jumpDir = -stateMachine.FacingDir;
                stateMachine.Speed = new Vector2(jumpDir * stateMachine.wallJumpForceX, stateMachine.wallJumpForceY);
                stateMachine.JumpState.ConfigureWallJumpLock(stateMachine.wallJumpDuration, jumpDir);
            }
            else
            {
                stateMachine.CurrentStamina -= 25f;
                stateMachine.Speed = new Vector2(0f, stateMachine.jumpForce);
                StartCooldown(0.2f);
            }

            // 【核心新增】：继承抓着的墙壁的动量！
            Collider2D wall = stateMachine.GetWallCollider();
            if (wall != null)
            {
                MomentumBlock block = wall.GetComponentInParent<MomentumBlock>();
                if (block != null)
                {
                    // 把墙壁的移动速度叠加给自己！
                    stateMachine.Speed += block.CurrentVelocity;
                }
            }

            stateMachine.ChangeState(stateMachine.JumpState);
            return;
        }

        // 体力消耗计算
        float moveY = stateMachine.MoveInput.y;
        if (moveY > 0)
        {
            // 往上爬，消耗巨大
            stateMachine.CurrentStamina -= stateMachine.climbStaminaCost * Time.deltaTime;
        }
        else
        {
            // 不动或往下滑，消耗极小
            stateMachine.CurrentStamina -= stateMachine.holdStaminaCost * Time.deltaTime;
        }

        // 体力耗尽强制脱手掉落
        if (stateMachine.CurrentStamina <= 0)
        {
            stateMachine.CurrentStamina = 0;
            stateMachine.ChangeState(stateMachine.WallSlideState); // 没力气了，只能滑下去
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 攀爬的物理极度简单：完全根据玩家的 Y 轴输入来决定上下速度
        float moveY = stateMachine.MoveInput.y;
        // 保证在墙上时，X轴绝对不动
        stateMachine.Speed.x = 0f;
        // Y 轴速度完全听从摇杆的指挥
        stateMachine.Speed.y = moveY * stateMachine.climbSpeed;
        // 因为没有减去重力，所以只要不推摇杆Speed.y 就是 0
    }

    public override void Exit()
    {
        base.Exit();
    }
}
