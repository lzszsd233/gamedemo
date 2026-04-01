using UnityEngine;
using System.Collections;

public class FallingBlock : MonoBehaviour, IInteractable, IResettable
{
    [Header("引用设置")]
    public Transform visualTransform;

    [Header("崩塌参数")]
    public float shakeDuration = 0.5f;
    public float shakeIntensity = 0.05f;
    public float fallSpeed = 15f;
    public float fallDuration = 2f;
    public float respawnTime = 3f;

    // 只需要记录自己是不是已经被踩了，防止重复触发
    private bool isTriggered = false;
    private Vector2 originalPosition;

    // 获取身上所有的碰撞体（实心的和触发器都要管）
    private Collider2D[] colliders;

    private void Awake()
    {
        originalPosition = transform.position;
        colliders = GetComponents<Collider2D>();
    }

    public void Interact(PlayerStateMachine player)
    {
        // 如果已经被踩过了，就不理会玩家的雷达
        if (isTriggered) return;

        isTriggered = true;
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        float shakeTimer = 0f;
        while (shakeTimer < shakeDuration)
        {
            Vector2 randomOffset = Random.insideUnitCircle * shakeIntensity;
            visualTransform.localPosition = randomOffset;//position 是世界坐标，不能用，要用localposition
            shakeTimer += Time.deltaTime;
            yield return null;
        }
        visualTransform.localPosition = Vector3.zero;

        // 阶段 2：坠入深渊
        float fallTimer = 0f;
        while (fallTimer < fallDuration)
        {
            // 自己往下掉
            transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
            fallTimer += Time.deltaTime;
            yield return null;
        }


        foreach (var col in colliders) col.enabled = false;
        visualTransform.gameObject.SetActive(false);

        yield return new WaitForSeconds(respawnTime);


        transform.position = originalPosition;

        foreach (var col in colliders) col.enabled = true;
        visualTransform.gameObject.SetActive(true);

        isTriggered = false;
    }

    public void ResetState()
    {
        // 1. 如果它正在崩塌或者抖动，立刻掐断它的协程！
        StopAllCoroutines();

        // 2. 瞬间把自己瞬移回最初记录的位置
        transform.position = originalPosition;
        visualTransform.localPosition = Vector3.zero;

        // 3. 重新开启所有的碰撞体和视觉图片
        foreach (var col in colliders) col.enabled = true;
        visualTransform.gameObject.SetActive(true);

        // 4. 重置触发锁，允许玩家再次踩上来
        isTriggered = false;

    }
}
