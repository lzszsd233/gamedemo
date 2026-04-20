using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// 全局转场管理器 (控制 Iris Wipe 黑屏动画)
/// </summary>
public class TransitionManager : MonoBehaviour
{
    // 单例模式：让全宇宙的代码都能极其方便地找到这个大管家！
    public static TransitionManager Instance { get; private set; }

    [Header("UI 遮罩设置")]
    public Image irisWipeImage;      // 挂载了 IrisWipeMaterial 的 UI 图片
    public float transitionSpeed = 2f; // 转场动画的速度 (越小越慢)

    [Header("转场材质包")]
    public Material irisMaterial;
    public Material leftToRightMaterial;
    public Material voidMaterial;

    private System.Action currentOnMidpoint;


    // 缓存材质的属性 ID (提高性能，不用每次都去查字符串)
    private int radiusID;
    private int centerXID;
    private int centerYID;

    // 记录最大半径 (通常屏幕对角线的一半，1.5 足够覆盖 16:9 屏幕)
    private const float MAX_RADIUS = 1.5f;

    private void Awake()
    {
        // 经典单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 换场景也不销毁大管家
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化 Shader 属性的 ID
        radiusID = Shader.PropertyToID("_Radius");
        centerXID = Shader.PropertyToID("_CenterX");
        centerYID = Shader.PropertyToID("_CenterY");

        // 游戏刚开始时，确保屏幕是完全透明的（Radius 最大）
        if (irisWipeImage != null && irisWipeImage.material != null)
        {
            irisWipeImage.material.SetFloat(radiusID, MAX_RADIUS);
        }
    }


    /// <summary>
    /// 标准转场（只传回调，默认从屏幕中心，用圆形）
    /// 比如：主菜单进游戏、退出主菜单
    /// </summary>
    public void StartTransition(System.Action onMidpoint)
    {
        StartTransition(0, onMidpoint);
    }

    /// <summary>
    /// 高级转场（指定圆心、指定样式）
    /// 比如：各种花式死亡
    /// </summary>
    public void StartTransition(int style, System.Action onMidpoint)
    {
        if (irisWipeImage == null) return;

        // 根据传入的样式换材质
        if (style == 0 && irisMaterial != null) irisWipeImage.material = irisMaterial;
        else if (style == 1 && leftToRightMaterial != null) irisWipeImage.material = leftToRightMaterial;
        else if (style == 2 && voidMaterial != null) irisWipeImage.material = voidMaterial;

        // 【核心修复】：不再用 currentOnMidpoint 这种容易漏掉的野路子了！
        // 直接开启一个统管“收缩-等待-拉开”的超级协程，把所有步骤焊死在一起！
        StartCoroutine(FullTransitionRoutine(onMidpoint));
    }


    // ================= 【终极闭环协程】 =================

    private IEnumerator FullTransitionRoutine(System.Action onMidpoint)
    {
        // 1. 先拉上黑幕 (等待完全变黑)
        yield return StartCoroutine(CloseBlackScreen());

        // 2. 黑透了！执行外部传进来的逻辑 (比如加载场景、传回复活点)
        onMidpoint?.Invoke();

        // （可选：如果你觉得加载太快，可以强行让全黑状态多保持 0.2 秒，增加节奏感）
        // yield return new WaitForSecondsRealtime(0.2f);

        // 【致命遗漏修复：拉开黑幕！】
        // 3. 逻辑执行完了，新场景或小恐龙已经就位了。
        // 现在，我们要从复活点（小恐龙的新位置）开始，把黑幕拉开！

        yield return StartCoroutine(OpenBlackScreen());
    }
    // ================= 【核心修复 3：补全拆分后的收缩和展开协程】 =================

    /// <summary>
    /// 动作 1：拉上黑幕（收缩）
    /// </summary>
    public IEnumerator CloseBlackScreen()
    {
        if (irisWipeImage == null || irisWipeImage.material == null) yield break;
        Material mat = irisWipeImage.material;

        // 【核心修改】：管你外面传什么进来，我只认屏幕正中心！
        mat.SetFloat(centerXID, 0.5f);
        mat.SetFloat(centerYID, 0.5f);

        // 收缩动画
        float currentRadius = MAX_RADIUS;
        while (currentRadius > 0f)
        {
            currentRadius -= Time.unscaledDeltaTime * transitionSpeed;
            mat.SetFloat(radiusID, Mathf.Max(0f, currentRadius));
            yield return null;
        }
        mat.SetFloat(radiusID, 0f);

        // 全黑后停顿一小下，增加窒息感
        yield return new WaitForSecondsRealtime(0.2f);

        // 【最重要的一句】：黑透了！执行你传进来的代码（比如传回复活点、卸载场景）！
        currentOnMidpoint?.Invoke();
    }

    /// <summary>
    /// 动作 2：拉开黑幕（展开）
    /// </summary>
    public IEnumerator OpenBlackScreen()
    {
        if (irisWipeImage == null || irisWipeImage.material == null) yield break;
        Material mat = irisWipeImage.material;

        // 【核心修改】：只认屏幕正中心！
        mat.SetFloat(centerXID, 0.5f);
        mat.SetFloat(centerYID, 0.5f);

        // 展开动画
        float currentRadius = 0f;
        while (currentRadius < MAX_RADIUS)
        {
            currentRadius += Time.unscaledDeltaTime * transitionSpeed;
            mat.SetFloat(radiusID, Mathf.Min(MAX_RADIUS, currentRadius));
            yield return null;
        }
        mat.SetFloat(radiusID, MAX_RADIUS); // 彻底透明
    }
    #region 顿帧系统 (Hitstop / Freeze Frame)

    /// <summary>
    /// 触发全局顿帧效果
    /// </summary>
    /// <param name="duration">时间完全静止的真实时间（秒）</param>
    public void Hitstop(float duration)
    {
        // 防御：如果游戏已经被暂停（比如打开了设置菜单），就不要顿帧了
        if (Time.timeScale == 0) return;

        StartCoroutine(HitstopCoroutine(duration));
    }

    private IEnumerator HitstopCoroutine(float duration)
    {
        Time.timeScale = 0f;

        // 等待指定的真实时间，因为 timeScale 是 0，普通的 WaitForSeconds 也会停止，必须用 Realtime
        yield return new WaitForSecondsRealtime(duration);

        if (!UIManager.Instance.isUILocked)
        {
            Time.timeScale = 1f;
        }
    }

    #endregion
}
