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
}
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [Header("全局依赖")]
    private PlayerStateMachine player;
    public CinemachineConfiner2D globalConfiner;

    [Header("转场设置")]
    [Range(0.1f, 1.5f)]
    public float transitionDuration = 0.6f; // 默认延长到 0.6 秒，你可以随时在面板调

    // 整个关卡的房间数据列表
    [Header("房间数据")]
    public List<RoomData> allRooms = new List<RoomData>();

    // 运行时需要的私有变量
    private RoomData currentRoom;
    private PolygonCollider2D dynamicCameraBoundingBox; // 我们用代码动态捏的一个形状，专门喂给摄像机

    private void Awake()
    {
        Instance = this;

        // 在自己身上动态创建一个物理碰撞体，设置为 Trigger
        // 这个碰撞体永远不参与游戏逻辑，它唯一的使命就是变形成当前房间的形状，喂给摄像机限制器
        dynamicCameraBoundingBox = gameObject.AddComponent<PolygonCollider2D>();
        dynamicCameraBoundingBox.isTrigger = true;
        globalConfiner.BoundingShape2D = dynamicCameraBoundingBox;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.GetComponent<PlayerStateMachine>();

            if (player != null)
            {
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
        }
    }

    private void Update()
    {
        // 核心数学审判：如果玩家没有在转场，且不在当前房间的数学边界内
        if (!player.IsTransitioning && !currentRoom.bounds.Contains(player.transform.position))
        {
            // 越界,寻找掉进了哪个新房间
            foreach (var room in allRooms)
            {
                if (room.bounds.Contains(player.transform.position))
                {
                    // 找到了新房间，开始转场
                    StartCoroutine(TransitionRoutine(room));
                    break;
                }
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

        // 算出要被推向哪里
        Vector3 pushDir = Vector3.zero;
        if (Mathf.Abs(preservedSpeed.x) > Mathf.Abs(preservedSpeed.y))
            pushDir.x = Mathf.Sign(preservedSpeed.x);
        else
            pushDir.y = Mathf.Sign(preservedSpeed.y);

        Vector3 playerStartPos = player.transform.position;
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
        Vector3 camEndPos = new Vector3(camEndX, camEndY, camStartPos.z);

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

        player.currentCheckpoint = player.transform.position;
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
