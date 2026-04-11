using UnityEngine;
using System.Collections;

public class DashCrystal : MonoBehaviour, IInteractable, IResettable
{
    public GameObject activeVisual;
    public GameObject outlineVisual;

    [Header("水晶设置")]
    public float respawnTime = 2.5f;

    [Header("视觉与特效")]
    public GameObject collectEffectPrefab;
    public GameObject respawnEffectPrefab;
    private bool isActive = true;

    private void Awake()
    {
        if (activeVisual != null) activeVisual.SetActive(true);
        if (outlineVisual != null) outlineVisual.SetActive(true);
    }

    public void Interact(PlayerStateMachine player)
    {
        if (!isActive) return;

        player.CanDash = true;
        player.CurrentStamina = player.maxStamina;
        //TODO: 可以加个UI提示玩家获得了冲刺能力

        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.Hitstop(0.05f);
        }

        // 吃到水晶的瞬间有一种微微的“滞空感”，防止下落太快瞬间砸地
        if (player.Speed.y < 0)
        {
            player.Speed.y *= 0.3f;
        }

        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isActive = false;

        if (activeVisual != null) activeVisual.SetActive(false);

        yield return new WaitForSeconds(respawnTime);

        Vector2 checkSize = new Vector2(0.5f, 0.5f);
        while (Physics2D.OverlapBox(transform.position, checkSize, 0, LayerMask.GetMask("Player")))
        {
            yield return null;
        }

        //TODO: 这里可以加个重生特效
        if (respawnEffectPrefab != null)
        {
            Instantiate(respawnEffectPrefab, transform.position, Quaternion.identity);
        }

        isActive = true;
        if (activeVisual != null) activeVisual.SetActive(true);
    }

    public void ResetState()
    {
        // 掐断可能正在进行的复活倒计时协程
        StopAllCoroutines();

        // 强行恢复出厂设置：可用，且显示图片
        isActive = true;
        if (activeVisual != null) activeVisual.SetActive(true);
    }

    public Vector2 GetOriginalPosition()
    {
        // 水晶这辈子都不会移动，所以它现在在哪，它的老家就在哪
        return transform.position;
    }
}
