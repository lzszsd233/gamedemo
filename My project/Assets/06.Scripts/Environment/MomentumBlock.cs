using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D))]
public class MomentumBlock : MonoBehaviour, IInteractable
{
    [Header("节点引用")]
    public Transform visualTransform;
    public Transform endPoint;

    [Header("动力学参数")]
    public float acceleration = 40f;
    public float maxSpeed = 25f;
    public float returnSpeed = 5f;
    public float endPauseTime = 0.15f;

    public Vector2 CurrentVelocity { get; private set; }
    private Vector2 startPoint;
    private bool isMoving = false;

    private BoxCollider2D col;

    private void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        startPoint = transform.position;
        if (endPoint != null) endPoint.SetParent(null);
    }

    public void Interact(PlayerStateMachine player)
    {
        if (isMoving) return;
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        isMoving = true;
        float currentSpeed = 0f;

        while ((Vector2)transform.position != (Vector2)endPoint.position)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.fixedDeltaTime);
            Vector2 newPos = Vector2.MoveTowards(transform.position, endPoint.position, currentSpeed * Time.fixedDeltaTime);

            Vector2 moveDelta = newPos - (Vector2)transform.position;
            CurrentVelocity = moveDelta / Time.fixedDeltaTime;

            // 【司机核心】：开车前先载客！
            MoveWithPassengers(moveDelta);

            yield return new WaitForFixedUpdate();
        }

        transform.position = endPoint.position;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(endPauseTime);

        CurrentVelocity = Vector2.zero;
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
        isMoving = false;
    }

    private void MoveWithPassengers(Vector2 moveDelta)
    {
        // 1. 【核心修改】：扩大扫描范围！不仅看头顶，连左边、右边、底下的空间全包进去！
        // 比如加宽/加高 0.2f 的探测距离
        Vector2 checkSize = col.size + new Vector2(0.2f, 0.2f);
        Vector2 checkPos = (Vector2)transform.position + col.offset;

        Collider2D[] hitsAround = Physics2D.OverlapBoxAll(checkPos, checkSize, 0);

        HashSet<IRider> passengers = new HashSet<IRider>();

        // 2. 挨个问话
        foreach (var hit in hitsAround)
        {
            IRider rider = hit.GetComponentInParent<IRider>();

            // 如果有车票 + 还没验过票 + 【小恐龙亲口承认抓着我】！
            if (rider != null && !passengers.Contains(rider) && rider.IsRiding(this.transform))
            {
                passengers.Add(rider);

                // 闭气搬运
                col.enabled = false;
                rider.MoveWithPlatform(moveDelta);
                col.enabled = true;
            }
        }

        // 3. 司机自己移动
        transform.position += (Vector3)moveDelta;
        Physics2D.SyncTransforms();
    }
}