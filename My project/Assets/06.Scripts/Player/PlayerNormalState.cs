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

        //把冲刺判定放在跳跃判定的上方并且加上 return
        // 这样如果同时按下，永远优先切入 DashState
        if (stateMachine.dashAction.action.WasPressedThisFrame() && stateMachine.CanDash)
        {
            stateMachine.ChangeState(stateMachine.DashState);
            return;
        }

        //判断“跳跃缓冲池”里是否有剩余时间
        if (stateMachine.CoyoteTimeCounter > 0f && stateMachine.JumpBufferCounter > 0f)
        {
            // 成功触发跳跃前把缓冲池清空
            stateMachine.ConsumeJumpBuffer();//落地瞬间会连续起跳
            stateMachine.ConsumeCoyoteTime();//起跳瞬间还能再跳

            stateMachine.Speed.y = stateMachine.jumpForce;

            // 切换到跳跃状态
            stateMachine.ChangeState(stateMachine.JumpState);
        }

        // 当不在地上，且土狼时间也耗尽时，强制切入 JumpState
        if (!stateMachine.IsGrounded() && stateMachine.CoyoteTimeCounter <= 0f)
        {
            stateMachine.ChangeState(stateMachine.JumpState);
        }
    }

    // 物理相关的更新
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 【修改这里】：算出玩家期望的目标速度
        float targetSpeedX = stateMachine.MoveInput.x * stateMachine.moveSpeed;

        // 设定地面加速度。数值越大，起步和刹车越快（100f 手感比较紧凑，类似蔚蓝；如果调小就会像冰面）
        float groundAcceleration = 100f;

        // 【核心魔法】：让当前速度，以地面加速度，平滑地趋近于目标速度
        stateMachine.Speed.x = Mathf.MoveTowards(stateMachine.Speed.x, targetSpeedX, groundAcceleration * Time.fixedDeltaTime);

        // 模拟重力
        stateMachine.Speed.y -= stateMachine.customGravity * Time.fixedDeltaTime;
    }

    // 当玩家离开这个状态执行
    public override void Exit()
    {
        base.Exit();
    }
}
