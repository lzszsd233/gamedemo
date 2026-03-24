using UnityEngine;

public class Spring : MonoBehaviour, IInteractable
{
    [Header("弹簧设置")]
    public float bounceForce = 20f; // 弹射的力度

    // 可以在这里加个特效预制体，或者简单的动画变量
    // public Animator anim;

    public void Interact(PlayerStateMachine player)
    {
        player.Speed = Vector2.zero;
        player.Speed = transform.up * bounceForce;

        player.CanDash = true;
        player.ChangeState(player.JumpState);
    }
}
