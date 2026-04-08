using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    // 单例模式，方便全局呼叫
    public static UIManager Instance { get; private set; }

    [Header("UI 面板引用")]
    public GameObject settingsPanel;

    public GameObject pauseButtonObj;

    private bool isPaused = false;
    //全局UI锁
    public bool isUILocked { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        settingsPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (isUILocked) return;
        // 允许玩家按键盘的 Esc 键也能呼出/关闭菜单
        if (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    /// <summary>
    /// 切换暂停/恢复状态
    /// </summary>
    public void TogglePause()
    {

        if (isUILocked) return;
        isPaused = !isPaused;

        if (isPaused)
        {
            // 暂停游戏
            settingsPanel.SetActive(true); // 显示面板
            Time.timeScale = 0f;           // 把游戏时间流速设为 0，物理和动画全部停止
        }
        else
        {
            // 恢复游戏
            settingsPanel.SetActive(false); // 隐藏面板
            Time.timeScale = 1f;            // 恢复时间流速为 1
        }
    }

    /// <summary>
    /// 强行关闭暂停菜单，并恢复时间流速。
    /// （供“返回主菜单”按钮在转场前调用）
    /// </summary>
    public void ForceClosePauseMenu()
    {
        if (isPaused)
        {
            isPaused = false;
            if (settingsPanel != null) settingsPanel.SetActive(false);
            Time.timeScale = 1f; // 极其重要：恢复时间流速！
        }
    }

    /// <summary>
    /// 锁定或解锁所有 UI 操作 (供外部调用，如转场、死亡时)
    /// </summary>
    public void SetUILock(bool isLocked)
    {
        isUILocked = isLocked;
    }

    /// <summary>
    /// 【新增】：切换主菜单/游戏内 UI 模式
    /// </summary>
    public void SetMainMenuMode(bool isMainMenu)
    {
        // 如果在主菜单，UI锁死（不响应ESC），且隐藏右上角暂停按钮
        isUILocked = isMainMenu;
        if (pauseButtonObj != null)
        {
            pauseButtonObj.SetActive(!isMainMenu);
        }
    }
}
