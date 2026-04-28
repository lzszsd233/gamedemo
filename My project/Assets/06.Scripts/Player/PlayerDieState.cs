using UnityEngine;
using System.Collections;

public class PlayerDieState : PlayerState
{
    private EventBus.DeathType currentDeathType;

    // 【新增】：记录被挤压的方向
    private Vector2 crushDirection;

    public PlayerDieState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public void ConfigureDeath(EventBus.DeathType type)
    {
        currentDeathType = type;
    }

    // 【新增】：专属的挤死配置方法
    public void ConfigureCrushDeath(Vector2 direction)
    {
        currentDeathType = EventBus.DeathType.Crush;
        crushDirection = direction.normalized; // 拿到单位方向
    }

    public override void Enter()
    {
        base.Enter();

        // 1. 【区别对待】：如果是被挤死的，绝对不准弹飞！必须死死钉在原地！
        if (currentDeathType == EventBus.DeathType.Crush)
        {
            stateMachine.Speed = Vector2.zero;

            // 【核心魔法：根据方向智能变形！】
            // 如果是左右挤压（X 轴力更大）
            if (Mathf.Abs(crushDirection.x) > Mathf.Abs(crushDirection.y))
            {
                // 变成一张贴在墙上的海报：X极窄，Y被挤得拉长
                stateMachine.Anim.transform.localScale = new Vector3(0.2f, 1.5f, 1f);
            }
            // 如果是上下挤压（Y 轴力更大）
            else
            {
                // 变成一张贴在地上的煎饼：X拉宽，Y极扁
                stateMachine.Anim.transform.localScale = new Vector3(1.5f, 0.2f, 1f);
            }

            // 【核心魔法】：不仅变红，而且如果在 EffectManager 顿帧期间，
            // 确保肉饼颜色极其鲜艳！
            SpriteRenderer sr = stateMachine.Anim.GetComponent<SpriteRenderer>();
            sr.color = Color.red;
        }
        else
        {
            // 其他死法（如尖刺），正常弹飞
            float bounceDirX = Random.Range(-1f, 1f);
            stateMachine.Speed = new Vector2(bounceDirX * 5f, 8f);
        }

        // 2. 【高潮时刻：引爆广播！】
        // 这一嗓子喊出去，转场管家去拉黑幕，关卡管家去重置房间，完全不用我操心！
        EventBus.PublishPlayerDied(currentDeathType);

        // 3. 执行自己那点可怜的动画
        stateMachine.StartCoroutine(SelfDeathAnimationCoroutine());
    }

    // 死亡状态现在只管小恐龙自己的尸体怎么碎！
    private IEnumerator SelfDeathAnimationCoroutine()
    {
        // 1. 尸体滞留时间
        if (currentDeathType == EventBus.DeathType.Crush)
        {
            // 被挤死时：让“肉饼形态”在屏幕上凄惨地定格 0.2 秒
            // 配合外面方块无情开过去的动作，视觉冲击力极强
            yield return new WaitForSeconds(0.2f);
        }
        else
        {
            // 尖刺死：尸体弹飞 0.15 秒
            yield return new WaitForSeconds(0.15f);
        }

        stateMachine.Speed = Vector2.zero;
        stateMachine.Anim.GetComponent<SpriteRenderer>().enabled = false;

        // 【极其重要的善后】：一定要把小恐龙的形状和颜色恢复原状！
        // 否则复活出来的小恐龙还是个肉饼！
        stateMachine.Anim.transform.localScale = Vector3.one;
        stateMachine.Anim.GetComponent<SpriteRenderer>().color = Color.white;

        // 【新增细节】：如果是虚空死亡，不爆小球！让他默默消失在黑暗中
        if (currentDeathType != EventBus.DeathType.FallVoid)
        {
            if (stateMachine.deathParticlesPrefab != null)
            {
                Object.Instantiate(stateMachine.deathParticlesPrefab, stateMachine.transform.position, Quaternion.identity);
            }
        }

        // 死等小球飞完！(假设小球寿命是 0.5 秒，这里等 0.6 秒)
        yield return new WaitForSeconds(0.2f);

        // 【高潮时刻：第二声广播！】
        // 小球飞完了，画面干净了！
        // 拿着大喇叭喊：“我碎干净了！收尸队（转场管家）可以上场了！”
        EventBus.PublishPlayerDeathAnimationFinished();
    }
    public override void LogicUpdate()
    {
        base.LogicUpdate();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        //stateMachine.Speed.y -= stateMachine.customGravity * Time.fixedDeltaTime;
    }

    public override void Exit()
    {
        base.Exit();
    }
}
