using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconInteractionTerminal : SemiconInteractable
    {
        [SerializeField] private string terminalName = "포토 공정 연구실";
        [SerializeField, TextArea] private string description = "노광 조건 실험 및 레시피 개발";
        [SerializeField] private PhotoExperimentPanel photoExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(3)
            ? $"[E]  {terminalName}\n{description}"
            : SemiconCampaignAccess.GetLockedPrompt(3, "포토 공정");

        private void Update()
        {
            if (glowTransform == null)
            {
                return;
            }
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(PhotoExperimentPanel panel, Transform glow)
        {
            photoExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(3))
            {
                SemiconCampaignAccess.ShowLockedToast(3, "포토 공정");
                return;
            }
            if (photoExperimentPanel != null)
            {
                photoExperimentPanel.Open(player, followCamera);
            }
        }
    }
}
