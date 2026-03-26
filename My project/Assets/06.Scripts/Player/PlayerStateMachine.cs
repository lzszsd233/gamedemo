using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;

public class PlayerStateMachine : MonoBehaviour
{
    #region  1.变量与属性(Variables & Properties)
    // 当前正在运行的状态
    public PlayerState CurrentState { get; private set; }//属性（Property）
    public PlayerNormalState NormalState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }
    public PlayerWallSlideState WallSlideState { get; private set; }
    public PlayerDashState DashState { get; private set; }
    public PlayerDieState DieState { get; private set; }
    public PlayerClimbState ClimbState { get; private set; }

    //private set：意思是私有修改。只有当前这个脚本才有权力修改这个变量的值。别的脚本绝对改不了这个变量的值，只能读取它。这样做的好处是，外部脚本只能获取到状态实例，但不能随意修改它们，保证了状态的完整性和安全性。    //get：意思是允许读取。别的脚本可以获取到这个变量的值

    [Header("组件引用")]
    public Rigidbody2D RB { get; private set; }
    public AnimationController Anim { get; private set; }
    public CinemachineImpulseSource impulseSource;

    [Header("视觉特效")]
    public GameObject deathParticlesPrefab;
    public GameObject respawnParticlesPrefab;
    [Header("输入")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction; //跳跃插座
    public InputActionReference dashAction; //冲刺插座
    public InputActionReference grabAction;
    public Vector2 MoveInput { get; private set; }

    [Header("自定义物理引擎")]
    // 所有状态修改这个 Speed
    public Vector2 Speed;
    // 亚像素累加器
    private Vector2 positionRemainder;
    // 碰撞盒，用于计算包围盒（AABB）射线
    public BoxCollider2D col { get; private set; }
    // ======================================================

    [Header("移动配置")]
    public float moveSpeed = 8f;
    public float FacingDir { get; private set; } = 1f; // 1是右，-1是左，默认朝右

    [Header("跳跃手感参数")]
    public float jumpForce = 16f;          // 起跳力度
    public float fallGravityMult = 1.5f;   // 下落时的重力倍数（越大掉得越快）
    public float jumpCutMult = 0.5f;       // 松开跳跃键时，速度保留的比例
    public float customGravity = 30f;    // 自定义物理的重力加速度
    public float maxFallSpeed = -20f;    // 最大下落速度

    [Header("蹬墙跳参数")]
    public float wallJumpForceY = 16f; // 蹬墙向上弹的力（通常比普通跳稍小一点）
    public float wallJumpForceX = 12f; // 蹬墙向反方向弹开的力
    public float wallJumpDuration = 0.15f; // 蹬墙后，锁死玩家输入的时间（极短，制造惯性硬直）

    [Header("冲刺配置")]
    public float dashSpeed = 24f;      // 冲刺爆发速度
    public float dashDuration = 0.2f;  // 冲刺持续时间
    public bool CanDash { get; set; }  // 当前是否可以冲刺（充能标识）

    [Header("攀爬配置")]
    public float climbSpeed = 4f;        // 爬墙速度
    public float maxStamina = 110f;      // 最大体力值
    public float climbStaminaCost = 45f; // 向上爬每秒消耗的体力
    public float holdStaminaCost = 10f;  // 挂在墙上不动每秒消耗的体力
    public float GrabCooldownCounter { get; private set; } // 抓取冷却器
    public float CurrentStamina { get; set; }

    [Header("宽容度机制")]
    public float jumpBufferTime = 0.15f; // 记住按键的时间
    public float JumpBufferCounter { get; private set; } // 倒计时器
    public float coyoteTime = 0.15f; // 土狼时间
    public float CoyoteTimeCounter { get; private set; }

    [Header("地面检测参数")]
    public LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.5f;

    [Header("墙壁交互参数")]
    public float wallCheckDistance = 0.55f; // 摸墙射线的长度
    public float wallSlideSpeed = 2f;       // 往下滑的最大速度（比自由落体慢）
    // 蹬墙跳硬直机制
    public float WallJumpLockCounter { get; private set; }
    public float WallJumpDirection { get; private set; } // 记录蹬墙跳的方向（-1或1）

    [Header("关卡机制")]
    public Vector2 currentCheckpoint; // 当前记录的重生点坐标
    public float respawnDelay = 1f;   // 死亡后多久复活

    [Header("转场状态")]
    public bool IsTransitioning { get; set; } // 转场锁

    #endregion
    #region 2. Unity 生命周期 (Unity Lifecycle)

    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        Anim = GetComponent<AnimationController>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        // 实例化状态
        NormalState = new PlayerNormalState(this);
        JumpState = new PlayerJumpState(this);
        DashState = new PlayerDashState(this);
        WallSlideState = new PlayerWallSlideState(this);
        DieState = new PlayerDieState(this);
        ClimbState = new PlayerClimbState(this);
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (dashAction != null) dashAction.action.Enable();
        if (grabAction != null) grabAction.action.Enable();
    }

    private void Start()
    {
        // 游戏开始，默认进入 Normal 状态
        currentCheckpoint = transform.position;
        Initialize(NormalState);
    }

    private void Update()
    {
        if (IsTransitioning) return;
        // 读取输入，存起来供各个状态使用
        MoveInput = moveAction.action.ReadValue<Vector2>();

        //记录玩家最后面朝的方向
        if (MoveInput.x != 0 && CurrentState != ClimbState)
        {
            FacingDir = Mathf.Sign(MoveInput.x);
            Anim.FlipCharacter(MoveInput.x);
        }

        // 更新蹬墙硬直计时器
        if (WallJumpLockCounter > 0)
        {
            WallJumpLockCounter -= Time.deltaTime;
        }

        if (IsGrounded() && CurrentState != DashState && Speed.y <= 0)
        {
            CanDash = true; // 只要脚踩地，就恢复冲刺次数
        }

        if (GrabCooldownCounter > 0)
        {
            GrabCooldownCounter -= Time.deltaTime;
        }

        //处理土狼时间
        if (IsGrounded())
        {
            // 只要在地上，土狼时间就总是满的
            CoyoteTimeCounter = coyoteTime;
        }
        else
        {
            CoyoteTimeCounter -= Time.deltaTime;
        }

        if (JumpBufferCounter > 0)
        {
            JumpBufferCounter -= Time.deltaTime;
        }

        // 无论在什么状态下，只要按下了跳跃键，就把计时器充满
        // （替换为 InputSystem 的 Action）
        if (jumpAction.action.WasPressedThisFrame())
        {
            JumpBufferCounter = jumpBufferTime;
        }

        if (CurrentState != null)
        {
            CurrentState.LogicUpdate();
        }

        if (IsGrounded())
        {
            CurrentStamina = maxStamina;
        }
    }

    private void FixedUpdate()
    {
        if (IsTransitioning) return;
        if (CurrentState != null)
        {
            CurrentState.PhysicsUpdate();

            MoveH(Speed.x * Time.fixedDeltaTime * 50f);
            MoveV(Speed.y * Time.fixedDeltaTime * 50f);
        }
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (dashAction != null) dashAction.action.Disable();
        if (grabAction != null) grabAction.action.Disable();
    }
    #endregion
    #region 3. 状态机核心逻辑

    // 初始化状态的方法
    public void Initialize(PlayerState startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    // 切换状态的方法
    public void ChangeState(PlayerState newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    #endregion
    #region 4. 公开辅助方法
    // 地面检测
    public bool IsGrounded()
    {
        // 把碰撞盒往下挪一点点(0.05f)探测
        Vector2 checkPos = RB.position + col.offset + new Vector2(0, -0.05f);
        return Physics2D.OverlapBox(checkPos, col.size, 0, groundLayer);
    }

    public bool IsTouchingWall()
    {
        // 往面朝方向挪一点点探测
        Vector2 checkPos = RB.position + col.offset + new Vector2(FacingDir * 0.05f, 0);
        return Physics2D.OverlapBox(checkPos, col.size, 0, groundLayer);
    }

    // 跳跃成功后调用，清空缓冲池
    public void ConsumeJumpBuffer()
    {
        JumpBufferCounter = 0f;
    }

    //跳跃成功后调用，把空气踏板撤掉
    public void ConsumeCoyoteTime()
    {
        CoyoteTimeCounter = 0f;
    }

    // 触发蹬墙跳时调用
    public void SetWallJumpLock(float direction)
    {
        WallJumpLockCounter = wallJumpDuration; // 比如 0.15 秒
        WallJumpDirection = direction; // 记住被弹开的方向
    }

    public void SetGrabCooldown(float time)
    {
        GrabCooldownCounter = time;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && col != null)
        {
            Gizmos.color = Color.red;
            // 画出向下探测地面的红框
            Vector2 groundCheckPos = RB.position + col.offset + new Vector2(0, -0.05f);
            Gizmos.DrawWireCube(groundCheckPos, col.size);

            // 画出向前方探测墙壁的蓝框
            Gizmos.color = Color.blue;
            Vector2 wallCheckPos = RB.position + col.offset + new Vector2(FacingDir * 0.05f, 0);
            Gizmos.DrawWireCube(wallCheckPos, col.size);
        }
    }

    /// <summary>
    /// 开启死亡表演协程
    /// </summary>
    public void StartDeathSequence()
    {
        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUILock(true);
        }

        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }
        // 尸体弹飞
        yield return new WaitForSeconds(0.15f);//只能在协程中使用的延时等待类

        Speed = Vector2.zero;

        Anim.GetComponent<SpriteRenderer>().enabled = false;//unity生命周期，不能关闭自身，只能管SpriteRenderer组件的显示与否


        PlayDeathEffects();

        yield return new WaitForSeconds(0.4f);

        // ================= 第 3 幕：呼叫转场大管家！ =================
        // 我们把复活的逻辑打包成一个 Lambda 表达式 (就是那个 () => { ... })，作为参数传给管家
        // 管家会在屏幕【完全黑掉的那一瞬间】，执行这个大括号里的逻辑！
        TransitionManager.Instance.StartTransition(() =>
        {
            // -------------------- 全黑时刻发生的事 --------------------
            // 仅仅是偷偷把隐身的尸体运过去，不显示！
            PrepareForRespawn();

            // 开启一个新的协程，专门负责“黑幕展开后的重生表演”
            StartCoroutine(RespawnSequenceCoroutine());
        });
    }

    private IEnumerator RespawnSequenceCoroutine()
    {
        yield return new WaitForSeconds(0.2f);

        PlayRespawnEffects();

        yield return new WaitForSeconds(0.3f);

        FinalizeRespawn();
    }


    public void PrepareForRespawn()
    {
        transform.position = currentCheckpoint;

        Speed = Vector2.zero;

    }

    public void FinalizeRespawn()
    {

        Anim.GetComponent<SpriteRenderer>().enabled = true;

        ChangeState(NormalState);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetUILock(false);
        }
    }

    /// <summary>
    /// 在玩家当前位置，瞬间生成并播放死亡粒子特效
    /// </summary>
    public void PlayDeathEffects()
    {
        if (deathParticlesPrefab != null)
        {
            Instantiate(deathParticlesPrefab, transform.position, Quaternion.identity);

            // TODO: 未来可以在这里加上死亡音效的代码
            // AudioManager.PlaySound("Player_Death");
        }
    }

    /// <summary>
    /// 播放向内聚拢的重生特效
    /// </summary>
    public void PlayRespawnEffects()
    {
        if (respawnParticlesPrefab != null)
        {
            Instantiate(respawnParticlesPrefab, transform.position, Quaternion.identity);
        }
    }

    #region 触发器检测 (Triggers)
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IHazard hazard = collision.GetComponent<IHazard>();

        if (hazard != null)
        {
            if (CurrentState != DieState)
            {
                ChangeState(DieState);
            }
            return;
        }

        IInteractable interactable = collision.GetComponent<IInteractable>();

        if (interactable != null)
        {
            interactable.Interact(this);
        }
    }

    #endregion

    /// <summary>
    /// 尝试进行顶角边缘修正
    /// </summary>
    /// <param name="currentPos">当前虚拟推演的坐标</param>
    /// <returns>如果修正成功返回 true，否则返回 false</returns>
    private bool AttemptCornerCorrection(ref Vector2 currentPos)
    {
        float correctionDistance = 0.08f;
        float step = 0.02f;

        // 尝试向右寻找空隙
        for (float i = step; i <= correctionDistance; i += step)
        {
            Vector2 checkPosRight = currentPos + new Vector2(i, 0.02f);
            if (!Physics2D.OverlapBox(checkPosRight + col.offset, col.size, 0, groundLayer))
            {
                // 如果不撞头了！赶紧把虚拟坐标往右推！
                currentPos += new Vector2(i, 0);
                return true; // 修正成功！
            }
        }

        // 尝试向左寻找空隙
        for (float i = step; i <= correctionDistance; i += step)
        {
            Vector2 checkPosLeft = currentPos + new Vector2(-i, 0.02f);
            if (!Physics2D.OverlapBox(checkPosLeft + col.offset, col.size, 0, groundLayer))
            {
                currentPos += new Vector2(-i, 0);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 水平移动
    /// </summary>
    public void MoveH(float moveAmount)
    {
        positionRemainder.x += moveAmount;
        int move = Mathf.RoundToInt(positionRemainder.x);

        if (move != 0)
        {
            positionRemainder.x -= move;
            int sign = (int)Mathf.Sign(move);

            // 提取当前实际位置，用它来做步进计算
            Vector2 currentPos = transform.position;

            while (move != 0)
            {
                // 用 currentPos 去做探测，这样每次循环起点都在往前推
                Vector2 checkPos = currentPos + new Vector2(sign * 0.02f, 0);
                bool hitWall = Physics2D.OverlapBox(checkPos + col.offset, col.size, 0, groundLayer);

                if (!hitWall)
                {
                    currentPos += new Vector2(sign * 0.02f, 0); // 更新局部位置
                    move -= sign;
                }
                else
                {
                    Speed.x = 0f;
                    positionRemainder.x = 0f;
                    break;
                }
            }

            // 循环结束后，一次性将最终计算出的位置赋给玩家
            transform.position = currentPos;
        }
    }

    /// <summary>
    /// 垂直移动
    /// </summary>
    public void MoveV(float moveAmount)
    {
        positionRemainder.y += moveAmount;
        int move = Mathf.RoundToInt(positionRemainder.y);

        if (move != 0)
        {
            positionRemainder.y -= move;
            int sign = (int)Mathf.Sign(move);

            // 提取当前实际位置
            Vector2 currentPos = transform.position;

            while (move != 0)
            {
                Vector2 checkPos = currentPos + new Vector2(0, sign * 0.02f);
                bool hitGround = Physics2D.OverlapBox(checkPos + col.offset, col.size, 0, groundLayer);

                if (!hitGround)
                {
                    currentPos += new Vector2(0, sign * 0.02f);
                    move -= sign;
                }
                else
                {
                    if (sign == 1)
                    {
                        // 尝试进行边缘修正
                        if (AttemptCornerCorrection(ref currentPos))
                        {
                            // 如果修正成功
                            // 用 continue 跳过下面的清零代码
                            continue;
                        }
                    }
                    Speed.y = 0f;
                    positionRemainder.y = 0f;
                    break;
                }
            }
            // 赋值最终位置
            transform.position = currentPos;//物理引擎与 Transform 的数据同步不是实时的。
        }
    }
}
