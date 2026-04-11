using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 dashDirection;
    private float dashStartTime;
    public PlayerDashState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();

        stateMachine.ConsumeDashBuffer();//进入一定要清空计时器

        //冻结帧
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.Hitstop(0.05f);
        }

        stateMachine.Anim.PlayDash();

        stateMachine.CanDash = false;
        dashStartTime = Time.time;

        dashDirection = stateMachine.MoveInput;

        if (dashDirection == Vector2.zero)
        {
            dashDirection = new Vector2(stateMachine.FacingDir, 0);
        }
        // .normalized 的作用是：确保斜向冲刺时，速度不会比单向快（防止勾股定理导致的加速）
        dashDirection = dashDirection.normalized;

        stateMachine.Speed = dashDirection * stateMachine.dashSpeed;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // ================= 【核心：整合超级跳与凌波微步】 =================
        // 只要玩家在地面上（或者刚好落地），且按下了跳跃键
        if (stateMachine.IsGrounded() && stateMachine.JumpBufferCounter > 0f)
        {
            // 只要冲刺方向有水平分量（纯上/纯下冲刺不能超级跳）
            if (dashDirection.x != 0)
            {
                stateMachine.ConsumeJumpBuffer();
                stateMachine.ConsumeCoyoteTime();

                // 计算从按下冲刺到现在，过了多久？
                float dashTimeElapsed = Time.time - dashStartTime;

                // 【蔚蓝的核心手感窗口】：
                // 1. 如果是冲刺刚开始（比如平地冲刺前 0.15 秒内按跳），这叫 Super Jump，飞得又快又低！
                // 2. 如果是空中斜下冲刺，落地时冲刺时间通常已经过了 0.15 秒，此时按跳，这叫 Wavedash，恢复正常跳跃高度，但保留冲刺极速！

                float superJumpWindow = 0.15f; // 扩大一点窗口，方便测试手感

                if (dashTimeElapsed <= superJumpWindow)
                {
                    // 触发 Super Jump：调用你写好的神级配置方法！
                    stateMachine.JumpState.ConfigureSuperJump(Mathf.Sign(dashDirection.x));
                }
                else
                {
                    // 触发 Wavedash / 普通冲刺跳：不需要压低高度，但依然要给水平极速！
                    // 这里我们直接给速度，不调用 ConfigureSuperJump（因为它会压低 Y 轴）
                    stateMachine.Speed.y = stateMachine.jumpForce;
                    stateMachine.Speed.x = Mathf.Sign(dashDirection.x) * stateMachine.dashSpeed * 2.5f;

                    // 【核心修复】：只要你在地面上按出了这招，不管你刚才在干嘛，
                    // 立刻、马上把你的冲刺次数回满！这就是 Wavedash 的核心收益！
                    stateMachine.CanDash = true;
                }

                // 统一交接给 JumpState 处理物理和动画
                stateMachine.ChangeState(stateMachine.JumpState);
                return;
            }
        }
        // ==================================================================

        if (Time.time >= dashStartTime + stateMachine.dashDuration)
        {
            if (stateMachine.IsGrounded())
                stateMachine.ChangeState(stateMachine.NormalState);
            else
                stateMachine.ChangeState(stateMachine.JumpState);
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (stateMachine.CurrentState != this)
            return;
        stateMachine.Speed = dashDirection * stateMachine.dashSpeed;
    }

    public override void Exit()
    {
        base.Exit();

        // 1. 对于水平方向 (X轴)：冲刺结束时，保留一半惯性，提供极其丝滑的滑行感。
        float newSpeedX = stateMachine.Speed.x * 0.5f;

        // 2. 对于垂直方向 (Y轴)：
        float newSpeedY = stateMachine.Speed.y;

        // 如果我们是向上冲刺（且速度依然向上），结束时必须削减速度！
        // 否则玩家会像火箭一样一直往上飞，破坏跳跃高度的平衡。
        if (dashDirection.y > 0 && stateMachine.Speed.y > 0)
        {
            newSpeedY *= 0.5f;
        }
        // 如果是向下冲刺（Speed.y < 0），我们【绝对不减速】！
        // 保留那股恐怖的下砸动量，让玩家像流星一样砸向地面！
        else if (dashDirection.y < 0)
        {
            newSpeedY = stateMachine.Speed.y;
        }

        // 3. 【防弹簧背刺的终极锁】：
        // 如果我们是被弹簧强制切走的（比如弹簧给了向上的 20 速度），
        // 此时 Speed.y 已经被弹簧改成了 20。
        // 因为 dashDirection.y 不一定是向上的，如果按上面的逻辑，我们可能会错误地衰减掉弹簧的 20！
        // 所以：如果现在的 Y 速度大于冲刺极速，说明肯定是被弹簧之类的外力接管了，我们绝对不碰它！
        if (Mathf.Abs(stateMachine.Speed.y) > stateMachine.dashSpeed)
        {
            newSpeedY = stateMachine.Speed.y;
        }

        // 把精细计算后的“遗产速度”还给状态机，让下一个状态（比如 JumpState）继承！
        stateMachine.Speed = new Vector2(newSpeedX, newSpeedY);
    }
}
