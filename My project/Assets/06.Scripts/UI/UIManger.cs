using UnityEngine;

public class UIManger : MonoBehaviour
{
    [Header("UI 面板引用")]
    public GameObject settingsPanel;
    private bool isPaused = false;
    private void Start()
    {
        settingsPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Update()
    {
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
}
