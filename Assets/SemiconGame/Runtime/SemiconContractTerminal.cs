using UnityEngine;

namespace SemiconCity.Game
{
    public sealed class SemiconContractTerminal : SemiconInteractable
    {
        [SerializeField] private SemiconContractPanel panel;
        [SerializeField] private Transform glowTransform;
        public override string Prompt => "[E]  FAB 납품 계약 보드\n공정 샘플·완제품 주문 수락 및 납품";
        public void Configure(SemiconContractPanel targetPanel, Transform glow) { panel = targetPanel; glowTransform = glow; }
        private void Update()
        {
            if (glowTransform == null) return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.35f) * 0.06f;
            glowTransform.localScale = new Vector3(pulse, pulse, pulse);
        }
        public override void Interact(SemiconPlayerController player, SemiconThirdPersonCamera followCamera) => panel?.Open(player, followCamera);
    }
}
