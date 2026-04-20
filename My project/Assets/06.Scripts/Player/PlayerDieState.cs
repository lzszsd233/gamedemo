using UnityEngine;
using System.Collections;

public class PlayerDieState : PlayerState
{
    private EventBus.DeathType currentDeathType;

    public PlayerDieState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public void ConfigureDeath(EventBus.DeathType type)
    {
        currentDeathType = type;
    }

    public override void Enter()
    {
        base.Enter();

        float bounceDirX = Random.Range(-1f, 1f);
        float bounceForceY = 8f;

        stateMachine.Speed = new Vector2(bounceDirX * 5f, bounceForceY);

        Debug.Log("【广播站】：大喇叭正在广播玩家死亡！死因是：" + currentDeathType);

        // 2. 【高潮时刻：引爆广播！】
        // 这一嗓子喊出去，转场管家去拉黑幕，关卡管家去重置房间，完全不用我操心！
        EventBus.PublishPlayerDied(currentDeathType);

        // 3. 执行自己那点可怜的动画
        stateMachine.StartCoroutine(SelfDeathAnimationCoroutine());
    }

    // 死亡状态现在只管小恐龙自己的尸体怎么碎！
    private IEnumerator SelfDeathAnimationCoroutine()
    {
        yield return new WaitForSeconds(0.15f);

        stateMachine.Speed = Vector2.zero;
        stateMachine.Anim.GetComponent<SpriteRenderer>().enabled = false;

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
