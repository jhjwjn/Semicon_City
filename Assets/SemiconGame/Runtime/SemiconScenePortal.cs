using UnityEngine;
using UnityEngine.SceneManagement;

namespace SemiconCity.Game
{
    public sealed class SemiconScenePortal : SemiconInteractable
    {
        [SerializeField] private Transform destination;
        [SerializeField] private string prompt = "[E]  입장";
        [SerializeField] private string arrivalMessage = "구역으로 이동했습니다.";
        [SerializeField] private SemiconHud hud;
        [SerializeField] private GameObject activateZone;
        [SerializeField] private GameObject deactivateZone;
        [SerializeField] private bool useInteriorCamera;
        [Header("Scene transition")]
        [SerializeField] private string targetSceneName;
        [SerializeField] private Vector3 sceneArrivalPosition;
        [SerializeField] private Vector3 sceneArrivalEuler;
        [SerializeField, Range(0, 8)] private int requiredProcessNumber;

        public override string Prompt => requiredProcessNumber > 0 &&
                                         !SemiconCampaignAccess.IsUnlocked(requiredProcessNumber)
            ? SemiconCampaignAccess.GetLockedPrompt(requiredProcessNumber, GetProcessName(requiredProcessNumber))
            : prompt;

        public void Configure(Transform target, string interactionPrompt, string message, SemiconHud targetHud,
            GameObject zoneToActivate = null, GameObject zoneToDeactivate = null, bool interiorCamera = false)
        {
            destination = target;
            prompt = interactionPrompt;
            arrivalMessage = message;
            hud = targetHud;
            activateZone = zoneToActivate;
            deactivateZone = zoneToDeactivate;
            useInteriorCamera = interiorCamera;
            targetSceneName = string.Empty;
            requiredProcessNumber = 0;
        }

        public void ConfigureScene(string sceneName, Vector3 arrivalPosition, Quaternion arrivalRotation,
            string interactionPrompt, string message, SemiconHud targetHud, bool interiorCamera,
            int requiredProcess = 0)
        {
            targetSceneName = sceneName;
            sceneArrivalPosition = arrivalPosition;
            sceneArrivalEuler = arrivalRotation.eulerAngles;
            prompt = interactionPrompt;
            arrivalMessage = message;
            hud = targetHud;
            useInteriorCamera = interiorCamera;
            requiredProcessNumber = requiredProcess;
            destination = null;
            activateZone = null;
            deactivateZone = null;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (player == null)
            {
                return;
            }

            if (requiredProcessNumber > 0 && !SemiconCampaignAccess.IsUnlocked(requiredProcessNumber))
            {
                SemiconCampaignAccess.ShowLockedToast(requiredProcessNumber, GetProcessName(requiredProcessNumber));
                return;
            }

            if (!string.IsNullOrWhiteSpace(targetSceneName))
            {
                if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
                {
                    hud?.ShowToast($"이동할 씬을 찾을 수 없습니다: {targetSceneName}");
                    Debug.LogError($"[Semicon Scene] Build Settings에 씬이 없습니다: {targetSceneName}");
                    return;
                }

                player.SetInputEnabled(false);
                SemiconSceneTravel.Request(targetSceneName, sceneArrivalPosition,
                    Quaternion.Euler(sceneArrivalEuler), arrivalMessage, useInteriorCamera);
                SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
                return;
            }

            if (destination == null)
            {
                return;
            }

            if (activateZone != null)
            {
                activateZone.SetActive(true);
            }
            player.Teleport(destination.position, destination.rotation);
            player.GetComponent<SemiconFallRecovery>()?.SetSafePosition(destination.position, destination.rotation);
            followCamera?.SetInteriorMode(useInteriorCamera);
            followCamera?.SnapToTarget();
            hud?.ShowToast(arrivalMessage);
            if (deactivateZone != null)
            {
                deactivateZone.SetActive(false);
            }
        }

        private static string GetProcessName(int processNumber)
        {
            return processNumber switch
            {
                1 => "웨이퍼 공정",
                2 => "산화 공정",
                3 => "포토 공정",
                4 => "식각 공정",
                5 => "증착 공정",
                6 => "금속 배선 공정",
                7 => "EDS 검사 공정",
                8 => "패키징 공정",
                _ => "공정"
            };
        }
    }
}
