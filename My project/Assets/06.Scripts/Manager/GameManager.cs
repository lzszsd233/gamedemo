using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("玩家引用")]
    public PlayerStateMachine player;

    [Header("主菜单回归位置")]
    public Transform menuIdlePoint;

    private EventBus.DeathType lastDeathType;

    // 【新增】：存档用的钥匙（Key）
    private const string SAVE_SCENE_KEY = "SavedScene";
    private const string SAVE_X_KEY = "SavedPosX";
    private const string SAVE_Y_KEY = "SavedPosY";

    // 记录我们要加载哪个场景、传送到哪个坐标
    private string targetSceneToLoad;
    private Vector2 targetRespawnPos;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // 【核心修改】：监听“动画播完”的频道！
        EventBus.OnPlayerDied += RecordDeathType;
        EventBus.OnPlayerDeathAnimationFinished += HandlePlayerDeathAnimationFinished;
    }

    private void Start()
    {
        // 1. 尝试找到小恐龙引用
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.GetComponent<PlayerStateMachine>();

        // 2. 判断当前是“冷启动”还是“编辑器测试模式”
        if (SceneManager.sceneCount == 1)
        {
            // 【情况 A：冷启动】
            // 如果内存里只有 Persistent，说明刚打开游戏，自动加载主菜单
            if (player != null)
            {
                // A. 【绝对关键】：确保小恐龙是 激活(Active) 的！这样他才能在主菜单露脸
                player.gameObject.SetActive(true);

                // B. 【上锁】：切断输入，让他只播 Idle 动画，不准乱动
                player.SetInputEnabled(false);

                // C. 【传送】：把他送到主菜单那个风景好的位置
                if (menuIdlePoint != null) player.transform.position = menuIdlePoint.position;

                // D. 【掰脸】：强制让他看向左边（如你之前所愿）
                player.ForceFacingDirection(1f);
            }

            if (UIManager.Instance != null) UIManager.Instance.SetMainMenuMode(true);

            // 异步加载主菜单
            SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            Debug.Log("[GameManager] 冷启动成功，已召唤主菜单。");
        }
        else
        {
            // 【情况 B：编辑器测试】
            // 如果你手动点开了两个场景运行，直接让关卡长官干活
            LevelManager levelMgr = Object.FindAnyObjectByType<LevelManager>();
            if (levelMgr != null) levelMgr.InitializeLevel();
            Debug.Log("[GameManager] 编辑器 Debug 模式，跳过自动加载。");
        }
    }

    private void OnDisable()
    {
        EventBus.OnPlayerDied -= RecordDeathType;
        EventBus.OnPlayerDeathAnimationFinished -= HandlePlayerDeathAnimationFinished;
    }

    // ================== 【存档系统】 ==================

    /// <summary>
    /// 由 LevelManager 的房间触发器，在每次更新重生点时调用！
    /// </summary>
    public void SaveGameProgress(string sceneName, Vector2 respawnPos)
    {
        // 把当前场景名字和最新重生点的 X、Y 坐标存入注册表/本地磁盘
        PlayerPrefs.SetString(SAVE_SCENE_KEY, sceneName);
        PlayerPrefs.SetFloat(SAVE_X_KEY, respawnPos.x);
        PlayerPrefs.SetFloat(SAVE_Y_KEY, respawnPos.y);
        PlayerPrefs.Save();

        Debug.Log($"[存档成功] 场景:{sceneName}, 坐标:{respawnPos}");
    }

    /// <summary>
    /// 供外部 UI 测试按钮调用：一键清空存档！
    /// </summary>
    public void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(SAVE_SCENE_KEY);
        PlayerPrefs.DeleteKey(SAVE_X_KEY);
        PlayerPrefs.DeleteKey(SAVE_Y_KEY);
        PlayerPrefs.Save();
        Debug.Log("存档已清空！");
    }

    // ================== 【开始/继续 逻辑】 ==================

    /// <summary>
    /// 开始新游戏：强制抹除存档，从第一关原点开始
    /// </summary>
    public void StartNewGame()
    {
        if (player.IsTransitioning) return;
        ClearSaveData();

        // 设定目标为第一关
        targetSceneToLoad = "Level_01";
        // 这里传 Vector2.positiveInfinity 作为一个特殊标记，
        // 告诉后面的代码：“去用第一关默认的 gameStartPoint，别用存档坐标”
        targetRespawnPos = Vector2.positiveInfinity;

        StartCoroutine(LoadLevelRoutine());
    }

    /// <summary>
    /// 继续游戏：读取本地存档。如果没有存档，就等同于开始新游戏。
    /// </summary>
    public void ContinueGame()
    {
        if (player.IsTransitioning) return;

        // 如果本地有存过场景名字，说明玩过
        if (PlayerPrefs.HasKey(SAVE_SCENE_KEY))
        {
            targetSceneToLoad = PlayerPrefs.GetString(SAVE_SCENE_KEY);
            targetRespawnPos = new Vector2(
                PlayerPrefs.GetFloat(SAVE_X_KEY),
                PlayerPrefs.GetFloat(SAVE_Y_KEY)
            );

            StartCoroutine(LoadLevelRoutine());
        }
        else
        {

            StartNewGame();
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        if (player.IsTransitioning) return;

        Debug.Log("正在退出游戏...");
        Application.Quit();
        // 在编辑器里运行时，退出指令没用，所以加个这行方便测试：
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


    private IEnumerator LoadLevelRoutine()
    {
        player.IsTransitioning = true;
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(true);

        yield return StartCoroutine(TransitionManager.Instance.CloseBlackScreen());

        // 1. 卸载主菜单
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync("MainMenu");
        if (unloadOp != null) yield return new WaitUntil(() => unloadOp.isDone);

        // 2. 加载目标场景（Level_01 或者是玩家玩到的 Level_05）
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetSceneToLoad, LoadSceneMode.Additive);
        yield return new WaitUntil(() => loadOp.isDone);

        // 3. 把坐标和控制权交给关卡长官
        LevelManager levelMgr = FindAnyObjectByType<LevelManager>();
        if (levelMgr != null)
        {
            // 把我们查出来的目标坐标传给它
            levelMgr.SetupPlayerInLevel(player, targetRespawnPos);
        }

        // ======================== 【核心新增：让黑幕多拉一会儿】 ========================
        // 为什么要用 Realtime？因为如果此时 Time.timeScale 是 0，普通的 WaitForSeconds 会卡死。
        // 我们在这里强行让黑幕多闭合 1 秒钟，给 Cinemachine 充足的时间滑过去。
        yield return new WaitForSecondsRealtime(1.0f);
        // ==============================================================================

        // 4. 现在，摄像机肯定已经停稳了！我们呼叫大管家：把黑幕拉开！
        yield return StartCoroutine(TransitionManager.Instance.OpenBlackScreen());

        if (player != null)
        {
            player.IsTransitioning = false; // 解除植物人状态（恢复 Update 循环）
            player.SetInputEnabled(true);   // 重新插上游戏手柄（恢复输入监听）
        }

        if (UIManager.Instance != null) UIManager.Instance.SetUILock(false);
        player.IsTransitioning = false;
    }

    /// <summary>
    /// 供游戏内 ESC 设置面板里的“返回主菜单”按钮调用
    /// </summary>
    public void ReturnToMainMenu()
    {
        if (player.IsTransitioning) return;

        Time.timeScale = 1f;
        // 1. 锁死 UI，防止转场中途玩家乱按
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(true);

        if (player != null)
        {
            player.LockPlayerForTransition();
        }

        StartCoroutine(ReturnToMenuRoutine());
    }

    private IEnumerator ReturnToMenuRoutine()
    {
        player.IsTransitioning = true;
        yield return StartCoroutine(TransitionManager.Instance.CloseBlackScreen());
        // ================= 全黑时刻的幕后操作 =================

        // 1. 寻找当前活动的关卡场景名字（比如 "Level_01"）
        // 只要不是 Persistent 和 MainMenu，那就是我们要卸载的关卡！
        string currentLevelName = "";
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.name != "Persistent" && scene.name != "MainMenu")
            {
                currentLevelName = scene.name;
                break;
            }
        }

        // 2. 异步卸载当前的关卡场景！
        if (!string.IsNullOrEmpty(currentLevelName))
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentLevelName);
            if (unloadOp != null) yield return new WaitUntil(() => unloadOp.isDone);
        }

        // 3. 异步重新加载主菜单场景！
        AsyncOperation loadOp = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
        yield return new WaitUntil(() => loadOp.isDone);

        // 4. 重置小恐龙：把他传送到主菜单的发呆位置，并剥夺操作权
        if (player != null)
        {
            player.SetInputEnabled(false);
            if (menuIdlePoint != null)
            {
                player.transform.position = menuIdlePoint.position;

                player.ForceFacingDirection(1f);
            }
        }

        // 5. 切换回主菜单 UI 模式
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetMainMenuMode(true);
        }

        // ======================== 【核心新增：让黑幕多拉一会儿】 ========================
        // 同样在这里等 1 秒，让摄像机平滑地回到主菜单风景图上
        yield return new WaitForSecondsRealtime(1.0f);
        // ==============================================================================

        // 6. 呼叫大管家：拉开黑幕！
        yield return StartCoroutine(TransitionManager.Instance.OpenBlackScreen());

        if (UIManager.Instance != null) UIManager.Instance.SetUILock(false);
        player.IsTransitioning = false;
    }

    private void RecordDeathType(EventBus.DeathType deathType)
    {
        lastDeathType = deathType;
    }

    // 当听到“小恐龙碎完了”的广播时，统帅才开始行动！
    private void HandlePlayerDeathAnimationFinished()
    {
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(true);

        if (TransitionManager.Instance != null)
        {
            // 【终极修复】：根据小本本上的记录，决定用几号幕布！
            int style = 0;
            if (lastDeathType == EventBus.DeathType.Crush) style = 1;      // 挤死 -> 1号幕布(从左到右)
            else if (lastDeathType == EventBus.DeathType.FallVoid) style = 2; // 虚空 -> 2号幕布(溶解)

            // 把样式数字 (style) 传进去！不要再用空参数的默认方法了！
            TransitionManager.Instance.StartTransition(style, () =>
            {
                player.transform.position = player.currentCheckpoint;
                player.Speed = Vector2.zero;

                LevelManager levelMgr = Object.FindAnyObjectByType<LevelManager>();
                if (levelMgr != null) levelMgr.ResetCurrentRoomEntities();

                StartCoroutine(RespawnSequenceCoroutine());
            });
        }
    }
    // 复活的动画剧本，也归 GameManager 管！
    private IEnumerator RespawnSequenceCoroutine()
    {
        yield return new WaitForSeconds(0.4f);
        if (player.respawnParticlesPrefab != null)
        {
            Object.Instantiate(player.respawnParticlesPrefab, player.transform.position, Quaternion.identity);
        }
        yield return new WaitForSeconds(0.3f);

        player.Anim.GetComponent<SpriteRenderer>().enabled = true;
        if (UIManager.Instance != null) UIManager.Instance.SetUILock(false);

        player.ChangeState(player.NormalState);
    }
}