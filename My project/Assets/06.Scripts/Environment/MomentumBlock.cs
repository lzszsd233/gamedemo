using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class MomentumBlock : MonoBehaviour, IInteractable, IResettable
{
    [Header("节点引用")]
    public Transform visualTransform;
    public Transform endPoint;

    [Header("动力学参数")]
    public float maxSpeed = 30f;
    public float returnSpeed = 10f;
    public float endPauseTime = 0.2f;

    public float liftBoostWindow = 0.1f;

    public float cooldownTime = 0.3f;

    public float startDelayTime = 0.5f;

    public Vector2 CurrentVelocity { get; private set; }

    public Vector2 LiftBoost { get; private set; }
    private Vector2 startPoint;

    private enum BlockState { Idle, Preparing, MovingOut, PausedAtEnd, MovingBack, Cooldown }
    private BlockState currentState = BlockState.Idle;

    private BoxCollider2D col;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        startPoint = transform.position;
        if (endPoint != null) endPoint.SetParent(null);
    }

    public void Interact(PlayerStateMachine player)
    {
        if (currentState == BlockState.Idle)
        {
            StartCoroutine(SequenceRoutine());
        }
    }

    private void Update()
    {
        // 如果方块在休息，且小恐龙正踩在/抓在我身上，立刻发车！
        if (currentState == BlockState.Idle && CheckPlayerRiding())
        {
            StartCoroutine(SequenceRoutine());
        }
    }

    private IEnumerator SequenceRoutine()
    {
        // ================= 0. 发车前摇 (抖动预警) =================
        currentState = BlockState.Preparing;

        float delayTimer = 0f;
        Vector3 originalVisualPos = visualTransform.localPosition;

        while (delayTimer < startDelayTime)
        {
            // 【核心加入：中途下车判定！】
            // 每一帧都扫描一次，如果玩家离开了，立刻踩死刹车！
            if (!CheckPlayerRiding())
            {
                // 玩家跑了！取消发车！
                if (visualTransform != null) visualTransform.localPosition = originalVisualPos;
                currentState = BlockState.Idle; // 恢复闲置状态
                yield break; // 【神级指令】：直接终止并退出这个协程！后面的代码全都不执行了！
            }

            // 制造视觉上的剧烈抖动！(注意：只抖动图片 visualTransform，不抖动碰撞体，防止玩家掉下去)
            if (visualTransform != null)
            {
                visualTransform.localPosition = originalVisualPos + (Vector3)(Random.insideUnitCircle * 0.05f);
            }

            delayTimer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        // 抖动结束，图片归位
        if (visualTransform != null) visualTransform.localPosition = originalVisualPos;


        currentState = BlockState.MovingOut;
        LiftBoost = Vector2.zero;

        while ((Vector2)transform.position != (Vector2)endPoint.position)
        {
            Vector2 newPos = Vector2.MoveTowards(transform.position, endPoint.position, maxSpeed * Time.fixedDeltaTime);
            Vector2 moveDelta = newPos - (Vector2)transform.position;
            CurrentVelocity = moveDelta / Time.fixedDeltaTime; // 记录极速

            MoveWithPassengers(moveDelta);
            yield return new WaitForFixedUpdate();
        }

        transform.position = endPoint.position;
        Physics2D.SyncTransforms();
        currentState = BlockState.PausedAtEnd;

        CurrentVelocity = Vector2.zero;

        Vector2 burstDirection = ((Vector2)endPoint.position - startPoint).normalized;
        LiftBoost = burstDirection * maxSpeed;

        yield return new WaitForSeconds(liftBoostWindow);
        LiftBoost = Vector2.zero;

        float remainingPauseTime = endPauseTime - liftBoostWindow;
        if (remainingPauseTime > 0)
        {
            yield return new WaitForSeconds(remainingPauseTime);
        }


        currentState = BlockState.MovingBack;

        while ((Vector2)transform.position != startPoint)
        {
            Vector2 newPos = Vector2.MoveTowards(transform.position, startPoint, returnSpeed * Time.fixedDeltaTime);
            Vector2 moveDelta = newPos - (Vector2)transform.position;
            CurrentVelocity = moveDelta / Time.fixedDeltaTime;

            MoveWithPassengers(moveDelta);
            yield return new WaitForFixedUpdate();
        }

        transform.position = startPoint;
        Physics2D.SyncTransforms();
        CurrentVelocity = Vector2.zero;
        currentState = BlockState.Cooldown;

        yield return new WaitForSeconds(cooldownTime);

        currentState = BlockState.Idle;
    }

    private bool CheckPlayerRiding()
    {
        // 在自己周围扫一圈（包括头上、脚下、四周）
        Vector2 checkSize = col.size + new Vector2(0.2f, 0.2f);
        Vector2 checkPos = (Vector2)transform.position + col.offset;
        Collider2D[] hitsAround = Physics2D.OverlapBoxAll(checkPos, checkSize, 0);

        foreach (var hit in hitsAround)
        {
            IRider rider = hit.GetComponentInParent<IRider>();
            // 如果扫到了乘客，且乘客亲口承认（IsRiding）正贴在我身上，返回 true！
            if (rider != null && rider.IsRiding(this.transform))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 司机搬运乘客，并强行推开挡路的家伙！
    /// </summary>
    private void MoveWithPassengers(Vector2 moveDelta)
    {
        // 1. 扫描所有可能碰到的人（头顶、脚下、四周加宽）
        Vector2 checkSize = col.size + new Vector2(0.2f, 0.2f);
        Vector2 checkPos = (Vector2)transform.position + col.offset;
        Collider2D[] hitsAround = Physics2D.OverlapBoxAll(checkPos, checkSize, 0);

        HashSet<IRider> passengers = new HashSet<IRider>();

        foreach (var hit in hitsAround)
        {
            IRider rider = hit.GetComponentInParent<IRider>();
            if (rider != null && !passengers.Contains(rider) && rider.IsRiding(this.transform))
            {
                passengers.Add(rider);
                col.enabled = false;
                rider.MoveWithPlatform(moveDelta);
                col.enabled = true;
            }
        }

        transform.position += (Vector3)moveDelta;
        Physics2D.SyncTransforms();

        // 4. 【绝对致命的碾压判定】
        // 我们不缩小雷达了！我们就用方块极其真实的、一模一样大小的碰撞盒去扫！
        Vector2 crushCheckSize = col.size - new Vector2(0.05f, 0.05f);
        Collider2D[] hitsInside = Physics2D.OverlapBoxAll((Vector2)transform.position + col.offset, crushCheckSize, 0);

        foreach (var hit in hitsInside)
        {
            IRider rider = hit.GetComponentInParent<IRider>();
            if (rider != null)
            {
                // 【核心修正】：不再跳过乘客！所有在体内的家伙都要查！
                // 但是！我们要把“我自己(this.transform)”作为 pusher 传给乘客去查！
                if (rider.WillBeCrushed(moveDelta, this.transform))
                {
                    // 如果他真的撞到了“除了我之外”的其他墙，让他去死
                    continue;
                }

                // 如果他没撞到其他墙（比如站在我顶上往下走），那我就只是温柔地把他推开
                if (!passengers.Contains(rider))
                {
                    col.enabled = false;
                    rider.MoveWithPlatform(moveDelta);
                    col.enabled = true;
                }
            }
        }
    }

    public void ResetState()
    {
        StopAllCoroutines();

        // 强行搬回老家
        transform.position = startPoint;
        Physics2D.SyncTransforms();

        // 状态清零
        CurrentVelocity = Vector2.zero;
        LiftBoost = Vector2.zero;
        currentState = BlockState.Idle;
    }

    public Vector2 GetOriginalPosition()
    {
        return startPoint;
    }

}