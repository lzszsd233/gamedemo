using System;
using UnityEngine;

/// <summary>
/// 全局事件总线：所有解耦通信的核心枢纽
/// </summary>
public static class EventBus
{
    // 定义死法枚举 (把它从 PlayerDieState 移到这里，因为它是全服通用的数据)
    public enum DeathType { Spike, Crush, FallVoid }

    // 频道 1：玩家死亡的绝对瞬间（用于触发震动、音效等即时反馈）
    public static event Action<DeathType> OnPlayerDied;

    // 提供给玩家用来“喊话”的方法
    public static void PublishPlayerDied(DeathType type)
    {
        OnPlayerDied?.Invoke(type);
    }

    // 【新增频道 2】：玩家死亡动画（爆浆小球）彻底播完的时刻！（用于触发黑幕转场）
    public static event Action OnPlayerDeathAnimationFinished;
    public static void PublishPlayerDeathAnimationFinished() => OnPlayerDeathAnimationFinished?.Invoke();
}