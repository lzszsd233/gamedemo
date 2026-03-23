using UnityEngine;

public interface IInteractable
{
    // 强制要求：所有机关必须实现这个“互动”方法。
    // 并且要求把PlayerStateMachine作为参数传进来，方便机关对玩家动手脚
    void Interact(PlayerStateMachine player);
}
