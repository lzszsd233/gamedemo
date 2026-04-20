using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening; // 我们用 DOTween 让按钮“淡出”，比直接消失更高级！


public class MainMenuManager : MonoBehaviour
{
    [Header("UI 导航")]
    public GameObject firstSelectedButton;

    private PlayerStateMachine player; // 记录玩家引用
    private CanvasGroup canvasGroup; // 【新增】：用来控制整个菜单的透明度和交互

    private void Awake()
    {
        // 获取挂在自己身上的 CanvasGroup
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        canvasGroup.DOKill();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // 1. 锁死 ESC 菜单，隐藏右上角按钮
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMainMenuMode(true);
        }

        // 2. 【核心修改：只切断输入，不隐身！】
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.GetComponent<PlayerStateMachine>();

            // 我们写一个公开的方法给老板，让他交出控制权
            if (player != null) player.SetInputEnabled(false);
        }

        // ================= 【核心修复：跨场景动态选中按钮】 =================
        // 只要全宇宙存在 EventSystem（它在 Persistent 里），且我们指定了按钮
        if (EventSystem.current != null && firstSelectedButton != null)
        {
            // 1. 先清空系统可能残留的旧焦点
            EventSystem.current.SetSelectedGameObject(null);

            // 2. 强行把焦点打在我们的“开始游戏”按钮上！
            EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }
    }

    // 删掉之前的 OnClickStartButton，换成这三个：

    public void OnClickNewGame()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0f, 0.2f).OnComplete(() =>//导致bug
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewGame();
        });
    }

    public void OnClickContinue()
    {
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.DOFade(0f, 0.2f).OnComplete(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.ContinueGame();
        });
    }

    public void OnClickQuit()
    {
        // 退出游戏不需要淡出转场，直接退
        if (GameManager.Instance != null) GameManager.Instance.QuitGame();
    }

    private void OnDestroy()
    {
        // ================= 封锁解除 =================
        // 这个脚本随着 MainMenu 场景被卸载而销毁时，自动把游戏恢复正常！

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMainMenuMode(false);
        }

        if (player != null) player.SetInputEnabled(true);

    }

}