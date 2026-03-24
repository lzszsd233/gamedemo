using UnityEngine;

public class PlayerClimbState : PlayerState
{
    public PlayerClimbState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // 抓墙瞬间，消除重力和所有速度，死死钉在墙上
        stateMachine.RB.gravityScale = 0f;
        stateMachine.RB.linearVelocity = Vector2.zero;

        // 播放抓墙动画 (如果你的小恐龙有的话，没有就用 WallSlide 代替)
        //stateMachine.Anim.PlayWallSlide();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 【退出条件 1】：松开抓取键，或者摇杆反推离开了墙壁 -> 掉下去变成滑墙或空中
        bool isPushingWall = stateMachine.MoveInput.x != 0 && Mathf.Sign(stateMachine.MoveInput.x) == stateMachine.FacingDir;
        if (!stateMachine.grabAction.action.IsPressed() || (!stateMachine.IsTouchingWall() && !isPushingWall))
        {
            stateMachine.ChangeState(stateMachine.JumpState);
            return;
        }

        // 【退出条件 2】：落地了
        if (stateMachine.IsGrounded())
        {
            stateMachine.ChangeState(stateMachine.NormalState);
            return;
        }

        // 【核心交互】：贴墙跳跃！
        if (stateMachine.jumpAction.action.WasPressedThisFrame())
        {
            // 消耗一截体力进行贴墙起跳
            stateMachine.CurrentStamina -= 25f;

            // 执行蹬墙跳逻辑 (复用我们之前的代码)
            float jumpDir = -stateMachine.FacingDir;
            stateMachine.RB.linearVelocity = new Vector2(jumpDir * stateMachine.wallJumpForceX, stateMachine.wallJumpForceY);
            stateMachine.SetWallJumpLock(jumpDir);

            stateMachine.ChangeState(stateMachine.JumpState);
            return;
        }

        // 【体力消耗计算】
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

        // 【退出条件 3】：体力耗尽！强制脱手掉落！
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
        stateMachine.RB.linearVelocity = new Vector2(0f, moveY * stateMachine.climbSpeed);
    }

    public override void Exit()
    {
        base.Exit();
        // 离开攀爬状态，必须把重力还给角色！
        //stateMachine.RB.gravityScale = stateMachine.defaultGravity;
    }
}
