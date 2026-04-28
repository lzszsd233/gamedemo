using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 全局特效大管家
/// 专职处理全屏特效、震动、以及未来可能加入的全局音效
/// </summary>
public class EffectManager : MonoBehaviour
{
    public static EffectManager Instance { get; private set; }

    [Header("震动发生器")]
    public CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 自动获取身上的震动组件
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnEnable()
    {
        EventBus.OnPlayerDied += HandlePlayerDeathEffects;
    }

    private void OnDisable()
    {
        EventBus.OnPlayerDied -= HandlePlayerDeathEffects;
    }

    // ================== 【核心业务处理】 ==================

    /// <summary>
    /// 听到玩家死亡广播后，自动执行的逻辑
    /// </summary>
    private void HandlePlayerDeathEffects(EventBus.DeathType deathType)
    {

        if (deathType != EventBus.DeathType.Crush)
        {
            if (impulseSource != null)
            {
                // 触发屏幕震动（如果你想区分震动力度，可以传不同的数字，比如 2f, 5f）
                impulseSource.GenerateImpulse();
            }
        }
        else
        {
            // 【绝杀表现】：被方块挤死时，虽然不震屏，但让全世界的时间瞬间静止 0.1 秒！
            // 此时方块停住了，小恐龙变成了肉饼。
            // 0.1 秒后，时间恢复，方块无情地从小恐龙的尸体上碾过去！
            if (TransitionManager.Instance != null)
            {
                TransitionManager.Instance.Hitstop(0.1f);
            }

            // TODO: 以后加了 AudioManager，可以在这里写：
            // if (deathType == EventBus.DeathType.Void) AudioManager.Play("FallScream");
            // else AudioManager.Play("DeathSplat");
        }

        // 以后还可以加 HandlePlayerDash, HandleStrawberryCollected 等各种特效逻辑！
    }
}
