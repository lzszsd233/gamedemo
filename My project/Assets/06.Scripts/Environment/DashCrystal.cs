using UnityEngine;
using System.Collections;

public class DashCrystal : MonoBehaviour, IInteractable
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

        //TODO: 这里可以加个重生特效
        if (respawnEffectPrefab != null)
        {
            Instantiate(respawnEffectPrefab, transform.position, Quaternion.identity);
        }

        isActive = true;
        if (activeVisual != null) activeVisual.SetActive(true);
    }
}
