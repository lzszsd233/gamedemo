using UnityEngine;

/// <summary>
/// 静态背景跟随器
/// 专门解决 Cinemachine 架构下，背景挂在相机上会抖动穿帮的问题。
/// 确保背景永远死死、平滑地填满当前屏幕！
/// </summary>
public class StaticBackgroundFollower : MonoBehaviour
{
    private Transform mainCameraTransform;

    // 为了防止背景图的原始比例被破坏，我们记录它一开始的 Z 轴（通常离相机远一点）
    private float initialZ;

    private void Start()
    {
        // 锁定全世界唯一的主相机（Cinemachine Brain 所在的地方）
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        initialZ = transform.position.z;
    }

    // 必须用 LateUpdate！等 Cinemachine 把相机挪完位置后，背景再瞬间跟上！
    private void LateUpdate()
    {
        if (mainCameraTransform != null)
        {
            // 强行把背景的 X 和 Y 坐标，死死绑定在主相机的 X 和 Y 上！
            // Z 轴保持自己原来的距离，防止挡住游戏画面。
            transform.position = new Vector3(
                mainCameraTransform.position.x,
                mainCameraTransform.position.y,
                initialZ
            );
        }
    }
}