using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconEtchTerminal : SemiconInteractable
    {
        [SerializeField] private EtchExperimentPanel etchExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(4)
            ? "[E]  식각 공정 연구 단말기\nRF 파워·가스 유량 실험 및 ETCH-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(4, "식각 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(EtchExperimentPanel panel, Transform glow)
        {
            etchExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(4))
            {
                SemiconCampaignAccess.ShowLockedToast(4, "식각 공정");
                return;
            }
            etchExperimentPanel?.Open(player, followCamera);
        }
    }
}
