using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class AnimationController : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer sr;

    // 1. 性能优化：把所有可能播放的动画名字，提前转换成底层的“数字身份证（Hash）”
    // 这样以后播放时就不需要处理慢吞吞的字符串了！
    private readonly int IDLE = Animator.StringToHash("PlayerIdle");
    private readonly int RUN = Animator.StringToHash("PlayerRun");
    private readonly int JUMP = Animator.StringToHash("PlayerJump");
    private readonly int FALL = Animator.StringToHash("PlayerFall");
    //private readonly int WALL_SLIDE = Animator.StringToHash("PlayerWallSlide");
    private readonly int DASH = Animator.StringToHash("PlayerDash");

    // 记住当前正在播放的动画
    private int currentState;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 由状态机呼叫动画播放
    /// </summary>
    /// <param name="newStateHash">想要播放的动画号</param>
    /// /// <param name="forceRestart">是否无视状态锁定，强制从第0帧重播（针对连续冲刺、连续攻击）</param>
    public void ChangeAnimationState(int newStateHash, bool forceRestart = false)
    {
        if (currentState == newStateHash && !forceRestart) return;
        if (forceRestart)
        {
            anim.Play(newStateHash, -1, 0f);
        }
        else
        {
            anim.Play(newStateHash);
        }

        currentState = newStateHash;
    }

    /// <summary>
    /// 翻转角色身体
    /// </summary>
    public void FlipCharacter(float moveInput)
    {
        if (moveInput > 0)
            sr.flipX = false;
        else if (moveInput < 0)
            sr.flipX = true;
    }

    // 给状态机提供的点单接口

    public void PlayIdle() => ChangeAnimationState(IDLE);
    public void PlayRun() => ChangeAnimationState(RUN);
    public void PlayJump() => ChangeAnimationState(JUMP);
    public void PlayFall() => ChangeAnimationState(FALL);
    //public void PlayWallSlide() => ChangeAnimationState(WALL_SLIDE);
    public void PlayDash() => ChangeAnimationState(DASH, true);

    public void ForcePlayIdle() => ChangeAnimationState(IDLE, true);
}
