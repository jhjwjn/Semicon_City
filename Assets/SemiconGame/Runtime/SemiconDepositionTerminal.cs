using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconDepositionTerminal : SemiconInteractable
    {
        [SerializeField] private DepositionExperimentPanel depositionExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(5)
            ? "[E]  증착 공정 연구 단말기\n온도·압력 실험 및 DEPO-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(5, "증착 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(DepositionExperimentPanel panel, Transform glow)
        {
            depositionExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(5))
            {
                SemiconCampaignAccess.ShowLockedToast(5, "증착 공정");
                return;
            }
            depositionExperimentPanel?.Open(player, followCamera);
        }
    }
}
