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
    /// 【核心接口】：执行一次完整的转场动画 (收缩 -> 停顿 -> 展开)
    /// </summary>
    /// <param name="focusWorldPos">转场圆心的世界坐标 (比如小恐龙尸体的位置)</param>
    /// <param name="onMidpoint">当黑屏完全收拢(全黑)时，要执行的动作 (比如传送、复活)</param>
    public void StartTransition(Vector3 focusWorldPos, System.Action onMidpoint)
    {
        StartCoroutine(TransitionRoutine(focusWorldPos, onMidpoint));
    }

    private IEnumerator TransitionRoutine(Vector3 worldPos, System.Action onMidpoint)
    {
        if (irisWipeImage == null || irisWipeImage.material == null) yield break;

        Material mat = irisWipeImage.material;

        // 将世界坐标转换成屏幕 UV 坐标 (0 到 1 之间)，告诉 Shader 圆心在哪
        Vector2 screenPos = Camera.main.WorldToViewportPoint(worldPos);
        mat.SetFloat(centerXID, screenPos.x);
        mat.SetFloat(centerYID, screenPos.y);

        // 向中心收缩 
        float currentRadius = MAX_RADIUS;
        while (currentRadius > 0f)
        {
            currentRadius -= Time.unscaledDeltaTime * transitionSpeed; // 用 unscaledDeltaTime 保证即使游戏暂停也能转场
            mat.SetFloat(radiusID, Mathf.Max(0f, currentRadius));
            yield return null; // 等待下一帧
        }

        mat.SetFloat(radiusID, 0f); // 确保死透了

        // 极其重要的回调！在全黑的时候，执行外面传进来的逻辑 (比如把小恐龙传送到复活点)
        onMidpoint?.Invoke();

        yield return new WaitForSecondsRealtime(0.2f);

        // 找到屏幕上带有 Player 标签的物体
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 newScreenPos = Camera.main.WorldToViewportPoint(player.transform.position);
            mat.SetFloat(centerXID, newScreenPos.x);
            mat.SetFloat(centerYID, newScreenPos.y);
        }

        // 3. 动画第二阶段：向外展开 (Radius 从 0 变回 MAX_RADIUS)
        while (currentRadius < MAX_RADIUS)
        {
            currentRadius += Time.unscaledDeltaTime * transitionSpeed;
            mat.SetFloat(radiusID, Mathf.Min(MAX_RADIUS, currentRadius));
            yield return null;
        }

        mat.SetFloat(radiusID, MAX_RADIUS); // 确保完全透明，不挡屏幕
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
