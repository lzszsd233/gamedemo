using UnityEngine;
using UnityEngine.InputSystem;

//继承PlayerState（状态基类模板）
public class PlayerNormalState : PlayerState
{
    public PlayerNormalState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    // 当玩家进入状态执行
    public override void Enter()
    {
        base.Enter();
        //TODO: 触发 Idle/Run 动画状态切换
        Debug.Log("进入了 Normal 状态！");
    }


    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (Mathf.Abs(stateMachine.MoveInput.x) > 0)
        {
            stateMachine.Anim.PlayRun();
        }
        else
        {
            stateMachine.Anim.PlayIdle();
        }

        //判断“跳跃缓冲池”里是否有剩余时间
        if (stateMachine.CoyoteTimeCounter > 0f && stateMachine.JumpBufferCounter > 0f)
        {
            // 成功触发跳跃前把缓冲池清空
            stateMachine.ConsumeJumpBuffer();//落地瞬间会连续起跳
            stateMachine.ConsumeCoyoteTime();//起跳瞬间还能再跳

            // 在这里瞬间给予向上的速度,起跳的动力由离开地面的这一刻决定,让jump变成纯粹滞空状态
            stateMachine.RB.linearVelocity = new Vector2(stateMachine.RB.linearVelocity.x, stateMachine.jumpForce);

            // 切换到跳跃状态
            stateMachine.ChangeState(stateMachine.JumpState);
        }

        // 当不在地上，且土狼时间也耗尽时，强制切入 JumpState
        if (!stateMachine.IsGrounded() && stateMachine.CoyoteTimeCounter <= 0f)
        {
            stateMachine.ChangeState(stateMachine.JumpState);
        }

        if (stateMachine.dashAction.action.WasPressedThisFrame() && stateMachine.CanDash)
        {
            stateMachine.ChangeState(stateMachine.DashState);
        }
    }

    // 物理相关的更新
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float targetVelocityX = stateMachine.MoveInput.x * stateMachine.moveSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);
    }

    // 当玩家离开这个状态执行
    public override void Exit()
    {
        base.Exit();
        Debug.Log("退出了 Normal 状态！");
    }
}
