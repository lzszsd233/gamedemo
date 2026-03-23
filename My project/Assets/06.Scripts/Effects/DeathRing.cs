using UnityEngine;

/// <summary>
/// 死亡花火发射器（负责生成并呈环状发射 8 颗碎片）
/// </summary>
public class DeathRing : MonoBehaviour
{
    [Header("碎片设置")]
    public GameObject orbPrefab;
    public int orbCount = 8;
    public float orbSpeed = 15f; // 碎片飞行的初速度
    public float orbLifeTime = 0.5f; // 碎片多久后完全消失

    public bool isRespawnMode = false;

    private void Start()
    {
        Explode();
    }

    private void Explode()
    {
        if (orbPrefab == null) return;

        // 计算每颗碎片之间的角度间隔 (360度 / 8 = 45度)
        float angleStep = 360f / orbCount;

        for (int i = 0; i < orbCount; i++)
        {
            // 1. 计算这颗碎片的角度
            float angle = i * angleStep;

            // 2. 把角度(度数)转换成二维的向量方向 (数学公式：(cos, sin))
            float rad = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            // 3. 生成这颗碎片
            GameObject orbObj = Instantiate(orbPrefab, transform.position, Quaternion.identity);

            // 4. 找到碎片身上的脚本，给它下达起飞命令！
            DeathOrb orbScript = orbObj.GetComponent<DeathOrb>();
            if (orbScript != null)
            {
                orbScript.Init(direction, orbSpeed, orbLifeTime, isRespawnMode);
            }
        }

        // 发射完 8 颗碎片后，这个发射器就没用了，销毁自己
        Destroy(gameObject);
    }
}
