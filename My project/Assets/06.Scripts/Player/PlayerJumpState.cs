using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJumpState : PlayerState
{
    private float wallJumpLockTimer = 0f;
    private float wallJumpDirection = 0f;
    private bool isSuperJumpMode = false;
    private float superJumpDirectionX = 0f;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public void ConfigureWallJumpLock(float duration, float direction)
    {
        wallJumpLockTimer = duration;
        wallJumpDirection = direction;
    }

    public void ConfigureSuperJump(float directionX)
    {
        isSuperJumpMode = true;
        superJumpDirectionX = directionX;
    }


    public override void Enter()
    {
        base.Enter();

        if (isSuperJumpMode)
        {
            stateMachine.Speed.y = stateMachine.jumpForce * 0.8f;
            stateMachine.Speed.x = superJumpDirectionX * stateMachine.dashSpeed * 1.1f;
        }
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (wallJumpLockTimer > 0)
        {
            wallJumpLockTimer -= Time.deltaTime;
        }

        if (stateMachine.Speed.y > 0)
        {
            stateMachine.Anim.PlayJump();
        }
        else
        {
            stateMachine.Anim.PlayFall();
        }

        // 跳跃打断
        // 如果玩家在上升过程中（y速度>0），并且松开了跳跃键
        if (stateMachine.Speed.y > 0 && stateMachine.jumpAction.action.WasReleasedThisFrame())
        {
            stateMachine.Speed.y *= stateMachine.jumpCutMult;
        }

        if (stateMachine.IsGrounded() && stateMachine.Speed.y <= 0.1f)
        {
            stateMachine.ChangeState(stateMachine.NormalState);
            return;
        }

        // 在 WallSlide 判断之前优先判断 Climb
        if (stateMachine.grabAction.action.IsPressed() && stateMachine.IsTouchingWall() && stateMachine.CurrentStamina > 0 && stateMachine.ClimbState.CanGrad())
        {
            stateMachine.ChangeState(stateMachine.ClimbState);
            return;
        }

        // 滑墙判定
        // 只要 X 轴有输入，且方向对着墙，就能滑墙！不管有没有按 W 或 S
        bool isPushingWall = stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == stateMachine.FacingDir;

        if (stateMachine.Speed.y < 0 && stateMachine.IsTouchingWall() && isPushingWall)
        {
            stateMachine.ChangeState(stateMachine.WallSlideState);
            return;
        }

        if (stateMachine.DashBufferCounter > 0f && stateMachine.CanDash && stateMachine.ActionLockCounter <= 0f)
        {
            stateMachine.ConsumeDashBuffer();
            stateMachine.ChangeState(stateMachine.DashState);
            return;// 切状态后加 return
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 检查是不是刚被墙壁弹出来
        if (wallJumpLockTimer > 0)
        {
            // 锁在的时候，绝对不碰 Speed.x
            // 锁的意义仅仅是：不走下面那个 else 分支，剥夺玩家用 MoveInput.x 减速的权力！
            // 这样，你起跳时继承的 42 的神速，会毫无保留、不受任何阻力地在这 0.15 秒内全部喷发出来！
        }
        else
        {
            // 锁解开了！空中的平滑移动控制
            float targetSpeedX = stateMachine.MoveInput.x * stateMachine.moveSpeed;

            float currentAbsSpeedX = Mathf.Abs(stateMachine.Speed.x);

            // 【核心重构：超速状态下的动量保鲜法则】
            if (currentAbsSpeedX > stateMachine.moveSpeed)
            {
                // 1. 【顺水推舟】：玩家推摇杆的方向，和当前极速飞行的方向一模一样！
                if (stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == Mathf.Sign(stateMachine.Speed.x))
                {
                    // 魔法就在这里：空气阻力为 0！绝不减速！
                    // 只要你死死按住方向键，30 的速度就会一直保持 30，直到你撞墙或落地！
                    float momentumDecay = 0f;
                    stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, momentumDecay * Time.fixedDeltaTime);
                }
                // 2. 【悬崖勒马】：玩家反推摇杆，想要紧急刹车
                else if (stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) != Mathf.Sign(stateMachine.Speed.x))
                {
                    float brakeAcceleration = 50f; // 刹车阻力给大点，保持微操手感
                    stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, brakeAcceleration * Time.fixedDeltaTime);
                }
                // 3. 【随波逐流】：玩家完全松开了键盘
                else
                {
                    float slideDecay = 2f; // 给一个很小的阻力，让他能在空中飘行很远
                    stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, slideDecay * Time.fixedDeltaTime);
                }
            }
            else
            {
                // 普通跳跃（没携带极速），保持指哪打哪的敏捷手感
                float airAcceleration = 40f;
                stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, airAcceleration * Time.fixedDeltaTime);
            }
        }

        float currentGravity = stateMachine.customGravity;

        // 下落重力加倍 (Fall Gravity)
        if (stateMachine.Speed.y < 0)
        {
            currentGravity *= stateMachine.fallGravityMult;
        }

        // 核心物理运算：每秒往下掉
        stateMachine.Speed.y -= currentGravity * Time.fixedDeltaTime;

        // 安全锁：限制最大下落速度，防止穿模
        stateMachine.Speed.y = Mathf.Max(stateMachine.Speed.y, stateMachine.maxFallSpeed);


    }

    public override void Exit()
    {
        base.Exit();

        wallJumpLockTimer = 0f;
        wallJumpDirection = 0f;

        isSuperJumpMode = false;
        superJumpDirectionX = 0f;
    }
}
