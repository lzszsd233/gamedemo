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
            stateMachine.Speed.x = superJumpDirectionX * stateMachine.dashSpeed * 1.2f;
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

        if (stateMachine.DashBufferCounter > 0f && stateMachine.CanDash)
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
            // 锁还在，强制维持被弹开的方向和设定的速度
            stateMachine.Speed.x = wallJumpDirection * stateMachine.wallJumpForceX;
        }
        else
        {
            // 锁解开了！空中的平滑移动控制
            float targetSpeedX = stateMachine.MoveInput.x * stateMachine.moveSpeed;

            float currentAbsSpeedX = Mathf.Abs(stateMachine.Speed.x);

            // 核心修复：现在只检查状态内部的 isSuperJumpMode 标记
            if (currentAbsSpeedX > stateMachine.moveSpeed && isSuperJumpMode)
            {
                if (stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) != Mathf.Sign(stateMachine.Speed.x))
                {
                    float brakeAcceleration = 60f;
                    stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, brakeAcceleration * Time.fixedDeltaTime);
                }
                else
                {
                    float momentumDecay = 10f;
                    stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, momentumDecay * Time.fixedDeltaTime);
                }
            }
            else
            {
                // 普通跳跃，或者冲刺末尾的普通起跳，都会走这里，享受正常的空气阻力迅速降速
                float airAcceleration = 60f;
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
