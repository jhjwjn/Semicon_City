using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconArchiveTerminal : SemiconInteractable
    {
        [SerializeField] private SemiconArchivePanel panel;
        [SerializeField] private Transform glowTransform;
        public override string Prompt => "[E]  FAB ARCHIVE\n공정·제품·자재·인력·디스크·고객 기록 열람";
        public void Configure(SemiconArchivePanel targetPanel, Transform glow) { panel = targetPanel; glowTransform = glow; }
        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.15f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }
        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera) => panel?.Open(player, followCamera);
    }
}
