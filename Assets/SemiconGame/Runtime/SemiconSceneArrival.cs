using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SemiconCity.Game
{
    public sealed class SemiconSceneArrival : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            var sceneName = SceneManager.GetActiveScene().name;
            if (!SemiconSceneTravel.TryConsume(sceneName, out var position, out var rotation,
                    out var message, out var interiorCamera))
            {
                yield break;
            }

            var player = FindFirstObjectByType<SemiconPlayerController>();
            var cameraController = FindFirstObjectByType<SemiconThirdPersonCamera>();
            if (player == null)
            {
                Debug.LogError($"[Semicon Scene] {sceneName} 씬에 플레이어가 없습니다.");
                yield break;
            }

            player.Teleport(position, rotation);
            player.SetInputEnabled(true);
            player.GetComponent<SemiconFallRecovery>()?.SetSafePosition(position, rotation);
            cameraController?.SetInteriorMode(interiorCamera);
            cameraController?.SnapToTarget();
            FindFirstObjectByType<SemiconHud>()?.ShowToast(message);
            Debug.Log($"[Semicon Scene] ARRIVED / {sceneName} / {position}");
        }
    }
}
