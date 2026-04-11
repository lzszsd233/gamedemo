using UnityEngine;

/// <summary>
/// 可重置机关接口
/// 所有在玩家离开房间后需要恢复原状的物体（崩塌平台、草莓、冲刺水晶等），都必须继承此接口。
/// </summary>
public interface IResettable
{
    /// <summary>
    /// 房间大管家会在玩家离开或死亡时，强制呼叫这个方法！
    /// </summary>
    void ResetState();

    Vector2 GetOriginalPosition();
}
