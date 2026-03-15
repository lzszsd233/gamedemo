using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerStateMachine : MonoBehaviour
{
    // 当前正在运行的状态
    public PlayerState CurrentState { get; private set; }//属性（Property）

    //get：意思是允许读取。别的脚本可以获取到这个变量的值


    public PlayerNormalState NormalState { get; private set; }
    public PlayerJumpState JumpState { get; private set; }

    //private set：意思是私有修改。只有当前这个脚本才有权力修改这个变量的值。别的脚本绝对改不了这个变量的值，只能读取它。这样做的好处是，外部脚本只能获取到状态实例，但不能随意修改它们，保证了状态的完整性和安全性。


    [Header("组件引用")]
    public Rigidbody2D RB { get; private set; }

    [Header("输入")]
    public InputActionReference moveAction;
    public Vector2 MoveInput { get; private set; }

    [Header("移动配置")]
    public float moveSpeed = 8f;

    [Header("地面检测参数")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.5f;

    // 地面检测
    public bool IsGrounded()
    {
        // 向下发射射线
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null; // 只要射线碰到东西，就返回 true
    }

    // 编辑器里看到射线长度
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }



    private void Awake()
    {
        RB = GetComponent<Rigidbody2D>();

        // 实例化状态
        NormalState = new PlayerNormalState(this);
        JumpState = new PlayerJumpState(this);
    }

    private void Start()
    {
        // 游戏开始，默认进入 Normal 状态
        Initialize(NormalState);
    }

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

    private void Update()
    {
        // 读取输入，存起来供各个状态使用
        MoveInput = moveAction.action.ReadValue<Vector2>();

        // 让当前状态每帧执行逻辑
        if (CurrentState != null)
            CurrentState.LogicUpdate();
    }

    private void FixedUpdate()
    {
        // 让当前状态执行物理逻辑
        if (CurrentState != null)
            CurrentState.PhysicsUpdate();
    }

    private void OnEnable()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
    }
    private void OnDisable()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }

    }
}
