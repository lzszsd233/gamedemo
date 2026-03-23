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

        stateMachine.RB.gravityScale = stateMachine.defaultGravity; // 确保起跳时重力是正常的
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (stateMachine.RB.linearVelocity.y > 0)
        {
            stateMachine.Anim.PlayJump();
        }
        else
        {
            stateMachine.Anim.PlayFall();
        }

        // 跳跃打断 (Jump Cut)
        // 如果玩家在上升过程中（y速度>0），并且松开了跳跃键
        if (stateMachine.RB.linearVelocity.y > 0 && Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            // 瞬间削减向上的速度，实现轻按小跳，长按大跳
            stateMachine.RB.linearVelocity = new Vector2(stateMachine.RB.linearVelocity.x, stateMachine.RB.linearVelocity.y * stateMachine.jumpCutMult);
        }

        // 下落重力加倍 (Fall Gravity)
        // 如果玩家开始下落（y速度<0）
        if (stateMachine.RB.linearVelocity.y < 0)
        {
            // 增加重力，使下落更快更有手感
            stateMachine.RB.gravityScale = stateMachine.defaultGravity * stateMachine.fallGravityMult;
        }

        // 状态切换：如果检测到落地，并且正在往下掉（防止起跳瞬间被误判落地）
        if (stateMachine.IsGrounded() && stateMachine.RB.linearVelocity.y <= 0.1f)
        {
            stateMachine.ChangeState(stateMachine.NormalState);
        }

        // 滑墙判定
        // 只要 X 轴有输入，且方向对着墙，就能滑墙！不管有没有按 W 或 S
        bool isPushingWall = stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == stateMachine.FacingDir;

        if (stateMachine.RB.linearVelocity.y < 0 && stateMachine.IsTouchingWall() && isPushingWall)
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
            float forcedVelocityX = stateMachine.WallJumpDirection * stateMachine.wallJumpForceX;
            stateMachine.RB.linearVelocity = new Vector2(forcedVelocityX, stateMachine.RB.linearVelocity.y);
        }
        else
        {
            // 锁解开了！把空中的控制权还给玩家
            float targetVelocityX = stateMachine.MoveInput.x * stateMachine.moveSpeed;
            stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);
        }
    }

    public override void Exit()
    {
        base.Exit();

        // 恢复正常重力
        stateMachine.RB.gravityScale = stateMachine.defaultGravity;
    }
}
