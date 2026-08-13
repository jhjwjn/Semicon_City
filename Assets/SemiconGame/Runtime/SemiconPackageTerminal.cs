using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconPackageTerminal : SemiconInteractable
    {
        [SerializeField] private PackageExperimentPanel packageExperimentPanel;
        [SerializeField] private Transform glowTransform;

        public override string Prompt => SemiconCampaignAccess.IsUnlocked(8)
            ? "[E]  패키징 공정 연구 단말기\n본딩 압력·몰딩 온도 실험 및 PACKAGE-01 레시피 개발"
            : SemiconCampaignAccess.GetLockedPrompt(8, "패키징 공정");

        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }

        public void Configure(PackageExperimentPanel panel, Transform glow)
        {
            packageExperimentPanel = panel;
            glowTransform = glow;
        }

        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera)
        {
            if (!SemiconCampaignAccess.IsUnlocked(8))
            {
                SemiconCampaignAccess.ShowLockedToast(8, "패키징 공정");
                return;
            }
            packageExperimentPanel?.Open(player, followCamera);
        }
    }
}
