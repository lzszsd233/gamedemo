using UnityEngine;

public class Spring : MonoBehaviour, IInteractable
{
    [Header("弹簧设置")]
    public float bounceForce = 20f; // 弹射的力度

    // 可以在这里加个特效预制体，或者简单的动画变量
    // public Animator anim;

    public void Interact(PlayerStateMachine player)
    {
        player.CanDash = true;
        player.ChangeState(player.JumpState);

        player.Speed = new Vector2(player.Speed.x, 0f);
        player.Speed += (Vector2)transform.up * bounceForce;

        player.transform.position += (Vector3)transform.up * 0.1f;

        player.SetActionLock(0.05f);
    }
}
