using UnityEngine;
using System.Collections;

public class PlayerDieState : PlayerState
{
    private DeathType currentDeathType;

    public PlayerDieState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }
    public void ConfigureDeath(DeathType type)
    {
        currentDeathType = type;
    }

    public override void Enter()
    {
        base.Enter();

        float bounceDirX = Random.Range(-1f, 1f);
        float bounceForceY = 8f;

        stateMachine.Speed = new Vector2(bounceDirX * 5f, bounceForceY);

        // stateMachine.Anim.Play("Player_Hurt");

        stateMachine.StartCoroutine(DeathSequenceCoroutine());
    }

    // 将原本大管家里的协程直接搬到这里
    private IEnumerator DeathSequenceCoroutine()
    {
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(true);
        if (stateMachine.impulseSource != null) stateMachine.impulseSource.GenerateImpulse();

        // 2. 尸体弹飞延时 (完全遵守你的 0.15 秒)
        yield return new WaitForSeconds(0.15f);

        // 3. 速度清零，隐藏角色
        stateMachine.Speed = Vector2.zero;
        stateMachine.Anim.GetComponent<SpriteRenderer>().enabled = false;

        // 4. 严格按照你原本的时间节点，在角色隐藏后播放对应死法特效
        switch (currentDeathType)
        {
            case DeathType.Spike:
                PlaySpikeDeathEffect();
                break;
            case DeathType.Crush:
                PlayCrushDeathEffect();
                break;
            case DeathType.FallVoid:
                PlayFallVoidDeathEffect();
                break;
        }

        yield return new WaitForSeconds(0.4f);

        // 呼叫转场管家，准备黑屏复活
        TransitionManager.Instance.StartTransition(() =>
        {
            stateMachine.transform.position = stateMachine.currentCheckpoint;
            stateMachine.Speed = Vector2.zero;
            stateMachine.StartCoroutine(RespawnSequenceCoroutine());
        });
    }

    private IEnumerator RespawnSequenceCoroutine()
    {
        yield return new WaitForSeconds(0.4f);

        if (stateMachine.respawnParticlesPrefab != null)
        {
            Object.Instantiate(stateMachine.respawnParticlesPrefab, stateMachine.transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(0.3f);

        stateMachine.Anim.GetComponent<SpriteRenderer>().enabled = true;
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(false);

        stateMachine.ChangeState(stateMachine.NormalState);
    }

    private void PlaySpikeDeathEffect()
    {
        if (stateMachine.deathParticlesPrefab != null)
        {
            Object.Instantiate(stateMachine.deathParticlesPrefab, stateMachine.transform.position, Quaternion.identity);
        }
    }

    private void PlayCrushDeathEffect()
    {
        // 比如在这里写：变成肉饼的缩放动画、播放骨折音效
        Debug.Log("播放【变成肉饼】的特效！");
    }

    private void PlayFallVoidDeathEffect()
    {
        // 比如在这里写：角色持续缩小、播放渐远的惨叫声
        Debug.Log("播放【掉落深渊】的惨叫声！");
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
