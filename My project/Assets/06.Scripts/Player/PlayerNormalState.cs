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
        if (stateMachine.DashBufferCounter > 0f && stateMachine.CanDash)
        {
            // 如果在按冲刺的同一帧（或者跳跃缓冲池里有指令），他按了跳跃
            if (stateMachine.JumpBufferCounter > 0f)
            {
                stateMachine.ConsumeDashBuffer();
                stateMachine.ConsumeJumpBuffer(); // 消耗跳跃输入

                // 计算方向，配置跳跃状态，然后切换
                float dir = stateMachine.MoveInput.x != 0 ? Mathf.Sign(stateMachine.MoveInput.x) : stateMachine.FacingDir;
                stateMachine.JumpState.ConfigureSuperJump(dir);
                stateMachine.ChangeState(stateMachine.JumpState);
                return;
            }

            // 如果没按跳，才老老实实进入普通冲刺状态
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

            // 2. 【核心修复】：看看脚下踩的是不是动量方块？
            Collider2D ground = stateMachine.GetGroundCollider();
            if (ground != null)
            {
                MomentumBlock block = ground.GetComponentInParent<MomentumBlock>();
                if (block != null)
                {
                    // 3. 把方块的速度【叠加】到自己身上！
                    // 如果方块往上飞(25)，你(16)起跳后速度就是 41！方块永远追不上你！
                    stateMachine.Speed += block.CurrentVelocity;
                }
            }

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

        // 算出玩家期望的目标速度
        float targetSpeedX = stateMachine.MoveInput.x * stateMachine.moveSpeed;

        // 设定地面加速度。数值越大，起步和刹车越快
        float groundAcceleration = 100f;

        //修改Speed的值
        // 让当前速度，以地面加速度，平滑地趋近于目标速度
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
