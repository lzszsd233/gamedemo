using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private float jumpForce = 12f; // 起跳力度

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        // 瞬间给予向上的速度
        stateMachine.RB.linearVelocity = new Vector2(stateMachine.RB.linearVelocity.x, jumpForce);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // 当垂直速度小于0，说明开始下落，切回普通状态
        if (stateMachine.RB.linearVelocity.y <= 0)
        {
            stateMachine.ChangeState(stateMachine.NormalState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // 这里可以保留移动能力，所以还是写上水平移动逻辑
        float targetVelocityX = stateMachine.MoveInput.x * stateMachine.moveSpeed;
        stateMachine.RB.linearVelocity = new Vector2(targetVelocityX, stateMachine.RB.linearVelocity.y);
    }
}
