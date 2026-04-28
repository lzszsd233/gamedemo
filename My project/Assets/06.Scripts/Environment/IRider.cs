using UnityEngine;

/// <summary>
/// 乘客接口 (车票)
/// 任何可以站在移动平台上，或者被移动平台推着走的物体，都必须实现此接口。
/// </summary>
public interface IRider
{
    /// <summary>
    /// 平台主动呼叫乘客：我要移动了，你跟我走！
    /// </summary>
    void MoveWithPlatform(Vector2 delta);

    /// <summary>
    /// 平台主动呼叫乘客：我要往你这边挤了，你拿碰撞盒扫一下，看看背后有没有墙？
    /// </summary>
    /// <returns>如果撞墙了返回 true（代表会被挤死）</returns>
    bool WillBeCrushed(Vector2 delta, Transform pusher);

    bool IsRiding(Transform platform);
}