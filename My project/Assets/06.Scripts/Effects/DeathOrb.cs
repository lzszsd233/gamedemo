using UnityEngine;

/// <summary>
/// 死亡爆出/重生聚拢的单颗碎片
/// </summary>
public class DeathOrb : MonoBehaviour
{
    private Vector2 moveDirection; // 飞行的方向
    private float moveSpeed;       // 飞行的速度
    private float lifeTime;        // 能活多久
    private float timer;           // 计时器

    private Vector3 initialScale;  // 刚出生时的大小

    private bool isReverse = false;

    /// <summary>
    /// 初始化这颗碎片的飞行参数
    /// </summary>
    /// /// <param name="reverse">如果为true，碎片将从远处向中心飞</param>
    public void Init(Vector2 dir, float speed, float time, bool reverse = false)
    {
        moveDirection = dir.normalized; // 确保方向是单位向量
        moveSpeed = speed;
        lifeTime = time;
        timer = 0f;
        initialScale = transform.localScale;
        isReverse = reverse;

        if (isReverse)
        {
            // 1. 把它出生点移到目标位置的“外面” (根据飞行时间和速度反推它的起点)
            transform.position += (Vector3)(moveDirection * moveSpeed * lifeTime);

            // 2. 让它的飞行方向反转（从外面往中心飞）
            moveDirection = -moveDirection;
        }
    }

    private void Update()
    {
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime);

        timer += Time.deltaTime;
        float progress = timer / lifeTime;

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
        else
        {
            if (isReverse)
            {
                // 重生：时光倒流，由小变大 (从 0 变回 initialScale)
                //transform.localScale = Vector3.Lerp(Vector3.zero, initialScale, progress);
            }
            else
            {
                // 死亡：由大变小 (从 initialScale 变成 0)
                //transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, progress);
            }
        }
    }
}
