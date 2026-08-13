using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconMetalTerminal : SemiconInteractable
    {
        [SerializeField] private MetalExperimentPanel metalExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(6)
            ? "[E]  금속 배선 공정 연구 단말기\n스퍼터 파워·공정 시간 실험 및 METAL-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(6, "금속 배선 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(MetalExperimentPanel panel, Transform glow)
        {
            metalExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(6))
            {
                SemiconCampaignAccess.ShowLockedToast(6, "금속 배선 공정");
                return;
            }
            metalExperimentPanel?.Open(player, followCamera);
        }
    }
}
