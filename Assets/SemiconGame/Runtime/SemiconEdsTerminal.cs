using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconEdsTerminal : SemiconInteractable
    {
        [SerializeField] private EdsExperimentPanel edsExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(7)
            ? "[E]  EDS 전기 검사 연구 단말기\n테스트 전압·누설 기준 실험 및 EDS-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(7, "EDS 검사 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(EdsExperimentPanel panel, Transform glow)
        {
            edsExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(7))
            {
                SemiconCampaignAccess.ShowLockedToast(7, "EDS 검사 공정");
                return;
            }
            edsExperimentPanel?.Open(player, followCamera);
        }
    }
}
