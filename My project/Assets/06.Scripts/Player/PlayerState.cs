using UnityEngine;

public class PlayerState//状态模板
{
    protected PlayerStateMachine stateMachine;

    public PlayerState(PlayerStateMachine stateMachine)
    {
        this.stateMachine = stateMachine;
    }

    // 状态开始时执行一次（类似 Start）
    public virtual void Enter() { }

    // 状态进行时每一帧执行（类似 Update）
    public virtual void LogicUpdate() { }

    // 状态进行时物理更新（类似 FixedUpdate）
    public virtual void PhysicsUpdate() { }

    // 状态结束时执行一次（比如从冲刺切换回普通时，清理一下特效）
    public virtual void Exit() { }
}
