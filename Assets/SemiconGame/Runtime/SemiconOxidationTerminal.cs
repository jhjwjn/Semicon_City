using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconOxidationTerminal : SemiconInteractable
    {
        [SerializeField] private OxidationExperimentPanel oxidationExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(2)
            ? "[E]  산화 공정 연구 단말기\n온도·시간 실험 및 OXIDE-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(2, "산화 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(OxidationExperimentPanel panel, Transform glow)
        {
            oxidationExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(2))
            {
                SemiconCampaignAccess.ShowLockedToast(2, "산화 공정");
                return;
            }
            oxidationExperimentPanel?.Open(player, followCamera);
        }
    }
}
