using UnityEngine;

/// <summary>
/// 致命危险物接口 (所有碰到立刻死亡的物体，都必须继承此接口)
/// </summary>
public interface IHazard
{
    // 目前我们不需要它具体做什么，它本身就是一张“资格证”。
    // 只要一个脚本挂了这个接口，玩家碰到它就会死。
}