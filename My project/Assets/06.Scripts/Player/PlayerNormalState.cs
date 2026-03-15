using UnityEngine;
using UnityEngine.InputSystem;

//它继承自PlayerState（状态基类模板）
public class PlayerNormalState : PlayerState
{
    public PlayerNormalState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    // 当玩家进入这个状态时（比如从冲刺切换回普通时），执行一次
    public override void Enter()
    {
        base.Enter();
        //TODO: 触发 Idle/Run 动画状态切换
        Debug.Log("进入了 Normal 状态！");
    }


    public override void LogicUpdate()
    {
        base.LogicUpdate();


        if (stateMachine.IsGrounded() && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("跳跃指令已触发！");
            stateMachine.ChangeState(stateMachine.JumpState);
        }
        // 以后这里可以写：如果按了冲刺键，就告诉老板切换到 DashState！
        // if (Input.GetButtonDown("Dash")) 
        //     stateMachine.ChangeState(stateMachine.DashState);
    }

    // 物理相关的更新
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        float targetVelocityX = stateMachine.MoveInput.x * stateMachine.moveSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);
    }

    // 当玩家离开这个状态时（比如起跳了、冲刺了），执行一次
    public override void Exit()
    {
        base.Exit();
        Debug.Log("退出了 Normal 状态！");
    }
}
