using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
// 定义一个纯数据结构，用来存储房间信息
[System.Serializable]
public struct RoomData
{
    public string roomName;
    public Rect bounds; // 房间的纯数学矩形边界
    public List<Transform> respawnPoints;
}
public class LevelManager : MonoBehaviour
{
    [Header("本关卡的出生点")]
    public Transform levelStartPoint; // 把本关第一个房间的复活点拖进来

    [Header("全局依赖")]
    private PlayerStateMachine player;
    public CinemachineConfiner2D globalConfiner;
    [Header("转场设置")]
    [Range(0.1f, 1.5f)]
    public float transitionDuration = 0.6f; // 默认延长到 0.6 秒，你可以随时在面板调
    // 整个关卡的房间数据列表
    [Header("房间数据")]
    public List<RoomData> allRooms = new List<RoomData>();

    [Header("关卡危险边界")]
    public float killYOffset = 2f;

    // 运行时需要的私有变量
    private RoomData currentRoom;
    private PolygonCollider2D dynamicCameraBoundingBox; // 我们用代码动态捏的一个形状，专门喂给摄像机

    private void Awake()
    {
        // 在自己身上动态创建一个物理碰撞体，设置为 Trigger
        // 这个碰撞体永远不参与游戏逻辑，它唯一的使命就是变形成当前房间的形状，喂给摄像机限制器
        dynamicCameraBoundingBox = gameObject.AddComponent<PolygonCollider2D>();
        dynamicCameraBoundingBox.isTrigger = true;
        globalConfiner.BoundingShape2D = dynamicCameraBoundingBox;

    }

    private void Update()
    {
        // 1. 防呆：如果没有玩家、玩家已经死了、或者正在转场中，什么都不做
        if (player == null || !player.gameObject.activeInHierarchy) return;
        if (player.CurrentState == player.DieState || player.IsTransitioning) return;

        // 2. 核心判定：玩家是否离开了当前房间的边界？
        // 注意：这里我们用 Contains 判断玩家还在不在当前的 Rect 里
        if (!currentRoom.bounds.Contains(player.transform.position))
        {
            // 【玩家越界了！】

            // 3. 第一步：先看看他是不是掉进了其他任何一个房间？
            bool foundNewRoom = false;
            foreach (var room in allRooms)
            {
                // 如果在其他的房间里找到了玩家
                if (room.bounds.Contains(player.transform.position))
                {
                    // 找到了新房间！立刻开始平滑转场！
                    StartCoroutine(TransitionRoutine(room));
                    foundNewRoom = true;
                    break;
                }
            }

            // 4. 第二步：【终极审判】如果找遍了全世界，都没找到接住他的新房间！
            if (!foundNewRoom)
            {
                // 此时玩家不仅出了当前房间，而且掉进了真正的虚空（没有相邻房间）！

                // 为了视觉表现（让玩家完全掉出屏幕外再死，而不是在边界线上瞬间暴毙），
                // 我们依然保留一个“容忍度”（比如向下 2 米）。
                float currentDeathLine = currentRoom.bounds.yMin - killYOffset;

                // 如果他掉得比容忍度还要深，神仙难救！
                if (player.transform.position.y < currentDeathLine)
                {
                    player.DieState.ConfigureDeath(EventBus.DeathType.FallVoid);
                    player.ChangeState(player.DieState);
                }
            }
        }
    }

    public void InitializeLevel()
    {
        // 确保能找到玩家（即便他在上一秒还是禁用的）
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.GetComponent<PlayerStateMachine>();
            }
        }

        if (player == null) return;

        // 自动架设本关摄像机
        if (globalConfiner != null)
        {
            var vcam = globalConfiner.GetComponent<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = player.transform;
                vcam.PreviousStateIsValid = false; // 瞬间贴脸，不准漂移
                vcam.gameObject.SetActive(true);
                vcam.Priority = 100;
            }
        }

        // 寻找并切入房间
        Vector2 playerPos = player.transform.position;
        foreach (var room in allRooms)
        {
            if (room.bounds.Contains(playerPos))
            {
                SwitchToRoom(room, false);
                break;
            }
        }
    }

    /// <summary>
    /// 【核心】：由 GameManager 加载完本场景后调用！
    /// </summary>
    public void SetupPlayerInLevel(PlayerStateMachine incomingPlayer, Vector2 targetPos)
    {
        player = incomingPlayer;

        // 1. 强制传送玩家！
        // 如果传过来的是正无穷大，说明是“新游戏”，就用关卡默认起点
        if (float.IsPositiveInfinity(targetPos.x))
        {
            if (levelStartPoint != null)
            {
                player.transform.position = levelStartPoint.position;
                player.currentCheckpoint = levelStartPoint.position;
            }
        }
        else
        {
            // 如果传过来的是具体数字，说明是“继续游戏读档”，直接瞬移过去！
            player.transform.position = targetPos;
            player.currentCheckpoint = targetPos;
        }

        // 2. 摄像机瞬间对齐，防止穿帮
        if (globalConfiner != null)
        {
            var vcam = globalConfiner.GetComponent<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = player.transform;

                // 【核心修复 2】：绝对不能直接传 player.transform.position（它的 Z 是 0）！
                // 必须手动把 Z 轴改成 -10f 再传给摄像机！否则摄像机会瞬间埋进地里！
                Vector3 safeCameraPos = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
                vcam.ForceCameraPosition(safeCameraPos, Quaternion.identity);

                vcam.gameObject.SetActive(true);
            }
        }

        // 3. 算出玩家落在哪个房间，切过去
        foreach (var room in allRooms)
        {
            if (room.bounds.Contains(player.transform.position))
            {
                SwitchToRoom(room, false);
                break;
            }
        }
    }


    // 转场协程
    private IEnumerator TransitionRoutine(RoomData nextRoom)
    {
        player.IsTransitioning = true;
        // 记录玩家真实速度，然后清零悬停
        Vector2 preservedSpeed = player.Speed;
        player.Speed = Vector2.zero;

        ResetRoomEntities(currentRoom);

        // ================= 【终极修复：基于几何越界的推力方向】 =================
        Vector3 pushDir = Vector3.zero;
        Vector3 playerStartPos = player.transform.position;

        // 核心魔法：我们拿玩家现在的坐标，和“旧房间”的四条边去比对！
        // 如果玩家超出了右边，必定是往右转场；超出了下边，必定是往下转场！

        // 容差值（防止刚好踩在边界上浮点数误判）
        float epsilon = 0.05f;

        if (playerStartPos.x > currentRoom.bounds.xMax - epsilon)
        {
            pushDir = Vector3.right; // 从右边出去了，往右推
        }
        else if (playerStartPos.x < currentRoom.bounds.xMin + epsilon)
        {
            pushDir = Vector3.left; // 从左边出去了，往左推
        }
        else if (playerStartPos.y > currentRoom.bounds.yMax - epsilon)
        {
            pushDir = Vector3.up; // 从顶上出去了，往上推
        }
        else if (playerStartPos.y < currentRoom.bounds.yMin + epsilon)
        {
            pushDir = Vector3.down; // 从底下出去了，往下推
        }
        else
        {
            // 防呆兜底：如果算不出来（几乎不可能），就用玩家当前的速度方向死马当活马医
            if (Mathf.Abs(preservedSpeed.x) > Mathf.Abs(preservedSpeed.y))
                pushDir.x = Mathf.Sign(preservedSpeed.x);
            else
                pushDir.y = Mathf.Sign(preservedSpeed.y);
        }
        // ========================================================================


        float targetPushDist = 1.5f; // 我们期望的最大推挤距离

        // 防穿模扫描
        // 发射一个和玩家一样大的隐形盒子，朝着推挤方向扫射
        RaycastHit2D hit = Physics2D.BoxCast(
            (Vector2)playerStartPos + player.col.offset, // 起点：玩家当前碰撞盒的中心
            player.col.size,                             // 形状：玩家的碰撞盒大小
            0f,                                          // 角度：0度不旋转
            pushDir,                                     // 方向：推挤的方向
            targetPushDist,                              // 距离：最多扫 1.5f
            player.groundLayer                           // 目标：只检测地面层，无视金币/敌人

        );

        Vector3 playerEndPos;
        if (hit.collider != null)
        {
            // 如果撞到了地面停在撞击点的前面一点点（退后 0.02f 的安全距离，防止物理浮点数误差）
            playerEndPos = playerStartPos + pushDir * (hit.distance - 0.02f);
        }
        else
        {
            playerEndPos = playerStartPos + pushDir * targetPushDist;
        }

        // 3. 算出摄像机要平滑滑向哪里 (终点)
        Camera mainCam = Camera.main;
        Vector3 camStartPos = mainCam.transform.position;

        // 获取当前摄像机的一半高和一半宽
        float camHalfHeight = mainCam.orthographicSize;
        float camHalfWidth = camHalfHeight * mainCam.aspect;

        // 算出新房间里，摄像机能呆的安全位置（防止拍到黑边）
        float camEndX = Mathf.Clamp(playerEndPos.x, nextRoom.bounds.xMin + camHalfWidth, nextRoom.bounds.xMax - camHalfWidth);
        float camEndY = Mathf.Clamp(playerEndPos.y, nextRoom.bounds.yMin + camHalfHeight, nextRoom.bounds.yMax - camHalfHeight);

        // ================= 【核心修复：轴向锁定魔法】 =================
        // pushDir 是我们之前算出来的推挤方向
        if (Mathf.Abs(pushDir.y) > 0.5f)
        {
            // 如果是上下转场：强行让目标的 X 等于起点的 X！
            // 这样摄像机就会笔直地向上/向下飞，绝对不会产生横向的漂移感。
            camEndX = camStartPos.x;
        }
        else if (Mathf.Abs(pushDir.x) > 0.5f)
        {
            // 如果是左右转场：强行让目标的 Y 等于起点的 Y！
            // 摄像机笔直左右飞，绝不上下晃动。
            camEndY = camStartPos.y;
        }
        // ============================================================

        Vector3 camEndPos = new Vector3(camEndX, camEndY, -10f);

        // 4. 【神级操作】：暂时关掉 Cinemachine，我们自己手动接管摄像机！
        globalConfiner.GetComponent<CinemachineCamera>().enabled = false;

        // 5. 开启平滑的逐帧动画！(这取代了之前的傻等 WaitForSeconds)
        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            // 算出进度百分比 (0 到 1)
            float t = elapsedTime / transitionDuration;
            // 套用一个平滑曲线，让移动有“慢-快-慢”的缓动感
            float easeT = Mathf.SmoothStep(0f, 1f, t);
            // 让恐龙和摄像机，在这一帧里同时往前走一小步！

            player.transform.position = Vector3.Lerp(playerStartPos, playerEndPos, easeT);
            mainCam.transform.position = Vector3.Lerp(camStartPos, camEndPos, easeT);
            elapsedTime += Time.deltaTime;

            yield return null; // 等待下一帧
        }
        // 6. 动画结束，强制对齐终点，防止浮点数误差
        player.transform.position = playerEndPos;
        mainCam.transform.position = camEndPos;

        // 7. 更新笼子形状，并重新唤醒 Cinemachine！它会无缝接手！
        SwitchToRoom(nextRoom, true);
        globalConfiner.GetComponent<CinemachineCamera>().enabled = true;

        // 8. 归还动能，继续游戏
        player.Speed = preservedSpeed;
        player.IsTransitioning = false;

        //复活点
        if (nextRoom.respawnPoints != null && nextRoom.respawnPoints.Count > 0)
        {
            Transform closestPoint = null;
            float minDistance = float.MaxValue;

            foreach (Transform spawnPoint in nextRoom.respawnPoints)
            {
                if (spawnPoint == null) continue;

                float dist = Vector2.Distance(playerEndPos, spawnPoint.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPoint = spawnPoint;
                }
            }

            if (closestPoint != null)
            {
                player.currentCheckpoint = closestPoint.position;

                // 【核心新增：无缝自动存档！】
                // 只要跨过边界、锁定了新房间的复活点，立刻存档！
                if (GameManager.Instance != null)
                {
                    // SceneManager.GetActiveScene().name 会获取当前场景名，但在 Additive 模式下，
                    // 最好直接用当前 GameObject 所在场景的名字
                    string currentSceneName = gameObject.scene.name;
                    GameManager.Instance.SaveGameProgress(currentSceneName, closestPoint.position);
                }
            }
        }
    }

    // 更新当前房间数据，并重塑摄像机边界
    private void SwitchToRoom(RoomData newRoom, bool playTransition)
    {
        currentRoom = newRoom;
        // 根据数学 Rect，手动捏出四个顶点的多边形
        Vector2[] newPoints = new Vector2[4];
        newPoints[0] = new Vector2(newRoom.bounds.xMin, newRoom.bounds.yMin); // 左下
        newPoints[1] = new Vector2(newRoom.bounds.xMin, newRoom.bounds.yMax); // 左上
        newPoints[2] = new Vector2(newRoom.bounds.xMax, newRoom.bounds.yMax); // 右上
        newPoints[3] = new Vector2(newRoom.bounds.xMax, newRoom.bounds.yMin); // 右下

        dynamicCameraBoundingBox.SetPath(0, newPoints);
        // 刷新摄像机缓存
        globalConfiner.InvalidateBoundingShapeCache();

        // ================= 【核心修复：智能锁死小房间摄像机】 =================
        Camera mainCam = Camera.main;
        if (mainCam != null && globalConfiner != null)
        {
            float camHeight = mainCam.orthographicSize * 2f;
            float camWidth = camHeight * mainCam.aspect;

            var vcam = globalConfiner.GetComponent<CinemachineCamera>();

            // 如果房间的宽度比摄像机还窄，或者高度比摄像机还矮！
            if (newRoom.bounds.width <= camWidth || newRoom.bounds.height <= camHeight)
            {
                // 房间太小了！拔掉跟随目标，让摄像机死死卡在房间正中央！
                if (vcam != null)
                {
                    vcam.Follow = null; // 取消跟随小恐龙

                    // 强行把摄像机锁在房间中心点（Z轴保持不变）
                    Vector3 roomCenter = new Vector3(newRoom.bounds.center.x, newRoom.bounds.center.y, vcam.transform.position.z);
                    vcam.ForceCameraPosition(roomCenter, Quaternion.identity);
                }
            }
            else
            {
                // 房间足够大，恢复对小恐龙的跟随！
                if (vcam != null && player != null)
                {
                    vcam.Follow = player.transform;
                }
            }
        }
    }

    /// <summary>
    /// 找到指定房间内的所有可重置机关，并强制它们恢复出厂设置
    /// </summary>
    private void ResetRoomEntities(RoomData roomToReset)
    {
        if (string.IsNullOrEmpty(roomToReset.roomName)) return;

        // 1. 找遍全宇宙的脚本（包括那些被 SetActive(false) 隐藏掉的！）
        // FindObjectsInactive.Include 极其关键！如果平台隐藏了自己，不加这句就搜不到它！
        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        //在进行全局状态重置时，绝不能遗漏被禁用的对象。

        foreach (var script in allScripts)
        {
            // 2. 【安全的 C# 转换】：如果这个脚本考了 IResettable 证件
            if (script is IResettable entity)
            {
                // 3. 【核心修复】：查它“户口本上”的出生点，而不是它现在掉在哪！
                if (roomToReset.bounds.Contains(entity.GetOriginalPosition()))
                {
                    entity.ResetState();
                }
            }
        }
    }

    /// <summary>
    /// 供玩家死亡全黑时调用，瞬间重置当前所在房间的所有机关
    /// </summary>
    public void ResetCurrentRoomEntities()
    {
        // currentRoom 是 LevelManager 内部记录的当前房间数据
        if (!string.IsNullOrEmpty(currentRoom.roomName))
        {
            ResetRoomEntities(currentRoom); // 复用我们之前写的暴力重置方法
        }
    }

    // 在 Unity 编辑器里画出这些房间边界，方便调整
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (var room in allRooms)
        {
            // Gizmos 画图是以中心点和大小来画的
            Gizmos.DrawWireCube(room.bounds.center, room.bounds.size);
        }
    }
}