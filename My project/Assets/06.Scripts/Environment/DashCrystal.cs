using UnityEngine;
using System.Collections;

public class DashCrystal : MonoBehaviour, IInteractable
{
    [Header("水晶设置")]
    public float respawnTime = 2.5f;

    [Header("视觉与特效")]
    public GameObject collectEffectPrefab;
    public GameObject respawnEffectPrefab;

    private SpriteRenderer sr;
    private BoxCollider2D col;

    private bool isActive = true;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    public void Interact(PlayerStateMachine player)
    {
        if (!isActive) return;

        player.CanDash = true;
        player.CurrentStamina = player.maxStamina;
        //TODO: 可以加个UI提示玩家获得了冲刺能力
        if (collectEffectPrefab != null)
        {
            Instantiate(collectEffectPrefab, transform.position, Quaternion.identity);
        }

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isActive = false;
        sr.enabled = false;

        yield return new WaitForSeconds(respawnTime);
        //TODO: 这里可以加个重生特效
        if (respawnEffectPrefab != null)
        {
            Instantiate(respawnEffectPrefab, transform.position, Quaternion.identity);
        }

        isActive = true;
        sr.enabled = true;
    }
}
