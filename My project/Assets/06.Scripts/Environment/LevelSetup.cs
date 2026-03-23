using UnityEngine;
using Unity.Cinemachine;

public class LevelSetup : MonoBehaviour
{
    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            var vcam = GetComponent<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = player.transform;
            }
        }
    }
}
