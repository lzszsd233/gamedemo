using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RebindManager : MonoBehaviour
{
    [Header("全套按键配置文件")]
    // 注意：这里要的是那个 .inputactions 资产文件本身
    public InputActionAsset inputAsset;
    [Header("遮罩 UI")]
    public GameObject rebindOverlay;

    // 这是一个特殊的变量，用来存储“正在等待玩家按下按键”的这个过程
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;


    // 存在硬盘里的注册表名字
    private const string REBINDS_KEY = "rebinds";

    private void Start()
    {
        // 游戏一运行，去硬盘里找：玩家以前改过键吗
        LoadRebinds();
    }

    /// <summary>
    /// (核心万能改键方法)外部的按钮按下去时，就会呼叫这个方法
    /// </summary>
    /// <param name="actionToRebind">玩家要的动作(如 Jump, Move)</param>
    /// <param name="bindingIndex">玩家要改的键(Jump是0，Left是3)</param>
    /// <param name="buttonText">改完之后更新的UI文字掉</param>
    public void StartRebind(InputActionReference actionToRebind, int bindingIndex, TMP_Text buttonText)
    {
        // 改键前，必须把整个动作表暂时关闭，否则会冲突报错
        actionToRebind.action.actionMap.Disable();

        //在开始听新按键之前，先把当前的“旧覆写路径”背下来
        // 如果玩家之前改过键（比如R），overridePath 就是 "<Keyboard>/r"
        // 如果玩家没改过键（出厂状态），overridePath 就是 null
        string oldOverridePath = actionToRebind.action.bindings[bindingIndex].overridePath;

        // 显示遮罩：“请按下新按键...”
        if (rebindOverlay != null) rebindOverlay.SetActive(true);
        buttonText.text = "等待输入";


        //链式编程（Fluent API）
        // 让 Unity 开始竖起耳朵听玩家按下的下一个键
        rebindingOperation = actionToRebind.action.PerformInteractiveRebinding(bindingIndex)
            // 排除掉鼠标
            .WithControlsExcluding("<Mouse>")
            // 当玩家按下一个键（改键完成）时，执行大括号里的事
            //Lambda 表达式（匿名函数）
            .OnComplete(operation =>
            {
                // 获取玩家刚刚按下的那个键的物理路径
                string newBindingPath = actionToRebind.action.bindings[bindingIndex].effectivePath;

                // 检查冲突，遍历整个输入表
                bool isConflict = CheckForConflict(actionToRebind.action, newBindingPath, bindingIndex);

                if (isConflict)
                {
                    Debug.LogWarning("按键冲突！恢复原按键！");
                    //actionToRebind.action.RemoveBindingOverride(bindingIndex);这句不能使用

                    // 真正的撤销逻辑
                    if (string.IsNullOrEmpty(oldOverridePath))
                    {
                        // 如果以前是出厂状态，就直接清除覆写，恢复出厂
                        actionToRebind.action.RemoveBindingOverride(bindingIndex);
                    }
                    else
                    {
                        // 如果以前有自定义的键（比如R），就把R重新覆写回去
                        actionToRebind.action.ApplyBindingOverride(bindingIndex, oldOverridePath);
                    }
                }
                else
                {
                    SaveRebinds();
                }

                operation.Dispose();
                actionToRebind.action.actionMap.Enable();
                if (rebindOverlay != null) rebindOverlay.SetActive(false);

                //  更新 UI
                buttonText.text = actionToRebind.action.GetBindingDisplayString(bindingIndex);
            })
            // 按 ESC 退出了改键
            .OnCancel(operation =>
            {
                operation.Dispose();
                actionToRebind.action.actionMap.Enable();
                if (rebindOverlay != null) rebindOverlay.SetActive(false);
                buttonText.text = actionToRebind.action.GetBindingDisplayString(bindingIndex); // 恢复原状
            })
            .Start(); // 启动监听
    }

    /// <summary>
    /// 检查按键是否被其他动作占用
    /// </summary>
    /// <param name="currentAction">当前正在改的动作</param>
    /// <param name="newBindingPath">玩家按下的新键的物理路径</param>
    /// <param name="currentBindingIndex">当前正在改的具体是第几个键</param>
    private bool CheckForConflict(InputAction currentAction, string newBindingPath, int currentBindingIndex)
    {
        //我们只查当前动作所在的那个表
        InputActionMap currentMap = currentAction.actionMap;

        foreach (var action in currentMap) // 注意：这里改成了遍历 currentMap
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                // 打破同部门包庇！
                // 只有当“动作是同一个” 且 “具体的排号(Index)也是同一个” 时，才算查到了自己，才能跳过
                if (action == currentAction && i == currentBindingIndex)
                {
                    continue;
                }

                // 如果在同一个表里，查到了其他键也在用这个路径
                if (action.bindings[i].effectivePath == newBindingPath)
                {
                    return true; // 报告冲突
                }
            }
        }
        return false; // 安全
    }

    // ================== 硬盘存储区 ==================

    private void SaveRebinds()
    {
        // 把当前所有的按键配置，打包成一段 JSON 文本
        string rebinds = inputAsset.SaveBindingOverridesAsJson();
        // 存进电脑的 PlayerPrefs 里
        PlayerPrefs.SetString(REBINDS_KEY, rebinds);
        PlayerPrefs.Save();
    }

    private void LoadRebinds()
    {
        // 如果电脑里存过这个数据
        if (PlayerPrefs.HasKey(REBINDS_KEY))
        {
            string rebinds = PlayerPrefs.GetString(REBINDS_KEY);
            // 把读出来的 JSON 文本强行覆盖到现在的按键上
            inputAsset.LoadBindingOverridesFromJson(rebinds);
        }
    }

}
