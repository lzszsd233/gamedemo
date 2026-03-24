using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJumpState : PlayerState
{
    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (stateMachine.Speed.y > 0)
        {
            stateMachine.Anim.PlayJump();
        }
        else
        {
            stateMachine.Anim.PlayFall();
        }

        // 跳跃打断 (Jump Cut)
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

        // 滑墙判定
        // 只要 X 轴有输入，且方向对着墙，就能滑墙！不管有没有按 W 或 S
        bool isPushingWall = stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == stateMachine.FacingDir;

        if (stateMachine.Speed.y < 0 && stateMachine.IsTouchingWall() && isPushingWall)
        {
            stateMachine.ChangeState(stateMachine.WallSlideState);
        }

        if (stateMachine.dashAction.action.WasPressedThisFrame() && stateMachine.CanDash)
        {
            stateMachine.ChangeState(stateMachine.DashState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 检查是不是刚被墙壁弹出来
        if (stateMachine.WallJumpLockCounter > 0)
        {
            // 锁还在,此时剥夺玩家摇杆的权力
            // 强制维持被弹开的方向和设定的速度
            stateMachine.Speed.x = stateMachine.WallJumpDirection * stateMachine.wallJumpForceX;
        }
        else
        {
            // 【修改这里】：锁解开了！空中的平滑移动控制
            float targetSpeedX = stateMachine.MoveInput.x * stateMachine.moveSpeed;

            // 空中加速度（设置得比地面小一点，比如 60f，能保留更多的弹簧弹飞惯性）
            float airAcceleration = 60f;

            // 平滑趋近目标速度
            stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, airAcceleration * Time.fixedDeltaTime);
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
    }
}
