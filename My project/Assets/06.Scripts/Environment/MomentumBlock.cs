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
    public float cooldownTime = 0.3f;

    public Vector2 CurrentVelocity { get; private set; }

    public Vector2 LiftBoost { get; private set; }
    private Vector2 startPoint;
    private bool isMoving = false;

    private enum BlockState { Idle, MovingOut, PausedAtEnd, MovingBack, Cooldown }
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

        yield return new WaitForSeconds(endPauseTime);

        LiftBoost = Vector2.zero;
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

        // 4. 【绝对防穿模核心】：我移过来了，现在谁在我的体内，我就把谁硬推出去！
        // 重新扫描方块现在的内部
        Collider2D[] hitsInside = Physics2D.OverlapBoxAll((Vector2)transform.position + col.offset, col.size, 0);
        foreach (var hit in hitsInside)
        {
            IRider rider = hit.GetComponentInParent<IRider>();
            // 如果你在我体内，且你不是主动跟着我走的乘客
            if (rider != null && !passengers.Contains(rider))
            {
                // 强行把你往我移动的方向推！
                // 如果推不动（背后有墙），WillBeCrushed 会返回 true 并触发你的死亡！
                if (!rider.WillBeCrushed(moveDelta))
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

}