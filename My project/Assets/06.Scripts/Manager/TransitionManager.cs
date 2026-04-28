using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

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
    private int progressID;

    private int isOpeningID;

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
        progressID = Shader.PropertyToID("_Progress");
        isOpeningID = Shader.PropertyToID("_IsOpening"); // 注册

        // 游戏刚开始时，确保屏幕是完全透明的（Radius 最大）
        if (irisWipeImage != null && irisWipeImage.material != null)
        {
            irisWipeImage.material.SetFloat(radiusID, MAX_RADIUS);
        }
    }

    /// <summary>
    /// 高级转场（指定圆心、指定样式）
    /// 比如：各种花式死亡
    /// </summary>
    public void StartTransition(int style, System.Action onMidpoint)
    {
        // 直接开启一个全套的协程
        StartCoroutine(FullTransitionRoutine(style, onMidpoint));
    }


    // ================= 【终极闭环协程】 =================

    private IEnumerator FullTransitionRoutine(int style, System.Action onMidpoint)
    {
        // 1. 先拉上黑幕 (等待完全变黑)
        yield return StartCoroutine(CloseBlackScreen(style));

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
    public IEnumerator CloseBlackScreen(int style = 0)
    {
        if (irisWipeImage == null || irisWipeImage.material == null) yield break;

        Material targetMat = irisMaterial;
        if (style == 1) targetMat = leftToRightMaterial;
        if (style == 2) targetMat = voidMaterial;

        irisWipeImage.material = targetMat;

        Material mat = irisWipeImage.material;

        // 【核心修改】：管你外面传什么进来，我只认屏幕正中心！
        mat.SetFloat(centerXID, 0.5f);
        mat.SetFloat(centerYID, 0.5f);

        mat.SetFloat(isOpeningID, 0f);

        // 收缩动画
        float currentRadius = MAX_RADIUS;
        while (currentRadius > 0f)
        {
            // 按真实时间(无视暂停)递减
            currentRadius -= Time.unscaledDeltaTime * transitionSpeed;

            // 算出 1 到 0 的进度百分比
            float progress = currentRadius / MAX_RADIUS;

            // 【核心修补】：将数值同时喂给两套系统！绝无遗漏！
            // 圆形 Shader 听这个：
            mat.SetFloat(radiusID, Mathf.Max(0f, currentRadius));
            // 擦除和溶解 Shader 听这个：
            mat.SetFloat(progressID, Mathf.Max(0f, progress));

            yield return null; // 必须等下一帧！
        }
        mat.SetFloat(radiusID, 0f);
        mat.SetFloat(progressID, 0f);

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

        mat.SetFloat(isOpeningID, 1f);

        // 展开动画
        float currentRadius = 0f;
        while (currentRadius < MAX_RADIUS)
        {
            // 按真实时间递增
            currentRadius += Time.unscaledDeltaTime * transitionSpeed;

            // 算出 0 到 1 的进度百分比
            float progress = currentRadius / MAX_RADIUS;

            // 【核心修补】：同步喂食两套 Shader 系统！
            mat.SetFloat(radiusID, Mathf.Min(MAX_RADIUS, currentRadius));
            mat.SetFloat(progressID, Mathf.Min(1f, progress));

            yield return null;
        }

        // 4. 循环结束，焊死终点值，确保完全透明！
        mat.SetFloat(radiusID, MAX_RADIUS);
        mat.SetFloat(progressID, 1f);
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
